using Garbati.ProductApi.Authorization;
using Garbati.ProductApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Garbati.ProductApi.Tests.Fixtures;

// Runs the real app in memory (WebApplicationFactory) against its own Postgres container, so requests go
// through the full pipeline: migrations (including the ProductLimitPolicy placeholder seed), correlation
// id, authentication, authorization, controllers and the database. This service calls no other service
// (DESIGN.md Section 1: "no door to LedgerApi and no door to IdentityApi"), so unlike OrchestratorApi's
// ApiFixture there are no downstream clients to fake here.
public class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    // The seed migration's own placeholder ProductLimitPolicy rows (Migrations/
    // *SeedPlaceholderProductLimitPolicies*) - never deleted by ResetAsync, same as StaffIdentityApi's
    // own seeded Administrator row.
    public const string SeededSavingsProductCode = "NG-SAV-STD";
    public const string SeededCurrentProductCode = "NG-CUR-STD";

    // For every scope there is a client holding only that scope, and one holding every scope except it.
    public static string OnlyClient(string scope) => $"only-{scope}";
    public static string AllButClient(string scope) => $"all-but-{scope}";
    public static string KeyFor(string clientId) => $"test-key-{clientId}";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var ctx = CreateContext())
        {
            await ctx.Database.MigrateAsync();
        }

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ProductDb"] = _container.GetConnectionString(),
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["Swagger:Enabled"] = "true",
        };

        var clients = ProductScopes.All
            .SelectMany(scope => new[]
            {
                (Id: OnlyClient(scope), Scopes: new[] { scope }),
                (Id: AllButClient(scope), Scopes: ProductScopes.All.Where(s => s != scope).ToArray()),
            })
            .ToList();

        for (var i = 0; i < clients.Count; i++)
        {
            settings[$"ApiKeys:Clients:{i}:ClientId"] = clients[i].Id;
            settings[$"ApiKeys:Clients:{i}:KeyHash"] = Convert.ToBase64String(ApiKeyHasher.Hash(KeyFor(clients[i].Id)));
            for (var j = 0; j < clients[i].Scopes.Length; j++)
            {
                settings[$"ApiKeys:Clients:{i}:Scopes:{j}"] = clients[i].Scopes[j];
            }
        }

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));
        });
    }

    public ProductDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new ProductDbContext(options);
    }

    // An HTTP client for the in-memory app, sending the given client's key (or no key when null).
    public HttpClient CreateClient(string? clientId)
    {
        var client = Factory.CreateClient();
        if (clientId is not null)
        {
            client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, KeyFor(clientId));
        }
        return client;
    }

    // Clears everything a test might have created, but keeps the rows no test creates and no test
    // should have to recreate: the seeded ProductLimitPolicy placeholders (FeeRule/ProfitSharingTerms
    // are never touched by ResetAsync either - they should stay empty for the lifetime of the fixture,
    // which the emptiness tests themselves assert).
    public async Task ResetAsync()
    {
        await using var context = CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM product_audit_logs; DELETE FROM account_product_bindings; DELETE FROM products;");
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<ApiFixture>;
