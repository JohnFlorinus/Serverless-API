using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Batproxy_Functions.Middleware
{
    public class RateLimitingMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly int _limit = 8;
        private readonly TimeSpan _window = TimeSpan.FromMinutes(1);
        private readonly ConcurrentDictionary<string, RequestCounter> _requests = new();

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var request = await context.GetHttpRequestDataAsync();
            var logger = context.GetLogger("RateLimiting");

            if (request == null)
            {
                await next(context);
                return;
            }

            string ip;
            if (request.Headers.TryGetValues("X-Forwarded-For", out var values))
            {
                ip = values.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim() ?? "unknown";
            }
            else
            {
                // Fallback: unique IP gen for testing - in production there is ALWAYS header with real IP
                ip = context.InvocationId.ToString();
            }

            var now = DateTime.UtcNow;
            var shouldBlock = false;
            int remaining = 0;
            var counter = _requests.GetOrAdd(ip, _ => new RequestCounter(now));
            lock (counter)
            {
                if (now - counter.Start >= _window)
                {
                    counter.Count = 1;
                    counter.Start = now;
                }
                else
                {
                    counter.Count++;
                }

                if (counter.Count > _limit)
                {
                    shouldBlock = true;
                }

                remaining = Math.Max(0, _limit - counter.Count);
            }

            if (shouldBlock)
            {
                var response = request.CreateResponse(System.Net.HttpStatusCode.TooManyRequests);
                await response.WriteStringAsync("Too many requests, please try again later.");
                context.GetInvocationResult().Value = response;
                return;
            }

            await next(context);
        }

        private class RequestCounter
        {
            public int Count;
            public DateTime Start;

            public RequestCounter(DateTime start)
            {
                Count = 1;
                Start = start;
            }
        }
    }
}
