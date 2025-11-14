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

// === 1. PostgreSQL ===
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

// === 2. Redis ===
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));

// === 3. Firebase Admin SDK (optional) ===
var credEnv = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
var credPath = !string.IsNullOrEmpty(credEnv)
    ? credEnv
    : Path.Combine(builder.Environment.ContentRootPath, "firebase-service-account.json");

if (File.Exists(credPath))
{
    var firebaseCred = GoogleCredential.FromFile(credPath);
    FirebaseApp.Create(new AppOptions { Credential = firebaseCred });
}
else
{
    Console.WriteLine($"[Warning] Firebase credentials not found at '{credPath}'. Skipping Firebase initialization.");
}

// === 4. DI Services ===
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IDoctorRegistrationService, DoctorRegistrationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRateLimiterService, RedisRateLimiterService>();
builder.Services.AddScoped<IPatientLoginService, PatientLoginService>();
builder.Services.AddScoped<IDoctorLoginService, DoctorLoginService>();
builder.Services.AddHttpContextAccessor();

// === 5. Controllers & API Explorer ===
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

// === 7. Authorization Policies ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// === 8. Swagger / OpenAPI ===
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TelmMed API",
        Version = "v1",
        Description = "Secure onboarding with Firebase + JWT + PostgreSQL"
    });

    // Prevent duplicate schemaId conflicts
    c.CustomSchemaIds(type => type.FullName?.Replace("+", ".") ?? type.Name);

    // JWT Bearer
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: `Bearer {token}`",
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

    // Resolve ambiguous actions
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

var app = builder.Build();

// === Middleware Pipeline ===

// 1. Developer Exception Page (Dev only)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 2. Swagger JSON endpoint (always)
app.UseSwagger();

// 3. Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TelmMed API v1");
        c.RoutePrefix = "swagger";
        c.DisplayOperationId();
        c.DisplayRequestDuration();
    });
}
else
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TelmMed API v1");
        c.RoutePrefix = "swagger"; // Set to "" to hide in prod
    });
}

// 4. HTTPS & Routing
app.UseHttpsRedirection();
app.UseRouting();

// 5. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 6. Map Controllers
app.MapControllers();

// === 7. Apply DB Migrations (Development AND Production) ===
var isDev = app.Environment.IsDevelopment();
var isProd = app.Environment.IsProduction();

if (isDev || isProd)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        db.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully ({Environment}).",
            isDev ? "Development" : "Production");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed ({Environment})!",
            isDev ? "Development" : "Production");

        // Fail fast in Production — Render will restart
        if (isProd)
        {
            throw;
        }
    }
}

app.Run();