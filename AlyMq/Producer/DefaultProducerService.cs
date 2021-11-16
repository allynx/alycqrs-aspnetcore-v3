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

namespace AlyMq.Producer
{
    public class DefaultProducerService : IProducerService
    {
        private Socket _adapter;
        private Socket _producer;
        private readonly HashSet<Socket> _clients;
        private readonly ILogger<DefaultProducerService> _logger;

        public DefaultProducerService(ILogger<DefaultProducerService> logger)
        {
            _logger = logger;
            _clients = new HashSet<Socket>();
        }

        private void Startup()
        {
            ProducerListen();
            AdapterConnect();
            TestSendTimer();
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


        private void ApartMessage(Socket socket, MemoryStream ms, int offset)
        {
            if (ms.Length > offset)
            {
                ms.Seek(offset, SeekOrigin.Begin);

                byte[] lengthBuffer = new byte[4];
                ms.Read(lengthBuffer, 0, 4);
                int length = BitConverter.ToInt32(lengthBuffer);

                byte[] strBuffer = new byte[length];
                ms.Read(strBuffer, 0, length);

                _logger.LogInformation($"Producer received message [{Encoding.UTF8.GetString(strBuffer)}]");
            }
        }


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

        private void TestSendTimer()
        {
            Timer timer = new Timer(1000 * 10);
            timer.Elapsed += (s, e) =>
            {
                //if (_adapter != null && !_adapter.SafeHandle.IsClosed)
                //{
                //    byte[] reportBuffer = Encoding.UTF8.GetBytes("Producer report message...");
                //    using (var msBuffer = new MemoryStream())
                //    {
                //        msBuffer.Write(BitConverter.GetBytes(reportBuffer.Length));
                //        msBuffer.Write(reportBuffer);

                //        byte[] reportMsgBuffer = msBuffer.GetBuffer();

                //        SocketAsyncEventArgs reportArgs = new SocketAsyncEventArgs();
                //        reportArgs.SetBuffer(reportMsgBuffer, 0, reportMsgBuffer.Length);

                //        try
                //        {
                //            if (_adapter.SendAsync(reportArgs)) { reportArgs.Dispose(); }
                //        }
                //        catch (NotSupportedException nse) { throw nse; }
                //        catch (ObjectDisposedException oe) { throw oe; }
                //        catch (SocketException se) { throw se; }
                //        catch (Exception ex) { throw ex; }
                //    }
                //}

                //byte[] replyBuffer = Encoding.UTF8.GetBytes("Producer reply message...");

                //foreach (Socket client in _clients)
                //{
                //    using (var msBuffer = new MemoryStream())
                //    {
                //        msBuffer.Write(BitConverter.GetBytes(replyBuffer.Length));
                //        msBuffer.Write(replyBuffer);

                //        byte[] replyMsgBuffer = msBuffer.GetBuffer();

                //        SocketAsyncEventArgs replyArgs = new SocketAsyncEventArgs();
                //        replyArgs.SetBuffer(replyMsgBuffer, 0, replyBuffer.Length);

                //        try
                //        {
                //            if (client.SendAsync(replyArgs)) { replyArgs.Dispose(); };
                //        }
                //        catch (NotSupportedException nse) { throw nse; }
                //        catch (ObjectDisposedException oe) { throw oe; }
                //        catch (SocketException se) { throw se; }
                //        catch (Exception ex) { throw ex; }
                //    }
                //}
            };
            timer.Enabled = true;
        }
    }
}
