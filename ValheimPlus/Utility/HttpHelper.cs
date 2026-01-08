using System;
using System.Net;
using System.Net.Http;

namespace ValheimPlus.Http
{
    public static class HttpHelper
    {
        private const string UserAgent = "ValheimPlusClient/1.0";

        public static string DownloadString(string url, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("url is null or empty", nameof(url));

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"Invalid URL scheme: '{url}'", nameof(url));
            }

            // Ensure TLS 1.2 on older runtimes
            try
            {
#pragma warning disable SYSLIB0039
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#pragma warning restore SYSLIB0039
            }
            catch { /* ignore on runtimes where this isn't applicable */ }

            using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
            using (var client = new HttpClient(handler))
            {
                client.Timeout = timeout ?? TimeSpan.FromSeconds(20);
                client.DefaultRequestHeaders.UserAgent.Clear();
                client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.ParseAdd("*/*");

                var response = client.GetAsync(uri).GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
                return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
        }
    }
}
