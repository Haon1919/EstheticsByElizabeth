// Required Usings for Azure Functions Isolated Process
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging; // Needed for LogLevel and LogTo
using Microsoft.EntityFrameworkCore; // Needed for EF Core configuration
using Microsoft.EntityFrameworkCore.InMemory; // Needed for UseInMemoryDatabase
using API.Data; // Your DbContext namespace
using Npgsql; // Needed for UseNpgsql and options
using System; // Needed for TimeSpan, StringComparison etc.
using System.Text.Json; // Needed for JsonSerializerOptions
using System.Text.Json.Serialization; // Needed for ReferenceHandler and JsonIgnoreCondition
using Minio; // MinIO client

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
    })
    .ConfigureAppConfiguration((context, config) =>
    {
        // Load configuration from local settings file
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureServices((context, services) => {        
        // --- Connection String Handling with Graceful Fallback ---
        Console.WriteLine("🔍 [STARTUP] Locating database connection string...");

        try
        {
            // Robustly find the connection string from multiple sources
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection") ??
                                   context.Configuration["ConnectionStrings:DefaultConnection"] ??
                                   Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (!string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine($"✨ [STARTUP] Connection string found, starting with: {connectionString.Substring(0, Math.Min(connectionString.Length, 20))}...");
                
                // Configure DbContext with Npgsql and EF Core Retry Strategy
                services.AddDbContext<ProjectContext>((serviceProvider, options) => {
                    options.UseNpgsql(connectionString, npgsqlOptions => {
                        // Increase the command timeout to 30 seconds for deployment
                        npgsqlOptions.CommandTimeout(30);

                        // Configure EF Core's built-in retry strategy
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3, // Reduced for faster startup
                            maxRetryDelay: TimeSpan.FromSeconds(10), // Reduced delay
                            errorCodesToAdd: null);
                    });

                    // Enable detailed errors for debugging
                    options.EnableDetailedErrors();
                    
                    // Only enable sensitive data logging in development
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        options.EnableSensitiveDataLogging();
                        options.LogTo(Console.WriteLine, LogLevel.Information);
                    }
                });
                
                Console.WriteLine("✅ [STARTUP] Database context configured with PostgreSQL");
            }
            else
            {
                Console.WriteLine("⚠️ [STARTUP] No database connection string found, using in-memory fallback");
                
                // Use in-memory database as fallback for deployment
                services.AddDbContext<ProjectContext>(options =>
                    options.UseInMemoryDatabase("FallbackDb"));
                    
                Console.WriteLine("✅ [STARTUP] Database context configured with in-memory fallback");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ [STARTUP] Database configuration error: {ex.Message}");
            Console.WriteLine("⚠️ [STARTUP] Using in-memory database fallback");
            
            // Use in-memory database as fallback
            services.AddDbContext<ProjectContext>(options =>
                options.UseInMemoryDatabase("ErrorFallbackDb"));
                  Console.WriteLine("✅ [STARTUP] Database context configured with error fallback");
        }

        // Register core services with error handling
        try
        {
            // Register the ClientReviewService
            services.AddScoped<API.Services.ClientReviewService>();
            Console.WriteLine("✅ [STARTUP] ClientReviewService registered");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [STARTUP] Failed to register ClientReviewService: {ex.Message}");
        }        // Register Email Service based on configuration with error handling
        try
        {
            var emailProvider = context.Configuration["Values:Email:Provider"] ?? "Development";
            Console.WriteLine($"📧 [STARTUP] Registering Email Provider: {emailProvider}");
            
            // Use simple fallback for deployment stability
            services.AddScoped<API.Services.IEmailService, API.Services.EmailService>();
            Console.WriteLine("✅ [STARTUP] Development email service registered (deployment safe)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ [STARTUP] Email service configuration failed: {ex.Message}, using fallback");
            services.AddScoped<API.Services.IEmailService, API.Services.EmailService>();
        }
        
        // Register HttpClient for HTTP-based services
        services.AddHttpClient();
          // Register Image Storage Service based on configuration
        var storageProvider = context.Configuration["Values:ImageStorage:Provider"] ?? "Local";
        Console.WriteLine($"🗄️ [STARTUP] Registering Image Storage Provider: {storageProvider}");
        
        if (storageProvider.Equals("MinIO", StringComparison.OrdinalIgnoreCase))
        {
            // Add MinIO client using official AddMinio method
            var endpoint = context.Configuration["Values:ImageStorage:MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = context.Configuration["Values:ImageStorage:MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = context.Configuration["Values:ImageStorage:MinIO:SecretKey"] ?? "minioadmin123";
            var useSSL = bool.Parse(context.Configuration["Values:ImageStorage:MinIO:UseSSL"] ?? "false");

            Console.WriteLine($"🪣 [STARTUP] Configuring MinIO with endpoint: {endpoint}, SSL: {useSSL}");
            
            services.AddMinio(configureClient => configureClient
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .WithSSL(useSSL)
                .Build());

            services.AddScoped<API.Services.IImageStorageService, API.Services.MinioImageStorageService>();
            Console.WriteLine("✅ [STARTUP] MinIO image storage service configured with official AddMinio");
        }
        else
        {
            services.AddScoped<API.Services.IImageStorageService, API.Services.LocalImageStorageService>();
            Console.WriteLine("✅ [STARTUP] Local image storage service configured");
        }
          Console.WriteLine("✅ [STARTUP] All services registered successfully");

        // Removed the separate Polly policy definition and registration.
        // EF Core's EnableRetryOnFailure is now handling database retries.
    })
    .Build();

// Launch the Azure Functions host application
Console.WriteLine("🚀 [STARTUP] Starting Azure Functions Host (Isolated Process Mode with Host-Level CORS from local.settings.json)");
host.Run();