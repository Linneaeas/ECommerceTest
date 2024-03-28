using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ECommerceBE.Database;

using Xunit;
using System.Data.Common;


public class ApplicationFactory<T> : WebApplicationFactory<T> where T : class
{
    // Denna metod anroppas automatiskt för varje test. 
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Byt ut den riktiga databasen mot en test databas. Här används Sqlite för att det är smidigt.
            var dbContextDescriptor = services.SingleOrDefault(
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

            // Byt ut riktig autentisering mot "fake" autentisering. Här används en klass som skapar fake tokens.
            // Se klassen längst ned för mer information.
            services.AddAuthentication("TestScheme")
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

            // Skapa / referera till det nya test-DbContext.
            var context = CreateDbContext(services);

            // Ta bort all data för varje test.
            context.Database.EnsureDeleted();
            // Skapa nya tabeller igen för varje test.
            context.Database.EnsureCreated();

            // Lägg in en användare som kan användas i testerna.
            // Den använder samma id som för autentiseringen så att de blir kopplade.
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
        // Skapa en instans av en egen User.
        var user = new User();
        // Hårdkoda email och id till något valfritt.
        user.Email = email;
        user.Id = email;

        // Spara användare till databas.
        db.Users.Add(user);

        // Registrera ändringar.
        db.SaveChanges();
    }
}

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // "AuthenticationHandler" kräver lite information som vi skickar vidare.
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    // Skapa en fake-token med en användare som har id:t "my-user-id".
    // Den kopplas till användaren som läggs in i databasen längre upp.
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
