using Azure.Core;
using Batproxy_API.Repository.Services;
using Batproxy_Functions.Data.DTOs;
using Batproxy_Functions.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;  // for QueryHelpers

namespace Batproxy_Functions.Functions;

public class AccountFunctions
{
    private readonly JwtMiddleware _jwt;
    private readonly AccountService _accountService;

    public AccountFunctions(JwtMiddleware jwtMiddleware, AccountService accountService)
    {
        _jwt = jwtMiddleware;
        _accountService = accountService;
    }

    [Function("public_AccountRegister")]
    public async Task<HttpResponseData> Register(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var payload = await req.ReadFromJsonAsync<RegisterDTO>();
            if (payload == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid JSON");
                return bad;
            }

            string clientIp = "unknown";
            if (req.Headers.TryGetValues("X-Forwarded-For", out var headerValues))
            {
                // header might be "client, proxy1, proxy2"
                clientIp = headerValues
                    .SelectMany(v => v.Split(','))
                    .FirstOrDefault()?.Trim()
                    ?? clientIp;
            }

            await _accountService.Register(payload.Email, payload.Password, payload.Tier, clientIp);

            return req.CreateResponse(HttpStatusCode.Created);
        }
        catch(Exception ex)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message);
            return bad;
        }
    }

    [Function("public_AccountLogin")]
    public async Task<HttpResponseData> Login(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            var payload = await req.ReadFromJsonAsync<LoginDTO>();
            if (payload == null)
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Invalid JSON");
                return bad;
            }

            string jwt = await _accountService.Login(payload.Email,payload.Password);

            var res = req.CreateResponse(HttpStatusCode.Created);
            res.Headers.Add("Set-Cookie",
                $"access_token={jwt}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age=2592000"); // 2592000 sek = 30 dagar

            return res;
        }
        catch (Exception ex)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message);
            return bad;
        }
    }

    [Function("public_AccountVerify")]
    public async Task<HttpResponseData> Verify(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            QueryHelpers.ParseQuery(req.Url.Query).TryGetValue("token", out var tokenValues);
            string token = tokenValues.FirstOrDefault();

            string jwt = await _accountService.Verify(token);

            var res = req.CreateResponse(HttpStatusCode.OK);
            res.Headers.Add("Set-Cookie",
                $"access_token={jwt}; HttpOnly; Secure; SameSite=None; Path=/; Max-Age=2592000"); // 2592000 sek = 30 dagar

            return res;
        }
        catch (Exception ex)
        {
            var bad = req.CreateResponse(HttpStatusCode.BadRequest);
            await bad.WriteStringAsync(ex.Message);
            return bad;
        }
    }

    [Function("public_AccountTest")]
    public async Task<HttpResponseData> Test(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteStringAsync("yo");
            return res;
    }

    [Function("AccountLogout")]
    public async Task<HttpResponseData> Logout(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")]
        HttpRequestData req,
        FunctionContext context)
    {
        if (!await _jwt.TokenPass(context))
            return req.CreateResponse(HttpStatusCode.Unauthorized);

        var res = req.CreateResponse(System.Net.HttpStatusCode.OK);
        // Delete cookie by resetting it with Max-Age=0
        res.Headers.Add("Set-Cookie",
            $"access_token=; HttpOnly; Secure; SameSite=None; Path=/; Max-Age=0");
        return res;
    }
}