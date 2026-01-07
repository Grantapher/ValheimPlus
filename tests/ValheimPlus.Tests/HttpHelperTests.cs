using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ValheimPlus.Http;

namespace ValheimPlus.Tests
{
    public class HttpHelperTests
    {
        private class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public HttpRequestMessage LastRequest { get; private set; }

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_responder(request));
            }
        }

        [Test]
        public void DownloadString_UsesClientFactory_ReturnsBody()
        {
            var handler = new StubHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
            var client = new HttpClient(handler);

            string result = HttpHelper.DownloadString("https://unit.test/", "ValheimPlus/Test", TimeSpan.FromSeconds(5), () => client);
            Assert.AreEqual("ok", result);
        }

        [Test]
        public void DownloadString_SetsUserAgentHeader()
        {
            var handler = new StubHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
            var client = new HttpClient(handler);

            string result = HttpHelper.DownloadString("https://unit.test/", "Valheim Plus Test Agent", TimeSpan.FromSeconds(5), () => client);
            Assert.AreEqual("ok", result);
            // Ensure UA header was set (sanitized)
            StringAssert.Contains("ValheimPlus", handler.LastRequest.Headers.UserAgent.ToString());
        }

        [Test]
        public void DownloadString_DefaultsUserAgentWhenMissing()
        {
            var handler = new StubHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok")
            });
            var client = new HttpClient(handler);

            string result = HttpHelper.DownloadString("https://unit.test/", null, TimeSpan.FromSeconds(5), () => client);
            Assert.AreEqual("ok", result);
            StringAssert.Contains("ValheimPlus/Client", handler.LastRequest.Headers.UserAgent.ToString());
        }

        [Test]
        public void SanitizeUserAgent_ProducesValidProductToken()
        {
            var sanitized = HttpHelper.SanitizeUserAgent("My Agent (Dev)");
            StringAssert.DoesNotContain(" ", sanitized);
            StringAssert.DoesNotContain("(", sanitized);
            StringAssert.Contains("/", sanitized);
        }

        [Test]
        public void SanitizeUserAgent_AppendsVersionWhenMissingSlash()
        {
            var sanitized = HttpHelper.SanitizeUserAgent("AgentNoSlash");
            StringAssert.Contains("AgentNoSlash/1.0", sanitized);
        }

        [Test]
        public void DownloadString_WhenPrimaryAndFallbackFail_ThrowsCombined()
        {
            var ex = Assert.Throws<InvalidOperationException>(() =>
                HttpHelper.DownloadString("httpz://invalid", "UA", TimeSpan.FromSeconds(1), () => throw new HttpRequestException("boom")));

            StringAssert.Contains("Primary: HttpRequestException", ex.Message);
            StringAssert.Contains("Fallback:", ex.Message);
        }
    }
}
