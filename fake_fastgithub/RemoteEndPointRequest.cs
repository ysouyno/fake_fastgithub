using DNS.Protocol;
using System.Net;

namespace fake_fastgithub
{
    sealed class RemoteEndPointRequest : Request
    {
        public EndPoint RemoteEndPoint { get; }

        public RemoteEndPointRequest(Request request, EndPoint remoteEndPoint)
            : base(request)
        {
            RemoteEndPoint = remoteEndPoint;
        }

        public IPAddress? GetLocalIPAddress()
        {
            return LocalMachine.GetLocalIPAddress(RemoteEndPoint);
        }
    }
}
