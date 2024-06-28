using DNS.Client.RequestResolver;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;
using System.Net;

namespace fake_fastgithub
{
    sealed class RequestResolver : IRequestResolver
    {
        private readonly TimeSpan ttl = TimeSpan.FromSeconds(1d);
        private readonly FastGithubConfig fastGithubConfig;

        public RequestResolver(FastGithubConfig fastGithubConfig)
        {
            this.fastGithubConfig = fastGithubConfig;
        }

        public async Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = default)
        {
            var response = Response.FromRequest(request);
            if (request is not RemoteEndPointRequest remoteEndPointRequest)
            {
                return response;
            }

            var question = request.Questions.FirstOrDefault();
            if (question == null || question.Type != RecordType.A)
            {
                return response;
            }

            // 解析匹配的域名指向本机 ip
            var domain = question.Name;
            if (fastGithubConfig.IsMatch(domain.ToString()))
            {
                Console.WriteLine($"matched: {domain}");
                var localAddress = remoteEndPointRequest.GetLocalIPAddress() ?? IPAddress.Loopback;
                var record = new IPAddressResourceRecord(domain, localAddress, ttl);
                response.AnswerRecords.Add(record);
                return response;
            }

            var fastResolver = new UdpRequestResolver(fastGithubConfig.FastDns);
            return await fastResolver.Resolve(request, cancellationToken);
        }
    }
}
