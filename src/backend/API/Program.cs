using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using API.Data;
using System.Diagnostics;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    // Explicitly add configuration sources
    .ConfigureAppConfiguration((context, config) => 
    {
        // Make sure local.settings.json is loaded properly
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) => {
        // Debug connection string 
        Console.WriteLine("Available configuration keys:");
        foreach (var key in context.Configuration.AsEnumerable())
        {
            Console.WriteLine($"Key: {key.Key}, Value: {key.Value ?? "[null]"}");
        }
        
        // Try to get connection string (with fallback methods)
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        
        // Alternative ways to access the connection string if standard method fails
        if (string.IsNullOrEmpty(connectionString))
        {
            // Try direct access to ConnectionStrings section
            connectionString = context.Configuration["ConnectionStrings:DefaultConnection"];
            Console.WriteLine($"Tried direct access: {connectionString ?? "[null]"}");
        }

        // Try environment variable format (Azure Functions transforms ConnectionStrings to this format)
        if (string.IsNullOrEmpty(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            Console.WriteLine($"Tried environment variable format: {connectionString ?? "[null]"}");
        }
        
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string 'DefaultConnection' not found.");
        }

        Console.WriteLine($"Using connection string: {connectionString.Substring(0, Math.Min(20, connectionString.Length))}...");

        services.AddDbContext<ProjectContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions => 
            {
                npgsqlOptions.EnableRetryOnFailure(5);
                npgsqlOptions.CommandTimeout(30);
            }));
    })
    .Build();

// Run the function app host
host.Run();
