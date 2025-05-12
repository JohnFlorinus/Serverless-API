using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Batproxy_Functions.Middleware
{
    public class JwtMiddleware : IFunctionsWorkerMiddleware
    {

        private readonly JwtSecurityTokenHandler _handler;
        private readonly TokenValidationParameters _params;
        private readonly IConfiguration _config;

        public JwtMiddleware(JwtSecurityTokenHandler handler,
                             TokenValidationParameters validationParams,
                             IConfiguration config)
        {
            _handler = handler;
            _params = validationParams;
            _config = config;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var req = await context.GetHttpRequestDataAsync();
            if (req.Url.AbsolutePath.ToLower().Contains("public_"))
            {
                // Skip authentication for public-marked endpoints
                await next(context);
                return;
            }

            if (req.Headers.TryGetValues(HeaderNames.Cookie, out var cookieHeaders))
            {
                // HttpRequestData.Cookies is empty in isolated mode—parse header manually
                var raw = cookieHeaders.FirstOrDefault();
                var cookies = CookieHeaderValue.ParseList(new List<string> { raw });
                var token = cookies.FirstOrDefault(c => c.Name == "access_token")?.Value.ToString();
                if (!string.IsNullOrEmpty(token))
                {
                    context.Items["JwtToken"] = token;
                    try
                    {
                        var principal = _handler.ValidateToken(token, _params, out _);
                        context.Items["User"] = principal;
                    }
                    catch
                    {
                        var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
                        unauthorized.WriteString("Invalid or expired token");
                        context.GetInvocationResult().Value = unauthorized;
                        return;
                    }
                }
            }
            await next(context);
        }

        public async Task<string> GetJwtClaim(string token, string claimName)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var claimValue = jwtToken.Claims.FirstOrDefault(c => c.Type == claimName)?.Value;
            return claimValue;
        }

        public async Task<bool> TokenPass(FunctionContext context)
        {
            if (!context.Items.TryGetValue("User", out var u) ||
                !(u is ClaimsPrincipal user) ||
                !user.Identity.IsAuthenticated)
                    return false;

            // ingen pass om det har gått mer än tre dagar och shunon är frf på free trial
            if ((DateTime.UtcNow - Convert.ToDateTime(user.Claims.FirstOrDefault(c => c.Type == "CreatedDate").Value)) > TimeSpan.FromDays(3)
                && user.Claims.FirstOrDefault(c => c.Type == "Tier").Value.ToString().ToLower()=="freetrial")
                    return false;

            return true;
        }

        public async Task<string> TokenCreate(
            int AccountID,
            string Email,
            string Tier,
            bool TrialExpired,
            DateTime CreatedDate)
        {
            var claims = new List<Claim>
            {
        new Claim("AccountID", AccountID.ToString()),
        new Claim("Email", Email),
        new Claim("Tier", Tier),
        new Claim("TrialExpired", TrialExpired.ToString()),
        new Claim("CreatedDate", CreatedDate.ToString()),
            };
            var jwtToken = new JwtSecurityToken(
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddDays(30),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
                    SecurityAlgorithms.HmacSha256Signature));
            var jwtString = _handler.WriteToken(jwtToken);
            return jwtString;
        }
    }
}
