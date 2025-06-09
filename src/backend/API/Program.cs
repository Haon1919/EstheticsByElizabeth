// Required Usings for Azure Functions Isolated Process
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Needed for LogLevel and LogTo
using Microsoft.EntityFrameworkCore; // Needed for EF Core configuration
using API.Data; // Your DbContext namespace
using Npgsql; // Needed for UseNpgsql and options
using System; // Needed for TimeSpan, StringComparison etc.
using System.Text.Json; // Needed for JsonSerializerOptions
using System.Text.Json.Serialization; // Needed for ReferenceHandler and JsonIgnoreCondition

Console.WriteLine("🔧 [STARTUP] Configuring Azure Functions Host (Isolated Process Mode)");

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWorkerDefaults(builder => {
        
        // Configure JSON options for Functions Worker
        builder.Services.Configure<JsonSerializerOptions>(options =>
        {
            options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        
        // Configure JSON options for ASP.NET Core HTTP responses
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    })    .ConfigureAppConfiguration((context, config) =>
    {
        // Load configuration from local settings file
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) => {
        // --- Log Configuration Structure ---
        Console.WriteLine("📋 [STARTUP] Configuration Structure:");
        Console.WriteLine($"📁 [CONFIG] FUNCTIONS_WORKER_RUNTIME: {context.Configuration["Values:FUNCTIONS_WORKER_RUNTIME"] ?? "[not set]"}");
        Console.WriteLine($"📁 [CONFIG] ConnectionStrings:DefaultConnection: {(string.IsNullOrEmpty(context.Configuration["Values:ConnectionStrings:DefaultConnection"]) ? "[not set]" : "[configured]")}");
        Console.WriteLine($"📁 [CONFIG] AzureWebJobsStorage: {(string.IsNullOrEmpty(context.Configuration["Values:AzureWebJobsStorage"]) ? "[not set]" : "[configured]")}");
        
        // --- Check if local.settings.json is being loaded ---
        Console.WriteLine("🔍 [DEBUG] Checking local.settings.json loading:");
        Console.WriteLine($"🏠 [CONFIG] Host:CORS from config: {context.Configuration["Host:CORS"] ?? "[not set]"}");
        Console.WriteLine($"🏠 [CONFIG] Host:LocalHttpPort from config: {context.Configuration["Host:LocalHttpPort"] ?? "[not set]"}");
        Console.WriteLine($"🌍 [ENV] Host__CORS environment variable: {Environment.GetEnvironmentVariable("Host__CORS") ?? "[not set]"}");
        Console.WriteLine($"🌍 [ENV] ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "[not set]"}");
        Console.WriteLine($"🌍 [ENV] AZURE_FUNCTIONS_ENVIRONMENT: {Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "[not set]"}");
        
        // Check all configuration providers
        Console.WriteLine("🔍 [DEBUG] Configuration providers:");
        foreach (var provider in context.Configuration.AsEnumerable().Take(30))
        {
            if (!string.IsNullOrEmpty(provider.Key))
            {
                var value = provider.Value?.Length > 50 ? $"{provider.Value.Substring(0, 50)}..." : provider.Value;
                Console.WriteLine($"    🔑 {provider.Key}: {value ?? "[null]"}");
            }
        }
        
        // --- Connection String Handling ---
        Console.WriteLine("🔍 Locating database connection string...");

        // Robustly find the connection string from multiple sources
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ??
                               context.Configuration["ConnectionStrings:DefaultConnection"] ??
                               Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            // Log details if connection string is missing to help debugging
            Console.WriteLine($"ERROR: GetConnectionString('DefaultConnection'): {context.Configuration.GetConnectionString("DefaultConnection") ?? "[null]"}");
            Console.WriteLine($"ERROR: Configuration['ConnectionStrings:DefaultConnection']: {context.Configuration["ConnectionStrings:DefaultConnection"] ?? "[null]"}");
            Console.WriteLine($"ERROR: Environment.GetEnvironmentVariable('ConnectionStrings__DefaultConnection'): {Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? "[null]"}");
            throw new InvalidOperationException("🚫 Database connection string 'DefaultConnection' not found. Verify configuration sources (local.settings.json, environment variables).");
        }
        // Log only a part of the connection string for security
        Console.WriteLine($"✨ Connection string found, starting with: {connectionString.Substring(0, Math.Min(connectionString.Length, 20))}...");

        // --- Configure DbContext with Npgsql and EF Core Retry Strategy ---
        services.AddDbContext<ProjectContext>((serviceProvider, options) => {
            options.UseNpgsql(connectionString, npgsqlOptions => {                // Increase the command timeout to 90 seconds
                npgsqlOptions.CommandTimeout(90); // Changed from 60 to 90

                // Configure EF Core's built-in retry strategy provided by Npgsql.
                // This handles transient database errors like temporary network issues.
                // **IMPORTANT**: This configuration (maxRetryCount: 5) should prevent the "max retries (0)" error.
                // If you still get that error, ensure this exact code is deployed.
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5, // Number of retry attempts on transient failure
                    maxRetryDelay: TimeSpan.FromSeconds(30), // Fixed delay between retries
                    errorCodesToAdd: null); // Use default list of transient PostgreSQL error codes

                // ** IMPORTANT - Configure Pooling in Connection String **
                // Settings like Pooling, Min Pool Size, Max Pool Size, Connection Idle Lifetime, etc.,
                // should be set directly within your connection string itself, typically in
                // 'local.settings.json' for local dev or environment variables for Azure.
                // Example segment: "...;Pooling=True;Min Pool Size=5;Max Pool Size=20;"
                // The previous NpgsqlConnectionStringBuilder block here was removed as it was ineffective.
            });

            // --- EF Core Logging Configuration ---
            // Enable detailed errors. Useful during development.
            options.EnableDetailedErrors();

            // Enable sensitive data logging (like parameter values).
            // CAUTION: Avoid enabling this in production environments for security reasons.
            // Consider wrapping this with: if (context.HostingEnvironment.IsDevelopment()) { ... }
            options.EnableSensitiveDataLogging();            // Log EF Core activity to the console, focusing on command execution.
            // Consider using ILogger<ProjectContext> for more robust logging in production.
            options.LogTo(message => {
                // Filter for relevant command execution messages
                if (message.Contains("Executed DbCommand", StringComparison.OrdinalIgnoreCase) ||
                    message.Contains("Failed executing DbCommand", StringComparison.OrdinalIgnoreCase))
                {
                    // Simple console log with timestamp
                    Console.WriteLine($"[EFCore SQL] {DateTime.UtcNow:HH:mm:ss.fff} {message}");
                }            },
            LogLevel.Information); // Set the minimum log level for messages from EF Core
        });

        // Register the ClientReviewService
        services.AddScoped<API.Services.ClientReviewService>();
        
        Console.WriteLine("✅ [STARTUP] All services registered successfully");

        // Removed the separate Polly policy definition and registration.
        // EF Core's EnableRetryOnFailure is now handling database retries.
    })
    .Build();

// Launch the Azure Functions host application
Console.WriteLine("🚀 [STARTUP] Starting Azure Functions Host (Isolated Process Mode with Host-Level CORS from local.settings.json)");
host.Run();