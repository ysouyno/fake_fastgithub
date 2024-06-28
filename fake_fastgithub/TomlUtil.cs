using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;

namespace fake_fastgithub
{
    static class TomlUtil
    {
        public static async Task<bool> SetAsync(string tomlPath, string key, object? value,
            CancellationToken cancellationToken = default)
        {
            var setted = false;
            var builder = new StringBuilder();
            var lines = await File.ReadAllLinesAsync(tomlPath, cancellationToken);

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, @$"(?<=#*\s*){key}(?=\s*=)") == false)
                {
                    builder.AppendLine(line);
                }
                else if (setted == false)
                {
                    setted = true;
                    builder.Append(key).Append(" = ").AppendLine(value?.ToString());
                }
            }

            var toml = builder.ToString();
            await File.WriteAllTextAsync(tomlPath, toml, cancellationToken);
            return setted;
        }

        public static Task<bool> SetListenAsync(string tomlPath, IPEndPoint endPoint,
            CancellationToken cancellationToken = default)
        {
            return SetAsync(tomlPath, "listen_addresses", $"['{endPoint}']", cancellationToken);
        }

        /// <summary>
        /// 获取公网 ip
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static async Task<IPAddress> GetPublicIPAddressAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3d) };
            var response = await httpClient.GetStringAsync("https://pv.sohu.com/cityjson?ie=utf-8", cancellationToken);
            var match = Regex.Match(response, @"\d+\.\d+\.\d+\.\d+");
            return match.Success && IPAddress.TryParse(match.Value, out var address)
                ? address
                : throw new Exception("无法获取外网 ip");
        }

        public static async Task<bool> SetEdnsClientSubnetAsync(string tomlPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var address = await GetPublicIPAddressAsync(cancellationToken);
                return await SetAsync(tomlPath, "edns_client_subnet", @$"[""{address}/32""]", cancellationToken);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
