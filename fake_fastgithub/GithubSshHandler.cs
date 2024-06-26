using Microsoft.AspNetCore.Connections;

namespace fake_fastgithub
{
    sealed class GithubSshHandler : ConnectionHandler
    {
        public override Task OnConnectedAsync(ConnectionContext connection)
        {
            throw new NotImplementedException();
        }
    }
}
