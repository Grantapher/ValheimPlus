using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ValheimPlus.Http
{
    public static class HttpHelper
    {
        public static string DownloadString(string url, string userAgent, TimeSpan? timeout = null, Func<HttpClient> clientFactory = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("url is null or empty", nameof(url));

            // Ensure TLS 1.2 on older runtimes
            try
            {
#pragma warning disable SYSLIB0039
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
#pragma warning restore SYSLIB0039
            }
            catch { /* ignore on runtimes where this isn't applicable */ }

            // Prefer HttpClient (better cross-platform support under Mono/Unity)
            try
            {
                HttpClient client = null;
                HttpClientHandler handler = null;
                var disposeClient = false;

                if (clientFactory != null)
                {
                    client = clientFactory();
                }
                else
                {
                    handler = new HttpClientHandler { AllowAutoRedirect = true };
                    client = new HttpClient(handler);
                    disposeClient = true;
                }

                try
                {
                    client.Timeout = timeout ?? TimeSpan.FromSeconds(20);
                    client.DefaultRequestHeaders.UserAgent.Clear();
                    try
                    {
                        var ua = string.IsNullOrWhiteSpace(userAgent) ? "ValheimPlus/Client" : SanitizeUserAgent(userAgent);
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
                    }
                    catch
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("ValheimPlus/Client");
                    }
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.ParseAdd("*/*");

                    var response = client.GetAsync(url).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();
                    return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                }
                finally
                {
                    if (disposeClient)
                    {
                        client.Dispose();
                        handler?.Dispose();
                    }
                }
            }
            catch (Exception httpEx)
            {
                // Fallback to WebClient for edge environments
                try
                {
                    using (var wc = new WebClient())
                    {
                        if (!string.IsNullOrWhiteSpace(userAgent))
                        {
                            wc.Headers.Add("User-Agent", userAgent);
                        }
                        return wc.DownloadString(url);
                    }
                }
                catch (Exception wcEx)
                {
                    // Surface the original exception, include fallback for diagnostics
                    throw new InvalidOperationException($"HTTP download failed for '{url}'. Primary: {httpEx.GetType().Name}: {httpEx.Message}. Fallback: {wcEx.GetType().Name}: {wcEx.Message}");
                }
            }
        }
        public static string SanitizeUserAgent(string ua)
        {
            // Convert to a simple safe UA token (Product/Version)
            // Remove spaces and invalid characters
            var cleaned = ua.Replace(" ", "/");
            foreach (var ch in new[] { '(', ')', '<', '>', '@', ',', ';', ':', '\\', '"', '\'', '[', ']', '?', '=', '{', '}', ' ', '\t' })
            {
                cleaned = cleaned.Replace(ch.ToString(), "");
            }
            if (!cleaned.Contains("/")) cleaned += "/1.0";
            return cleaned;
        }
    }
}
