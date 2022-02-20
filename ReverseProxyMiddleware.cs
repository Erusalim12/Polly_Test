using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Polly_Test
{
    public class ReverseProxyMiddleware : IMiddleware
    {
        private readonly HttpClient _httpClient;
        private readonly QueryOptions options;
        private readonly RequestDelegate _next;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly int _maxRetry = 5;

        private readonly IEnumerable<string> IgnoreHeaders = new[]
{
            HttpRequestHeader.Authorization.ToString(),
            HttpRequestHeader.Host.ToString()
        };
        public ReverseProxyMiddleware(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _retryPolicy = Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(_maxRetry, times =>
                 TimeSpan.FromMinutes(times * 10)
                );
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {

            options = ReadIpChangerOptions(context.Request);
            try
            {
                var request = CreateRequestMessage(context.Request);
                await ExecuteRequest(request, context);
            }
            catch (Exception e)
            {
                await _next(context);
                Log.Information(e, $"ошибка при выполнении {nameof(ReverseProxyMiddleware)}");

            }
        }
        private QueryOptions ReadIpChangerOptions(HttpRequest request)
        {
            QueryOptions options = new QueryOptions();
            StringValues value;

            if (request.Headers.TryGetValue("IpChanger-IpRange", out value))
            {
                switch (value)
                {
                    case "local":
                    default:
                        options.UseLocalIp = true;
                        break;
                }
            }

            if (request.Headers.TryGetValue("IpChanger-EnableBuffering", out value))
            {
                options.EnableBuffering = value == "true";
            }


            return options;
        }

        private void CopyRequestHeaders(HttpRequest original, HttpRequestMessage copy)
        {
            foreach (var header in original.Headers)
            {
                if (IgnoreHeaders.Contains(header.Key))
                    continue;

                if (!copy.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                    copy.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        private HttpRequestMessage CreateRequestMessage(HttpRequest request)
        {
            var message = new HttpRequestMessage
            {
                Method = new HttpMethod(request.Method),
                RequestUri = new Uri(request.Path)
            };

            CopyRequestHeaders(request, message);
            CopyRequestBody(request, message);

            return message;
        }

        private void CopyRequestBody(HttpRequest original, HttpRequestMessage copy)
        {
            if (!HasBody(original.Method))
                return;

            copy.Content = new StreamContent(original.Body);
        }

        private bool HasBody(string method)
        {
            return !new[] { "GET", "HEAD", "DELETE", "TRACE" }
                .Any(m => string.Equals(method, m, StringComparison.OrdinalIgnoreCase));
        }

        private async Task ExecuteRequest(HttpRequestMessage message, HttpContext context)
        {

            await _retryPolicy.ExecuteAsync(async () =>
          {
              var response = await GetResponse(message, context.RequestAborted);

              CopyStatusCode(response, context.Response);
              CopyResponseHeaders(response, context.Response);

              await CopyResponseBody(response, context.Response);

          });

        }

        private async Task<HttpResponseMessage> GetResponse(
            HttpRequestMessage message, CancellationToken cancellation)
        {

            return await _httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellation);

        }

        private void CopyStatusCode(HttpResponseMessage original, HttpResponse copy)
        {
            copy.StatusCode = (int)original.StatusCode;
        }

        private void CopyResponseHeaders(HttpResponseMessage original, HttpResponse copy)
        {
            foreach (var header in original.Headers)
                copy.Headers[header.Key] = header.Value.ToArray();

            foreach (var header in original.Content.Headers)
                copy.Headers[header.Key] = header.Value.ToArray();

            copy.Headers.Remove("transfer-encoding");
        }

        private async Task CopyResponseBody(HttpResponseMessage original, HttpResponse copy)
        {
            await original.Content.CopyToAsync(copy.Body);
        }

    }
}
