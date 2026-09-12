using System.Data.Common;
using Manipulator.Api.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Manipulator.Api.Tests;

public class ScenesApiFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private const string ValidApiKey = "dev-api-key";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbContextDescriptor = services.SingleOrDefault(serviceDescriptor =>
                serviceDescriptor.ServiceType
                == typeof(IDbContextOptionsConfiguration<AppDbContext>)
            );
            if (dbContextDescriptor is not null)
                services.Remove(dbContextDescriptor);

            var dbConnectionDescriptor = services.SingleOrDefault(serviceDescriptor =>
                serviceDescriptor.ServiceType == typeof(DbConnection)
            );
            if (dbConnectionDescriptor is not null)
                services.Remove(dbConnectionDescriptor);

            // TODO: swap for sqlite provider once installed.
            var databaseName = Guid.NewGuid().ToString();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(databaseName)
            );

            // TODO: Requires sqlite entity framework provider installed.
            // services.AddSingleton<DbConnection>(_ =>
            // {
            //     var connection = new SqliteConnection("DataSource=:memory:");
            //     connection.Open();
            //
            //     return connection;
            // });
            //
            // services.AddDbContext<AppDbContext>(
            //     (container, options) =>
            //     {
            //         var connection = container.GetRequiredService<DbConnection>();
            //         options.UseSqlite(connection);
            //     }
            // );
        });

        builder.UseEnvironment("Development");
    }

    public HttpClient CreateAuthorizedClient(string apiKey = ValidApiKey)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }
}
