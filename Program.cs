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

var builder = WebApplication.CreateBuilder(args);

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
            frontendUrl,
            "https://lively-coast-028e05600.6.azurestaticapps.net",
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
    app.UseStatusCodePages(); // for 404 etc.
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

// Set URLs in dev only
if (app.Environment.IsDevelopment())
{
    app.Urls.Clear();
    app.Urls.Add($"http://localhost:7197");
}

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DataSeeder.SeedData(services);
        app.Logger.LogInformation("Database seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();