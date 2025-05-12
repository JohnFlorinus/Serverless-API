using Batproxy_API.Repository.Helpers;
using Batproxy_API.Repository.Repos;
using Batproxy_API.Repository.Services;
using Batproxy_Functions.Data;
using Batproxy_Functions.Middleware;
using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker => {
        worker.UseMiddleware<RateLimitingMiddleware>();
        worker.UseMiddleware<JwtMiddleware>();
    })
    .ConfigureServices(async services =>
    {
        // Bind JWT settings from configuration
        services.AddSingleton<TokenValidationParameters>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            return new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidIssuer = cfg["Jwt:Issuer"],
                ValidateAudience = false,
                ValidAudience = cfg["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                                             Encoding.UTF8.GetBytes(cfg["Jwt:Key"])),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddSingleton<JwtSecurityTokenHandler>();
        services.AddScoped<AccountRepo>();
        services.AddScoped<AccountService>();
        services.AddScoped<EncryptionHelper>();
        services.AddScoped<MailHelper>();

        services.AddDbContext<SQLContext>(options =>
        options.UseSqlServer(@"Data Source=localhost;Initial Catalog=BatproxyDB;Integrated Security=SSPI;TrustServerCertificate=True;"));

        var file = Path.Combine(AppContext.BaseDirectory, "disposable_email_blocklist.conf");
        var domains = await File.ReadAllLinesAsync(file);
        services.AddSingleton(new HashSet<string>(domains, StringComparer.OrdinalIgnoreCase));
    })
    .Build();

host.Run();