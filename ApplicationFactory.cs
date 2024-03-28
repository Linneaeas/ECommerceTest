using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Security.Claims;
using ECommerceBE.Database;
using System.Data.Common;

public class ApplicationFactory<T> : WebApplicationFactory<T> where T : class
{

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {

            var dbContextDescriptor = services.SingleOrDefault
            (
                 d => d.ServiceType == typeof(DbContextOptions<MyDbContext>)
            );

            services.Remove(dbContextDescriptor);

            var dbConnDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbConnection)
            );
            services.Remove(dbConnDescriptor);


            services.AddDbContext<MyDbContext>(options =>
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                options.UseSqlite($"Data Source={Path.Join(path, "MyAppTests.db")}");
            });

            services.AddAuthentication("TestScheme")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            var context = CreateDbContext(services);

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            CreateUser(context, "my-user-id");
        });
    }

    private static MyDbContext CreateDbContext(IServiceCollection services)
    {

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MyDbContext>();
        return context;
    }

    private static void CreateUser(MyDbContext db, string email)
    {
        var user = new User();
        user.Email = email;
        user.Id = email;

        db.Users.Add(user);
        db.SaveChanges();
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.Name, "my-user"),
            new Claim(ClaimTypes.NameIdentifier, "my-user-id")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);
        return Task.FromResult(result);
    }
}
