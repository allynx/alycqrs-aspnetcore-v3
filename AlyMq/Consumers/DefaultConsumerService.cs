﻿using AlyMq.Consumers.Configuration;
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
using AlyMq.Brokers;
using System.Text.Json;

namespace AlyMq.Consumers
{
    public class DefaultConsumerService : IConsumerService
    {
        private Socket _adapter;
        private Socket _consumer;
        private readonly HashSet<Topic> _topics;
        private readonly HashSet<Broker> _borkers;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultConsumerService> _logger;

        public DefaultConsumerService(ILogger<DefaultConsumerService> logger)
        {
            _logger = logger;
            _topics = new HashSet<Topic>();
            _borkers = new HashSet<Broker>();
            _clients = new HashSet<Socket>();
        }

        private void Startup()
        {
            InitDefault();
            ConsumerListen();
            AdapterConnect();
            PullBrokerTimer();
        }

        private void ConsumerListen()
        {
            try
            {
                _consumer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new(IPAddress.Parse(ConsumerConfig.Instance.Address.Ip), ConsumerConfig.Instance.Address.Port);
                Listen(_consumer, ipEndPoint, ConsumerConfig.Instance.Address.Backlog);
            }
            catch (ArgumentNullException aue) { throw aue; }
            catch (FormatException fe) { throw fe; }
        }

        private void AdapterConnect()
        {
            try
            {
                _adapter = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipEndPoint = new(IPAddress.Parse(ConsumerConfig.Instance.AdapterAddress.Ip), ConsumerConfig.Instance.AdapterAddress.Port);
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
                _logger.LogInformation("Client {arg} is accepted ...", args.AcceptSocket.RemoteEndPoint);

                _clients.Add(args.AcceptSocket);
                Receive(args.AcceptSocket);
                Accept(socket, args);
            }
            else
            {
                //When the monitoring service is actively closed, a callback will be triggered here

                _logger.LogInformation("Service listenting is closed ...");

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
                SocketAsyncEventArgs args = new();
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
                string remoteEndPoint = socket.RemoteEndPoint.ToString();
                _logger.LogInformation("Server {arg} is connected ...", remoteEndPoint);

                Receive(socket);
            }
            else
            {
                _logger.LogInformation("Connect to remote server is failed ...");

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
                    case Instruct.PullBrokers:
                        ApartPullBrokerFromAdapter(socket, ms);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApartPullBrokerFromAdapter(Socket socket, MemoryStream memoryStream)
        {

            byte[] brokersLengthBuffer = new byte[4];
            memoryStream.Read(brokersLengthBuffer, 0, 4);
            int brokersLength = BitConverter.ToInt32(brokersLengthBuffer);

            byte[] brokersBuffer = new byte[brokersLength];
            memoryStream.Read(brokersBuffer, 0, brokersLength);

            HashSet<Broker> brokers = JsonSerializer.Deserialize<HashSet<Broker>>(brokersBuffer);
            _borkers.UnionWith(brokers);

            _logger.LogInformation("Consumer pull brokers by topic keys form adapter {arg} is success ...", _adapter.RemoteEndPoint);

            int offset = brokersLength + 8;//12 = Instruct of byte + brokers length of byte

            ApartMessage(socket, memoryStream, offset);
        }

        private void PullBrokerTimer()
        {
            Timer timer = new(30000);
            timer.Elapsed += (s, e) =>
            {
                PullBroker();
            };
            timer.Enabled = true;
        }

        private void PullBroker()
        {
            if (_adapter != null && !_adapter.SafeHandle.IsClosed)
            {
                using var msBuffer = new MemoryStream();
                IEnumerable<Guid> topicKeys = _topics.Select(s => s.Key);
                byte[] topicKeysBuffer = JsonSerializer.SerializeToUtf8Bytes(topicKeys.ToHashSet());

                msBuffer.Write(BitConverter.GetBytes(Instruct.PullBrokers));
                msBuffer.Write(BitConverter.GetBytes(topicKeysBuffer.Length));
                msBuffer.Write(topicKeysBuffer);

                byte[] buffer = msBuffer.GetBuffer();
                SocketAsyncEventArgs args = new();
                args.SetBuffer(buffer, 0, buffer.Length);

                try
                {
                    if (_adapter.SendAsync(args)) { SendCallback(_adapter, args); }
                    _logger.LogInformation("Consumer create pull brokers by topic keys form adapter {arg} ...", _adapter.RemoteEndPoint);
                }
                catch (ArgumentException ae) { throw ae; }
                catch (ObjectDisposedException ode) { throw ode; }
                catch (InvalidOperationException ioe) { throw ioe; }
                catch (NotSupportedException nse) { throw nse; }
                catch (SocketException se) { throw se; }

            }
        }

        #region Init broker default

        private void InitDefault()
        {
            IConfiguration config = new ConfigurationBuilder()
              .AddJsonFile("consumerconfig.json", true, true)
              .Build();

            config.Bind("Topics", _topics);
        }

        #endregion

        #region IConsumerService methods

        public Task Start()
        {
            Startup();
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (_consumer != null && !_consumer.SafeHandle.IsClosed)
            {
                _consumer.Dispose();
                _consumer.Close();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
