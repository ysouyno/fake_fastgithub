using Microsoft.AspNetCore.Connections;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace fake_fastgithub
{
    sealed class GithubSshHandler : ConnectionHandler
    {
        private readonly IDomainResolver domainResolver;
        private readonly DnsEndPoint githubSshEndPoint = new("ssh.github.com", 443);

        public GithubSshHandler(IDomainResolver domainResolver)
        {
            this.domainResolver = domainResolver;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var address = await domainResolver.ResolveAsync(githubSshEndPoint);
            using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(address, githubSshEndPoint.Port);
            var targetStream = new NetworkStream(socket, ownsSocket: false);

            var task1 = targetStream.CopyToAsync(connection.Transport.Output);
            var task2 = connection.Transport.Input.CopyToAsync(targetStream);
            await Task.WhenAny(task1, task2);
        }
    }
}
