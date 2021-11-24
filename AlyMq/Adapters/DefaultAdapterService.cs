using AlyMq.Adapters.Configuration;
using AlyMq.Adapters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Timers;
using System.Text;
using System.Security;
using AlyMq.Brokers;

namespace AlyMq.Adapters
{
    public class DefaultAdapterService : IAdapterService
    {
        private Socket _server;
        private readonly HashSet<Topic> _topics;
        private readonly HashSet<Broker> _brokers;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultAdapterService> _logger;
        public DefaultAdapterService(ILogger<DefaultAdapterService> logger)
        {
            _logger = logger;
            _topics = new HashSet<Topic>();
            _brokers = new HashSet<Broker>();
            _clients = new HashSet<Socket>();
        }
        private void Startup()
        {
            AdapterListen();
            TenSecondsPoller();
        }

        private void AdapterListen()
        {
            try
            {
                _server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(AdapterConfig.Instance.Address.Ip), AdapterConfig.Instance.Address.Port);
                Listen(_server, ipEndPoint, AdapterConfig.Instance.Address.Backlog);

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

                _logger.LogInformation($"Adapter is listenting on: [{ipEndPoint}] ...");

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
            args.SetBuffer(new byte[8912], 0, 8912);
            Accept(socket, args);

            _logger.LogInformation($"Async accepting is started for adapter[{socket.LocalEndPoint}]...");
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
                _logger.LogInformation($"Async accepted client[{args.AcceptSocket.RemoteEndPoint}] ...");

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

            _logger.LogInformation($"Async receiving is started for client[{socket.RemoteEndPoint}]...");
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
                    case Instruct.ReportBroker:
                        ApartReportBroker(socket, ms);
                        break;
                    case Instruct.PullBrokers:
                        ApartPullBrokers(socket, ms);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApartReportBroker(Socket socket, MemoryStream memoryStream)
        {
            byte[] brokerLengthBuffer = new byte[4];
            memoryStream.Read(brokerLengthBuffer, 0, 4);
            int brokerBufferLength = BitConverter.ToInt32(brokerLengthBuffer);

            byte[] topicLengthBuffer = new byte[4];
            memoryStream.Read(topicLengthBuffer, 0, 4);
            int topicBufferLength = BitConverter.ToInt32(topicLengthBuffer);

            byte[] brokerBuffer = new byte[brokerBufferLength];
            memoryStream.Read(brokerBuffer, 0, brokerBufferLength);

            byte[] topicBuffer = new byte[topicBufferLength];
            memoryStream.Read(topicBuffer, 0, topicBufferLength);

            using (MemoryStream msBroker = new MemoryStream(brokerBuffer))
            {
                using (MemoryStream msTopic = new MemoryStream(topicBuffer))
                {
                    IFormatter iFormatter = new BinaryFormatter();

                    Broker brokerInfo = iFormatter.Deserialize(msBroker) as Broker;

                    HashSet<Topic> topics = iFormatter.Deserialize(msTopic) as HashSet<Topic>;

                    _brokers.Add(brokerInfo);
                    _topics.UnionWith(topics);

                    _logger.LogInformation($"{brokerInfo.Name}->{brokerInfo.Ip}:{brokerInfo.Port} is reported ...");
                }
            }

            int offset = brokerBufferLength + topicBufferLength + 12;//12 = Instruct of byte + BrokerInfo length of byte + Topics length of byte

            ApartMessage(socket, memoryStream, offset);
        }

        private void ApartPullBrokers(Socket socket, MemoryStream memoryStream)
        {
            byte[] topicKeysLengthBuffer = new byte[4];
            memoryStream.Read(topicKeysLengthBuffer, 0, 4);
            int topicKeysLength = BitConverter.ToInt32(topicKeysLengthBuffer);

            byte[] topicKeysBuffer = new byte[topicKeysLength];
            memoryStream.Read(topicKeysBuffer, 0, topicKeysLength);

            using (MemoryStream msTopicKeys = new MemoryStream(topicKeysBuffer))
            {
                IFormatter iFormatter = new BinaryFormatter();

                IEnumerable<Guid> topicKeys = iFormatter.Deserialize(msTopicKeys) as IEnumerable<Guid>;

                IEnumerable<Topic> topics = _topics.Where(m => topicKeys.Contains(m.Key));

                IEnumerable<Broker> brokers = _brokers.Where(m => topics.Any(t => t.BrokerKey == m.Key));

                _logger.LogInformation($"Client [{socket.RemoteEndPoint}] is pull broker by topic keys ...");

                ReplyPullBrokers(socket, brokers);

                //_logger.LogInformation($"Client [{socket.RemoteEndPoint}] is pull topic broker ...");
            }

            int offset = topicKeysLength + 8;//12 = Instruct of byte + topicKeys length of byte

            ApartMessage(socket, memoryStream, offset);
        }

        private void ReplyPullBrokers(Socket socket, IEnumerable<Broker> borkers)
        {
            using (var msBuffer = new MemoryStream())
            {
                using (var msBroker = new MemoryStream())
                {
                    using (var msTopic = new MemoryStream())
                    {
                        IFormatter iFormatter = new BinaryFormatter();

                        iFormatter.Serialize(msBroker, borkers.ToHashSet());
                        byte[] brokersBuffer = msBroker.GetBuffer();

                        msBuffer.Write(BitConverter.GetBytes(Instruct.PullBrokers));
                        msBuffer.Write(BitConverter.GetBytes(brokersBuffer.Length));
                        msBuffer.Write(brokersBuffer);

                        byte[] buffer = msBuffer.GetBuffer();
                        SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                        args.SetBuffer(buffer, 0, buffer.Length);

                        try
                        {
                            if (socket.SendAsync(args)) { SendCallback(socket, args); }

                            _logger.LogInformation($"Push brokers to client [{socket.RemoteEndPoint}] ...");
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

        private void TenSecondsPoller()
        {
            Timer timer = new Timer(10000);
            timer.Elapsed += (s, e) =>
            {
                BrokerInspecter();
            };
            timer.Enabled = true;
        }
        private void BrokerInspecter()
        {
            _brokers.ToList().ForEach(item =>
            {
                if (item.PulseOn.AddMinutes(2) < DateTime.Now)
                {
                    _brokers.Remove(item);
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

        #region IAdapterService methods

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
