using AlyMq.Brokers.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using System.Security;
using AlyMq.Producers;
using AlyMq.Consumers;
using System.Text.Json;
using System.Collections;

namespace AlyMq.Brokers
{
    public class DefaultBrokerService : IBrokerService
    {
        private Socket _adapter;
        private Socket _server;
        private readonly Broker _broker;
        private readonly HashSet<Producer> _producers;
        private readonly HashSet<Consumer> _consumers;
        private readonly HashSet<Topic> _topics;
        private readonly HashSet<Queue> _queues;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultBrokerService> _logger;

        public DefaultBrokerService(ILogger<DefaultBrokerService> logger)
        {
            _logger = logger;

            _broker = new Broker
            {
                Ip = BrokerConfig.Instance.Address.Ip,
                Key = BrokerConfig.Instance.Key,
                Backlog = BrokerConfig.Instance.Address.Backlog,
                Name = BrokerConfig.Instance.Name,
                Port = BrokerConfig.Instance.Address.Port,
                CreateOn = DateTime.Now
            };

            _producers = new HashSet<Producer>();
            _consumers = new HashSet<Consumer>();
            _topics = new HashSet<Topic>();
            _queues = new HashSet<Queue>();
            _clients = new HashSet<Socket>();
        }

        private void Startup()
        {
            InitDefault();
            BrokerListen();
            AdapterConnect();
            TenSecondsPoller();
            ThirtySecondsPoller();
        }

        private void BrokerListen()
        {
            try
            {
                _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new(IPAddress.Parse(BrokerConfig.Instance.Address.Ip), BrokerConfig.Instance.Address.Port);
                Listen(_server, ipEndPoint, BrokerConfig.Instance.Address.Backlog);
            }
            catch (ArgumentNullException aue) { throw aue; }
            catch (FormatException fe) { throw fe; }
        }

        private void AdapterConnect()
        {
            try
            {
                _adapter = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new(IPAddress.Parse(BrokerConfig.Instance.AdapterAddress.Ip), BrokerConfig.Instance.AdapterAddress.Port);
                Connect(_adapter, ipEndPoint);
            }
            catch (ArgumentNullException aue) { throw aue; }
            catch (FormatException fe) { throw fe; }
        }

        private void Listen(Socket socket, IPEndPoint ipEndPoint, int backlog)
        {
            try
            {
                socket.Bind(ipEndPoint);
                socket.Listen(backlog);

                _logger.LogInformation("Server is listenting on {arg} ...", ipEndPoint);

                Accept(socket);

            }
            catch (ArgumentNullException aue) { throw aue; }
            catch (ObjectDisposedException ode) { throw ode; }
            catch (FormatException fe) { throw fe; }
            catch (SocketException se) { throw se; }
            catch (SecurityException se) { throw se; }
        }

        private void Accept(Socket socket)
        {
            SocketAsyncEventArgs args = new();
            args.Completed += AcceptCallback;
            Accept(socket, args);

            _logger.LogInformation("Async accepting on {arg} ...", socket.LocalEndPoint);
        }

        private void Accept(Socket socket, SocketAsyncEventArgs args)
        {
            try
            {
                args.AcceptSocket = null;
                if (!socket.AcceptAsync(args)) { AcceptCallback(socket, args); }
            }
            catch (SocketException se) { throw se; }
            catch (ObjectDisposedException ode) { throw ode; }
            catch (ArgumentOutOfRangeException aore) { throw aore; }
            catch (ArgumentException ae) { throw ae; }
            catch (InvalidOperationException ioe) { throw ioe; }
            catch (NotSupportedException nse) { throw nse; }
        }

        private void AcceptCallback(dynamic socket, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _logger.LogInformation("Async accepted client{arg} ...", args.AcceptSocket.RemoteEndPoint);

                _clients.Add(args.AcceptSocket);
                Receive(args.AcceptSocket);
                Accept(socket, args);
            }
            else
            {
                //When the monitoring service is actively closed, a callback will be triggered here

                _logger.LogInformation("Broker listenting is closed ...");

                foreach (Socket client in _clients)
                {
                    _logger.LogInformation("Client {arg} is closed ...", client.RemoteEndPoint);

                    client.Shutdown(SocketShutdown.Both);
                    client.Dispose();
                    client.Close();
                }
                _clients.Clear();
                args.Dispose();
            }
        }

        private void Receive(Socket socket)
        {
            SocketAsyncEventArgs args = new();
            args.Completed += ReceiveCallback;
            args.SetBuffer(new byte[8912], 0, 8912);
            args.UserToken = new MemoryStream();
            Receive(socket, args);

            _logger.LogInformation("Async receiving on {arg} ...", socket.RemoteEndPoint);
        }

        private void Receive(Socket socket, SocketAsyncEventArgs args)
        {
            try
            {
                if (!socket.ReceiveAsync(args)) { ReceiveCallback(socket, args); }
            }
            catch (SocketException se) { throw se; }
            catch (ArgumentException ae) { throw ae; }
            catch (NotSupportedException nse) { throw nse; }
            catch (ObjectDisposedException ode) { throw ode; }
            catch (InvalidOperationException ioe) { throw ioe; }
        }

        private void ReceiveCallback(dynamic socket, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.BytesTransferred > 0)
            {
                MemoryStream ms = args.UserToken as MemoryStream;
                ms.Write(args.Buffer, args.Offset, args.BytesTransferred);

                if (socket.Available == 0)
                {
                    ApartMessage(socket, ms, 0);
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.SetLength(0);
                }

                Receive(socket, args);
            }
            else
            {
                if (!socket.SafeHandle.IsClosed)
                {
                    string remoteEndPoint = socket.RemoteEndPoint.ToString();
                    _logger.LogInformation("Client {arg} is closed ...", remoteEndPoint);

                    _clients.Remove(socket);

                    socket.Shutdown(SocketShutdown.Both);
                    socket.Dispose();
                    socket.Close();
                }

                args.Dispose();
            }
        }

        private void Connect(Socket socket, IPEndPoint remoteEndPoint)
        {
            try
            {
                byte[] buffer = GetReportBrokerBuffer();

                SocketAsyncEventArgs args = new();
                args.Completed += ConnectCallback;
                args.RemoteEndPoint = remoteEndPoint;
                args.SetBuffer(buffer, 0, buffer.Length);

                if (!socket.ConnectAsync(args)) { ConnectCallback(socket, args); }
            }
            catch (ArgumentNullException ane) { throw ane; }
            catch (ArgumentException ae) { throw ae; }
            catch (ObjectDisposedException ode) { throw ode; }
            catch (InvalidOperationException ioe) { throw ioe; }
            catch (SocketException se) { throw se; }
            catch (NotSupportedException nse) { throw nse; }
            catch (SecurityException se) { throw se; }
        }

        private void ConnectCallback(dynamic socket, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                string remoteEndPoint = socket.RemoteEndPoint.ToString();
                _logger.LogInformation("Broerk is connected to remote server {arg} ...", remoteEndPoint);

                Receive(socket);
            }
            else
            {
                _logger.LogInformation("Broker connecting to remote server is failed ...");

                socket.Dispose();
                socket.Close();
                args.Dispose();
            }
        }

        private void SendCallback(dynamic socket, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                string remoteEndPoint = socket.RemoteEndPoint.ToString();
                _logger.LogInformation("Send to {arg} is success ...", remoteEndPoint);
            }
            else
            {
                _logger.LogInformation("Send to remote server is failed ...");

                socket.Dispose();
                socket.Close();
                args.Dispose();
            }
        }

        private void ApartMessage(Socket socket, MemoryStream ms, int offset)
        {
            if (ms.Length > offset)
            {
                ms.Seek(offset, SeekOrigin.Begin);

                byte[] instructBuffer = new byte[4];
                ms.Read(instructBuffer, 0, 4);
                int instruct = BitConverter.ToInt32(instructBuffer);

                switch (instruct)
                {
                    case Instruct.ReportProducer:
                        ApartReportProducer(socket, ms);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApartReportProducer(Socket socket, MemoryStream memoryStream)
        {
            byte[] producerLengthBuffer = new byte[4];
            memoryStream.Read(producerLengthBuffer, 0, 4);
            int producerBufferLength = BitConverter.ToInt32(producerLengthBuffer);

            byte[] producerBuffer = new byte[producerBufferLength];
            memoryStream.Read(producerBuffer, 0, producerBufferLength);

            Producer producer = JsonSerializer.Deserialize<Producer>(producerBuffer);
            _producers.Add(producer);

            _logger.LogInformation("Producer[{name} -> {ip}:{port}] is reported ...", producer.Name, producer.Ip, producer.Port);

            int offset = producerBufferLength + 8;//8 = Instruct of byte + Producer length of byte

            ApartMessage(socket, memoryStream, offset);
        }

        private void TenSecondsPoller()
        {
            Timer timer = new(10000);
            timer.Elapsed += (s, e) =>
            {
                ProducerInspecter();
                ConsumerInspecter();
            };
            timer.Enabled = true;
        }

        private void ThirtySecondsPoller()
        {
            Timer timer = new(30000);
            timer.Elapsed += (s, e) =>
            {
                //timer.Enabled = false;
                ReportBroker();
            };
            timer.Enabled = true;
        }

        private void ProducerInspecter()
        {
            _producers.ToList().ForEach(item =>
            {
                if (item.PulseOn.AddMinutes(2) < DateTime.Now)
                {
                    _producers.Remove(item);
                    Socket socket = _clients.FirstOrDefault(m => m.RemoteEndPoint.ToString() == $"{item.Ip}:{item.Port}");
                    if (socket != null)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Dispose();
                        socket.Close();
                    }
                }
            });
        }

        private void ConsumerInspecter()
        {
            _consumers.ToList().ForEach(item =>
            {
                if (item.PulseOn.AddMinutes(2) < DateTime.Now)
                {
                    _consumers.Remove(item);
                    Socket socket = _clients.FirstOrDefault(m => m.RemoteEndPoint.ToString() == $"{item.Ip}:{item.Port}");
                    if (socket != null)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Dispose();
                        socket.Close();
                    }
                }
            });
        }

        private void ReportBroker()
        {

            if (_adapter != null && !_adapter.SafeHandle.IsClosed)
            {
                SocketAsyncEventArgs args = new();
                byte[] buffer = GetReportBrokerBuffer();
                args.SetBuffer(buffer, 0, buffer.Length);

                try
                {
                    if (_adapter.SendAsync(args)) { SendCallback(_adapter, args); }
                }
                catch (ArgumentException ae) { throw ae; }
                catch (ObjectDisposedException ode) { throw ode; }
                catch (InvalidOperationException ioe) { throw ioe; }
                catch (NotSupportedException nse) { throw nse; }
                catch (SocketException se) { throw se; }
            }
        }

        private byte[] GetReportBrokerBuffer()
        {
            _broker.PulseOn = DateTime.Now;
            byte[] brokerBuffer = JsonSerializer.SerializeToUtf8Bytes(_broker);
            byte[] topicBuffer = JsonSerializer.SerializeToUtf8Bytes(_topics);

            using var msBuffer = new MemoryStream();
            msBuffer.Write(BitConverter.GetBytes(Instruct.ReportBroker));
            msBuffer.Write(BitConverter.GetBytes(brokerBuffer.Length));
            msBuffer.Write(BitConverter.GetBytes(topicBuffer.Length));
            msBuffer.Write(brokerBuffer);
            msBuffer.Write(topicBuffer);

            return msBuffer.GetBuffer(); ;
        }

        #region Init broker default

        private void InitDefault()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("brokerconfig.json", true, true)
              .Build();

            config.Bind("Topics", _topics);

            _logger.LogInformation("Broker initialized ...");
        }

        #endregion

        #region IBrokerService methods

        public Task Start()
        {
            Startup();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_server != null && !_server.SafeHandle.IsClosed)
            {
                _server.Dispose();
                _server.Close();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
