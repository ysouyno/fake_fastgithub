using DNS.Protocol;
using System.Net;
using System.Net.Sockets;

namespace fake_fastgithub
{
    sealed class DnsServer : IDisposable
    {
        private readonly RequestResolver requestResolver;
        private readonly ILogger<DnsServer> logger;
        private readonly Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private readonly byte[] buffer = new byte[ushort.MaxValue];

        public DnsServer(RequestResolver requestResolver, ILogger<DnsServer> logger)
        {
            this.requestResolver = requestResolver;
            this.logger = logger;
        }

        public void Bind(IPAddress address, int port)
        {
            if (OperatingSystem.IsWindows())
            {
                UdpTable.KillPortOwner(port);
            }

            if (LocalMachine.CanListenUdp(port) == false)
            {
                throw new Exception($"udp 端口 {port} 已经被其它进程占用");
            }

            if (OperatingSystem.IsWindows())
            {
                const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C); // ?
                socket.IOControl(SIO_UDP_CONNRESET, new byte[4], new byte[4]);
            }

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(address, port));
        }

        private async void HandleRequestAsync(byte[] datas, EndPoint remoteEndPoint, CancellationToken cancellationToken)
        {
            try
            {
                var request = Request.FromArray(datas);
                var remoteEndPointRequest = new RemoteEndPointRequest(request, remoteEndPoint);
                var response = await requestResolver.Resolve(remoteEndPointRequest, cancellationToken);
                await socket.SendToAsync(response.ToArray(), SocketFlags.None, remoteEndPoint);
            }
            catch (Exception ex)
            {
                logger.LogTrace($"处理 DNS 异常：{ex.Message}");
            }
        }

        public async Task ListenAsync(CancellationToken cancellationToken)
        {
            var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (cancellationToken.IsCancellationRequested == false)
            {
                try
                {
                    Console.WriteLine($"org remoteEndPoint: {remoteEndPoint}");
                    var result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, remoteEndPoint);
                    var datas = new byte[result.ReceivedBytes];
                    buffer.AsSpan(0, datas.Length).CopyTo(datas);
                    Console.WriteLine($"now remoteEndPoint: {result.RemoteEndPoint}");
                    HandleRequestAsync(datas, result.RemoteEndPoint, cancellationToken);
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.OperationAborted)
                {
                    Console.WriteLine("ListenAsync SocketError.OperationAborted");
                    break;
                }
            }
        }

        public void Dispose()
        {
            socket.Dispose();
        }
    }
}
