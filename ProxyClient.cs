using Polly;
using Polly.Retry;
using System;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Wrap;
using Microsoft.AspNetCore.Mvc;

namespace Polly_Test
{
    public class ProxyClient:ControllerBase
    {
        private readonly AsyncFallbackPolicy<IActionResult> _fallbackPolicy;
        private readonly HttpClient _httpClient;

        public ProxyClient(IHttpClientFactory httpClientFactory)
        {
            _fallbackPolicy = Policy<IActionResult>
                .Handle<Exception>()
                .FallbackAsync(Content("Sorry, we are currently experiencing issues. Please try again later"));
            _httpClient = httpClientFactory.CreateClient();
        }

    }
}
