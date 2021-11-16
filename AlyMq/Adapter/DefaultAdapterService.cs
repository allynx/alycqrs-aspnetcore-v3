﻿using AlyMq.Adapter.Configuration;
using AlyMq.Adapter;
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
using AlyMq.Broker;

namespace AlyMq.Adapter
{
    public class DefaultAdapterService : IAdapterService
    {
        private Socket _adapter;
        private readonly HashSet<Topic> _topics;
        private readonly HashSet<BrokerInfo> _brokers;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultAdapterService> _logger;

        public DefaultAdapterService(ILogger<DefaultAdapterService> logger)
        {
            _logger = logger;
            _topics = new HashSet<Topic>();
            _brokers = new HashSet<BrokerInfo>();
            _clients = new HashSet<Socket>();
        }

        private void Startup()
        {
            AdapterListen();
            TestSendTimer();
        }

        private void AdapterListen()
        {
            try
            {
                _adapter = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(AdapterConfig.Instance.Address.Ip), AdapterConfig.Instance.Address.Port);
                Listen(_adapter, ipEndPoint, AdapterConfig.Instance.Address.Backlog);

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
                    _logger.LogInformation($"Client [{socket.RemoteEndPoint}] is received ...");

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


        #region Logic methods

        private void ApartMessage(Socket socket, MemoryStream ms, int offset)
        {
            if (ms.Length > offset)
            {
                ms.Seek(offset, SeekOrigin.Begin);

                byte[] instructBuffer = new byte[4];
                ms.Read(instructBuffer, 0, 4);
                int instruct = BitConverter.ToInt32(instructBuffer);

                switch (instruct) {
                    case Instruct.ReportBrokerTopics:
                        ApartReportBrokerTopics(socket, ms);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApartReportBrokerTopics(Socket socket, MemoryStream memoryStream)
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

                    BrokerInfo brokerInfo = iFormatter.Deserialize(msBroker) as BrokerInfo;

                    HashSet<Topic> topics = iFormatter.Deserialize(msTopic) as HashSet<Topic>;

                    _brokers.Add(brokerInfo);
                    _topics.UnionWith(topics);

                    _logger.LogInformation($"Broker [{brokerInfo.Name} -> {brokerInfo.Ip}:{brokerInfo.Port}] is reported ...");
                }
            }

            int offset = brokerBufferLength + topicBufferLength + 12;//12 = Instruct length byte + BrokerInfo length byte + Topics length byte

            ApartMessage(socket, memoryStream, offset);
        }

        #endregion

        #region IAdapterService methods

        public Task Start()
        {
            Startup();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_adapter != null && !_adapter.SafeHandle.IsClosed)
            {
                _adapter.Dispose();
                _adapter.Close();
            }

            return Task.CompletedTask;
        }

        #endregion

        private void TestSendTimer()
        {
            Timer timer = new Timer(1000 * 10);
            timer.Elapsed += (s, e) =>
            {
                byte[] replyBuffer = Encoding.UTF8.GetBytes("Adapter reply message...");
                List<Socket> clients = _clients.ToList();
                for (int i = 0; i < clients.Count; i++)
                {
                    using (var msBuffer = new MemoryStream())
                    {
                        msBuffer.Write(BitConverter.GetBytes(replyBuffer.Length));
                        msBuffer.Write(replyBuffer);


                        byte[] buffer = msBuffer.GetBuffer();
                        SocketAsyncEventArgs replyArgs = new SocketAsyncEventArgs();
                        replyArgs.SetBuffer(buffer, 0, buffer.Length);

                        try
                        {
                            if (clients[i].SendAsync(replyArgs)) { replyArgs.Dispose(); }
                        }
                        catch (NotSupportedException nse) { throw nse; }
                        catch (ObjectDisposedException oe) { throw oe; }
                        catch (SocketException se) { throw se; }
                        catch (Exception ex) { throw ex; }
                    }
                }
            };
            timer.Enabled = true;
        }
    }
}
