using Backendapi.Data;
using Backendapi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// Add startup diagnostics
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics();

// Log startup configuration
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();
logger.LogInformation("Starting application...");
logger.LogInformation($"Environment: {builder.Environment.EnvironmentName}");
logger.LogInformation($"Content Root: {builder.Environment.ContentRootPath}");
logger.LogInformation($"Web Root: {builder.Environment.WebRootPath}");

try
{
    // Log configuration values (excluding sensitive data)
    logger.LogInformation("Configuration loaded:");
    logger.LogInformation($"ASPNETCORE_ENVIRONMENT: {builder.Environment.EnvironmentName}");
    logger.LogInformation($"Database Provider: {builder.Configuration.GetConnectionString("DefaultConnection")?.Split(';')[0]}");
    logger.LogInformation($"JWT Issuer configured: {!string.IsNullOrEmpty(builder.Configuration["Jwt:Issuer"])}");
    logger.LogInformation($"JWT Audience configured: {!string.IsNullOrEmpty(builder.Configuration["Jwt:Audience"])}");
    logger.LogInformation($"Frontend URL: {builder.Configuration["ApiSettings:FrontendUrl"]}");
}
catch (Exception ex)
{
    logger.LogError(ex, "Error during startup configuration logging");
}

// Enable Application Insights
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
{
    module.EnableSqlCommandTextInstrumentation = true;
});

// Register telemetry service
builder.Services.AddScoped<ITelemetryService, ApplicationInsightsTelemetryService>();

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// Read app settings
var apiPort = builder.Configuration["ApiSettings:Port"] ?? "7197";
var frontendUrl = builder.Configuration["ApiSettings:FrontendUrl"] ?? "http://localhost:3000";

// Add services
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    options.JsonSerializerOptions.MaxDepth = 32;
});

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EduSync API",
        Version = "v1",
        Description = "API for EduSync Course Management System",
        Contact = new OpenApiContact
        {
            Name = "EduSync Support",
            Email = "support@edusync.com"
        }
    });


c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "https://proud-plant-0cd1a3400.6.azurestaticapps.net",
            "http://localhost:3000"  // Keep local development URL
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// EF Core + Logging
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging();
});

builder.Logging.AddApplicationInsights(
configureTelemetryConfiguration: (config) =>
config.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"],
configureApplicationInsightsLoggerOptions: (_) => { }
);

var app = builder.Build();

// Enable Swagger and exception handling in dev
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseStatusCodePages();
    app.UseHsts(); // Add HSTS in production
}

// Always redirect to HTTPS in production
app.UseHttpsRedirection();

// Enable CORS early
app.UseCors("AllowFrontend");

// Serve static files
app.UseStaticFiles();

// Auth
app.UseAuthentication();
app.UseAuthorization();

// Logging middleware
app.Use(async (context, next) =>
{
    app.Logger.LogInformation($"Incoming request: {context.Request.Method} {context.Request.Path}");
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error processing request: {ex.Message}");
        app.Logger.LogError($"Stack trace: {ex.StackTrace}");
        throw;
    }
});

// API routes
app.MapControllers();

// Health and warmup routes
app.MapGet("/", () => Results.Ok("EduSync API is running."));
app.MapGet("/health", () => Results.Ok("Healthy"));

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        logger.LogInformation("Starting database initialization...");
        var dbContext = services.GetRequiredService<AppDbContext>();
        
        // Log database connection attempt
        logger.LogInformation("Testing database connection...");
        var canConnect = await dbContext.Database.CanConnectAsync();
        logger.LogInformation($"Database connection test result: {canConnect}");
        
        if (!canConnect)
        {
            logger.LogError("Cannot connect to database. Please check connection string and network access.");
            throw new Exception("Database connection failed");
        }

        // Ensure database exists
        logger.LogInformation("Ensuring database exists...");
        await dbContext.Database.EnsureCreatedAsync();
        
        // Seed data
        logger.LogInformation("Starting data seeding...");
        await DataSeeder.SeedData(services);
        logger.LogInformation("Database initialization completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database initialization");
        throw; // Rethrow to ensure the app doesn't start with a broken database
    }
}

app.Run();