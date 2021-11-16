using AlyMq.Producer;
using AlyMq.Producer.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Linq;
using AlyMq.Broker;

namespace AlyMq.Producer
{
    public class DefaultProducerService : IProducerService
    {
        private Socket _adapter;
        private Socket _producer;
        private readonly HashSet<Topic> _topics;
        private readonly HashSet<BrokerInfo> _borkers;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultProducerService> _logger;

        public DefaultProducerService(ILogger<DefaultProducerService> logger)
        {
            _logger = logger;
            _topics = new HashSet<Topic>();
            _borkers = new HashSet<BrokerInfo>();
            _clients = new HashSet<Socket>();
        }

        private void Startup()
        {
            InitDefault();
            ProducerListen();
            AdapterConnect();
            PullBrokerTimer();
        }

        private void ProducerListen()
        {
            try
            {
                _producer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ProducerConfig.Instance.Address.Ip), ProducerConfig.Instance.Address.Port);
                Listen(_producer, ipEndPoint, ProducerConfig.Instance.Address.Backlog);
            }
            catch (ArgumentNullException aue) { throw aue; }
            catch (FormatException fe) { throw fe; }
        }

        private void AdapterConnect()
        {
            try
            {
                _adapter = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ProducerConfig.Instance.AdapterAddress.Ip), ProducerConfig.Instance.AdapterAddress.Port);
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
                _logger.LogInformation($"Server is listenting on: [{ipEndPoint}] ...");

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
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += AcceptCallback;
            Accept(socket, args);
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
                _logger.LogInformation($"Client [{args.AcceptSocket.RemoteEndPoint}] is accepted ...");

                _clients.Add(args.AcceptSocket);
                Receive(args.AcceptSocket);
                Accept(socket, args);
            }
            else
            {
                //When the monitoring service is actively closed, a callback will be triggered here

                _logger.LogInformation($"Service listenting is closed ...");

                foreach (Socket client in _clients)
                {
                    _logger.LogInformation($"Client [{client.RemoteEndPoint}] is closed ...");

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
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.Completed += ReceiveCallback;
            args.SetBuffer(new byte[8912], 0, 8912);
            args.UserToken = new MemoryStream();
            Receive(socket, args);
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
                    _logger.LogInformation($"Client {socket.RemoteEndPoint} is closed ...");

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
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += ConnectCallback;
                args.RemoteEndPoint = remoteEndPoint;

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
                _logger.LogInformation($"Server [{socket.RemoteEndPoint}] is connected ...");

                Receive(socket);
            }
            else
            {
                _logger.LogInformation($"Connect to remote server is failed ...");

                socket.Dispose();
                socket.Close();
                args.Dispose();
            }
        }

        private void SendCallback(dynamic socket, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                _logger.LogInformation($"Send to [{socket.RemoteEndPoint}] is success ...");
            }
            else
            {
                _logger.LogInformation($"Send to remote server is failed ...");

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
                    case Instruct.PushBrokerFromAdapter:
                        ApartPushBrokerFromAdapter(socket, ms);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApartPushBrokerFromAdapter(Socket socket, MemoryStream memoryStream) {

            byte[] brokersLengthBuffer = new byte[4];
            memoryStream.Read(brokersLengthBuffer, 0, 4);
            int brokersLength = BitConverter.ToInt32(brokersLengthBuffer);

            byte[] brokersBuffer = new byte[brokersLength];
            memoryStream.Read(brokersBuffer, 0, brokersLength);

            using (MemoryStream msBrokers = new MemoryStream(brokersBuffer))
            {
                IFormatter iFormatter = new BinaryFormatter();

                HashSet<BrokerInfo> brokers = iFormatter.Deserialize(msBrokers) as HashSet<BrokerInfo>;

                _borkers.UnionWith(brokers);

                _logger.LogInformation($"Producer pull brokers by topic keys form adapter [{_adapter.RemoteEndPoint}] is success ...");
            }

            int offset = brokersLength + 8;//12 = Instruct of byte + brokers length of byte

            ApartMessage(socket, memoryStream, offset);
        }

        private void PullBrokerTimer()
        {
            Timer timer = new Timer(30000);
            timer.Elapsed += (s, e) =>
            {
                PullBroker();
            };
            timer.Enabled = true;
        }

        private void PullBroker() {
            if (_adapter != null && !_adapter.SafeHandle.IsClosed)
            {
                using (var msBuffer = new MemoryStream())
                {
                    using (var msTopicKeysBuffer = new MemoryStream())
                    {
                        IFormatter iFormatter = new BinaryFormatter();

                        IEnumerable<Guid> topicKeys = _topics.Select(s => s.Key);
                        iFormatter.Serialize(msTopicKeysBuffer, topicKeys.ToHashSet());
                        byte[] topicKeysBuffer = msTopicKeysBuffer.GetBuffer();

                        msBuffer.Write(BitConverter.GetBytes(Instruct.PullBrokerByTopicKeys));
                        msBuffer.Write(BitConverter.GetBytes(topicKeysBuffer.Length));
                        msBuffer.Write(topicKeysBuffer);

                        byte[] buffer = msBuffer.GetBuffer();
                        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                        args.SetBuffer(buffer, 0, buffer.Length);

                        try
                        {
                            if (_adapter.SendAsync(args)) { SendCallback(_adapter, args); }
                            _logger.LogInformation($"Producer create pull brokers by topic keys form adapter [{_adapter.RemoteEndPoint}] ...");
                        }
                        catch (ArgumentException ae) { throw ae; }
                        catch (ObjectDisposedException ode) { throw ode; }
                        catch (InvalidOperationException ioe) { throw ioe; }
                        catch (NotSupportedException nse) { throw nse; }
                        catch (SocketException se) { throw se; }
                    }
                }
                
            }
        }

        #region Init broker default

        private void InitDefault()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("producerconfig.json", true, true)
              .Build();

            config.Bind("Topics", _topics);
        }

        #endregion

        #region IProducerService methods

        public Task Start()
        {
            Startup();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_producer != null && !_producer.SafeHandle.IsClosed)
            {
                _producer.Dispose();
                _producer.Close();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
