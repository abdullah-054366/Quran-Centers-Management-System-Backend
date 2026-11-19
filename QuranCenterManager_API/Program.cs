using Business_Layer;
using Data_Access_Layer;
using Data_Access_Layer.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
// ✅ NEW (Rate limiting)
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuranCenterManager_API.Configuration;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;


// خلف reverse proxy (Caddy): لقراءة IP العميل الحقيقي من X-Forwarded-For
using Microsoft.AspNetCore.HttpOverrides;

// Create the application builder.
var builder = WebApplication.CreateBuilder(args);

// أسرار التشغيل المحلي (سلسلة الاتصال و Jwt:Secret) في ملف لا يُرفع إلى Git.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);



builder.Services.AddDbContext<HalaqaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<UserData>();
builder.Services.AddScoped<UserBusinessLayer>();

builder.Services.AddScoped<TeacherData>();
builder.Services.AddScoped<TeacherBusinessLayer>();

builder.Services.AddScoped<StudentData>();
builder.Services.AddScoped<StudentBusinessLayer>();

builder.Services.AddScoped<CenterData>();
builder.Services.AddScoped<CenterBusinessLayer>();

builder.Services.AddScoped<AttendanceData>();
builder.Services.AddScoped<AttendanceBusinessLayer>();

builder.Services.AddScoped<MemorizationData>();
builder.Services.AddScoped<MemorizationBusinessLayer>();

builder.Services.AddScoped<MemorizationPlanData>();
builder.Services.AddScoped<MemorizationPlanBusinessLayer>();

builder.Services.AddScoped<SettingsData>();
builder.Services.AddScoped<SettingsBusinessLayer>();

builder.Services.AddScoped<AuditData>();
builder.Services.AddScoped<AuditBusinessLayer>();

builder.Services.AddScoped<HalaqaData>();
builder.Services.AddScoped<HalaqaBusinessLayer>();





var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .GetChildren()
    .Select(origin => origin.Value)
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!)
    .ToArray();

if (allowedOrigins.Length == 0)
     allowedOrigins = new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteClient", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// ===============================
// 1) Authentication (JWT Bearer)
// ===============================
var jwtSecret = JwtConfiguration.GetSecret(builder.Configuration, builder.Environment);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = "StudentApi",
            ValidAudience = "StudentApiUsers",

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),

            ClockSkew = TimeSpan.Zero
        };
    });

// ===============================
// 2) Authorization
// ===============================
builder.Services.AddAuthorization(options =>
{
    // أي endpoint بلا [Authorize] أو [AllowAnonymous] يتطلب تسجيل الدخول افتراضياً.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ✅ NEW
// ===============================
// 2.5) Rate Limiting (Login + Refresh)
// ===============================

builder.Services.AddRateLimiter(options =>
{
    // Always return 429 when blocked
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy: max 5 requests per 1 minute per IP
    options.AddPolicy("AuthLimiter", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ip,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Register controllers
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// ===============================
// 3) Swagger / OpenAPI
// ===============================
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});

var app = builder.Build();

// ===============================
// 4) Middleware Pipeline
// ===============================
// خلف Caddy فقط (ForwardedHeaders__Enabled=true في docker-compose.prod.yml).
// يجب أن يسبق Rate Limiting حتى يُحسب الحد لكل عميل وليس لعنوان Caddy.
// آمن لأن حاوية الـ API لا تنشر أي منفذ للخارج؛ الوصول إليها عبر Caddy فقط.
if (app.Configuration.GetValue("ForwardedHeaders:Enabled", false))
{
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedOptions.KnownNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
}

// Swagger: مفعّل دائماً في التطوير، وفي الإنتاج فقط إذا كان Swagger__Enabled=true
// (في docker-compose.prod.yml). محمي في الإنتاج بكلمة مرور عبر Caddy.
var swaggerEnabled = app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Configuration.GetValue("HttpsRedirection:Enabled", app.Environment.IsDevelopment()))
    app.UseHttpsRedirection();

app.UseCors("ViteClient");

// ✅ NEW: Rate limiting should run early (before controllers)
app.UseRateLimiter();

//Return a safe 429 message (without revealing limits)
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
    {
        await context.Response.WriteAsync("Too many attempts. Please try again later.");
    }
});



app.UseAuthentication();
app.UseAuthorization();

// ✅  Global 403 logging middleware 
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.ToString();

        // ✅ Centralized security log for authorization abuse
        app.Logger.LogWarning(
            "Forbidden access. UserId={UserId}, Path={Path}, IP={IP}",
            userId,
            path,
            ip
        );
    }
});



app.MapControllers();

app.Run();


