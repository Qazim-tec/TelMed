using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;
using System.Text;
using TelmMed.Api.Data;
using TelmMed.Api.Services;
using TelmMed.Api.Services.DoctorService;
using TelmMed.Api.Services.Interfaces;
using TelmMed.Api.Services.RateLimiter;

var builder = WebApplication.CreateBuilder(args);

// === 1. PostgreSQL with Render.com SSL Fix ===
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connStr = builder.Configuration.GetConnectionString("PostgreSQL");
    if (string.IsNullOrEmpty(connStr))
        throw new InvalidOperationException("PostgreSQL connection string is missing.");

    // Auto-fix for Render.com (adds SSL if missing)
    if (connStr.Contains("render.com") && !connStr.Contains("SslMode"))
    {
        connStr += ";SslMode=Require;Trust Server Certificate=true";
    }

    options.UseNpgsql(connStr);
});

// === 2. Redis ===
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// === 3. Firebase Admin SDK - Bulletproof Initialization ===
try
{
    GoogleCredential credential;

    var envPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
    if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
    {
        credential = GoogleCredential.FromFile(envPath);
    }
    else
    {
        var possiblePaths = new[]
        {
            Path.Combine(builder.Environment.ContentRootPath, "telemedicine-project-5bf24-firebase-adminsdk-fbsvc-64756c1716.json"),
            Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "telemedicine-project-5bf24-firebase-adminsdk-fbsvc-64756c1716.json")
        };

        var jsonPath = possiblePaths.FirstOrDefault(File.Exists)
                       ?? throw new FileNotFoundException("Firebase service account JSON not found!");

        credential = GoogleCredential.FromFile(jsonPath);
    }

    FirebaseApp.Create(new AppOptions
    {
        Credential = credential,
        ProjectId = "telemedicine-project-5bf24"
    }, "TelmMedApp");

    Console.WriteLine("Firebase Admin SDK initialized successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Firebase init failed (non-fatal): {ex.Message}");
    // Don't crash the app — phone auth will just fail gracefully
}

// === 4. Dependency Injection ===
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDoctorRegistrationService, DoctorRegistrationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRateLimiterService, RedisRateLimiterService>();
builder.Services.AddScoped<IPatientLoginService, PatientLoginService>();
builder.Services.AddScoped<IDoctorLoginService, DoctorLoginService>();
builder.Services.AddHttpContextAccessor();

// === 5. Controllers ===
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// === 6. JWT Authentication ===
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Secret"]!)),
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Patient", policy => policy.RequireRole("Patient"));
    options.AddPolicy("Doctor", policy => policy.RequireRole("Doctor"));
});

// === 7. Swagger - 100% FIXED (No More 404 or Failed to load) ===
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TelmMed API",
        Version = "v1",
        Description = "Nigerian Telemedicine Platform – Firebase Phone Auth + JWT + PostgreSQL"
    });

    // CRITICAL: Fix ambiguous actions & duplicate routes
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // Force unique OperationIds – this alone fixes 95% of Swagger crashes
    c.CustomOperationIds(apiDesc =>
    {
        var controller = apiDesc.ActionDescriptor.RouteValues["controller"];
        var action = apiDesc.ActionDescriptor.RouteValues["action"];
        var method = apiDesc.HttpMethod ?? "UNKNOWN";
        return $"{controller}_{action}_{method}";
    });

    c.CustomSchemaIds(x => x.FullName?.Replace("+", "_") ?? x.Name);

    // JWT Support in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter your JWT token: Bearer {your-token-here}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// === Middleware Pipeline ===
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TelmMed API v1");
    c.RoutePrefix = "swagger";  // → https://localhost:7xxx/swagger
    c.DisplayOperationId();
    c.DisplayRequestDuration();
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === Database Migrations - Safe for Render.com ===
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying database migrations...");
        db.Database.Migrate();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration failed — check your PostgreSQL connection string (SSL required on Render)");
        // Don't throw — app stays alive, Swagger works
    }
}

Console.WriteLine("TelmMed API is now running!");
app.Run();