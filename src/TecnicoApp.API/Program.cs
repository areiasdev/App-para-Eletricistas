using System.Text;
using System.Threading.RateLimiting;
using Ardalis.Result.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using TecnicoApp.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TecnicoApp.API.Middleware;
using TecnicoApp.Application;
using TecnicoApp.Infrastructure;

// Fix: Npgsql requires DateTimeKind.Utc — legacy mode accepts Unspecified from JSON binding
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Must exist before WebApplication.CreateBuilder resolves the environment: if wwwroot is
// missing at boot (e.g. a fresh clone — it's gitignored, only created on first logo upload),
// IWebHostEnvironment.WebRootFileProvider gets permanently pinned to a NullFileProvider for
// this process's lifetime, so UseStaticFiles() 404s forever even after the folder/file show
// up on disk later. Creating it upfront guarantees a real PhysicalFileProvider every time.
Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"));

var builder = WebApplication.CreateBuilder(args);

// ── Validate critical config at startup ──────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

if (jwtSecret.Length < 32 || jwtSecret.StartsWith("SET_VIA") || jwtSecret.StartsWith("CHANGE") || jwtSecret.StartsWith("REPLACE"))
    throw new InvalidOperationException(
        "Jwt:Secret must be a strong random string of at least 32 characters. " +
        "Generate one with: openssl rand -base64 48");

// ── Serilog ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration)
          .WriteTo.Console()
          .WriteTo.File("logs/tecnicoapp-.txt", rollingInterval: RollingInterval.Day));

// ── Application & Infrastructure ─────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
    options.AddDefaultResultConvention())
    .AddJsonOptions(options =>
        // Without this, enums (QuoteStatus, InterventionStatus, UserRole, ...) serialize
        // as raw integers — the frontend types and comparisons all assume string names.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

// ── JWT ───────────────────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30), // tight window, default is 5 min
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

// ── Rate limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    // Auth endpoints: 10 attempts per minute *per client IP* (brute-force protection).
    // A plain AddFixedWindowLimiter is one global bucket — ten failed logins from anywhere
    // would lock the whole company out for a minute — so partition by the caller's address
    // (the real one, after UseForwardedHeaders below has applied X-Forwarded-For).
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
        }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsync(
            """{"title":"Demasiadas tentativas. Aguarda um momento.","status":429}""",
            ct);
    };
});

// ── Reverse proxy ─────────────────────────────────────────────────────────────
// Production runs behind Caddy/Nginx/Traefik (see README), so the TCP peer is the proxy.
// Trust X-Forwarded-For/-Proto only from the proxy's network — by default loopback plus the
// private ranges Docker networks use; override with ReverseProxy:KnownNetworks (CIDR list).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    var networks = builder.Configuration.GetSection("ReverseProxy:KnownNetworks").Get<string[]>()
        ?? ["127.0.0.0/8", "::1/128", "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var cidr in networks)
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(cidr));
});

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TécnicoApp API",
        Version = "v1",
        Description = "API para gestão de orçamentos, clientes e manutenções"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Insere o JWT Bearer token"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("TecnicoAppCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .WithHeaders("Content-Type", "Authorization", "X-Requested-With", "X-Csrf-Token")
              .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
              // Lets the browser read the server-chosen file name of PDF/CSV downloads.
              .WithExposedHeaders("Content-Disposition")
              .SetPreflightMaxAge(TimeSpan.FromHours(2))
              .AllowCredentials()));

// ── Hangfire ──────────────────────────────────────────────────────────────────
var hangfireConnStr = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(hangfireConnStr)));
builder.Services.AddHangfireServer();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply pending EF Core migrations before accepting any requests. Required for the
// documented one-command deploy (docker compose up -d --build): a fresh install has no
// schema at all otherwise, and every request touching the database fails with
// "relation ... does not exist" (42P01) — including the very first registration.
using (var migrationScope = app.Services.CreateScope())
{
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<TecnicoApp.Infrastructure.Persistence.AppDbContext>();
    dbContext.Database.Migrate();
}

// Must run first so logging, rate limiting and HTTPS redirection all see the real client.
app.UseForwardedHeaders();

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers.Append("X-Content-Type-Options", "nosniff");
    headers.Append("X-Frame-Options", "DENY");
    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=(), payment=()");
    headers.Append("Content-Security-Policy",
        "default-src 'none'; frame-ancestors 'none'; form-action 'none'");
    // Remove server fingerprint headers
    headers.Remove("Server");
    headers.Remove("X-Powered-By");
    if (!app.Environment.IsDevelopment())
        headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    await next();
});

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TécnicoApp API v1"));
}

app.UseCors("TecnicoAppCors");

// Default request logging echoes the raw RequestPath, which would put the
// invoice public pay-link token (a bearer-equivalent credential valid for up to
// a year, e.g. /api/v1/invoices/public/{token}) into plaintext logs on every
// hit. Redact it via a custom template instead of the raw path.
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {SafeRequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("SafeRequestPath", RedactTokenSegments(httpContext.Request.Path));
    };
});

app.UseRateLimiter();

app.UseHttpsRedirection();
// Serves uploaded company logos (wwwroot/uploads/logos) — publicly readable by design,
// same as a downloaded quote PDF; nothing sensitive lives under wwwroot.
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();

// Hangfire dashboard (dev only — add auth in production)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
    });
}

// Use the DI-registered IRecurringJobManager rather than the static RecurringJob helper:
// AddHangfire() no longer sets the static JobStorage.Current global as a side effect, so
// RecurringJob.* throws "JobStorage instance has not been initialized" at startup — every
// deploy crash-loops. IRecurringJobManager reads the storage that was actually configured
// via builder.Services.AddHangfire(...) above.
var recurringJobManager = app.Services.GetRequiredService<IRecurringJobManager>();

// Stale trigger cleanup: TrialExpirationJob's class was deleted when SaaS-billing was removed,
// but Hangfire persists recurring job schedules in its own Postgres tables (not in code), so the
// old "trial-expiration" trigger kept firing daily, failing to resolve the type, and logging a
// JobLoadException warning every cycle. RemoveIfExists is idempotent — safe to call on every startup.
recurringJobManager.RemoveIfExists("trial-expiration");

// Recurring jobs run on the company's local clock (App:TimeZone, default Europe/Lisbon) —
// Hangfire's default is UTC, which shifts every "08:00" reminder by an hour in summer.
var jobTimeZone = ResolveTimeZone(builder.Configuration["App:TimeZone"], app.Logger);
var jobOptions = new RecurringJobOptions { TimeZone = jobTimeZone };

recurringJobManager.AddOrUpdate<MaintenanceAlertJob>(
    "maintenance-alerts",
    job => job.RunAsync(default),
    "0 8 * * *",
    jobOptions);

// Marks unpaid invoices past their due date as Overdue — runs before the reminders below.
recurringJobManager.AddOrUpdate<InvoiceOverdueJob>(
    "invoice-overdue",
    job => job.RunAsync(default),
    "5 0 * * *",
    jobOptions);

// Reminds clients their invoice is due in ~3 days — staggered a few minutes after the
// maintenance-alerts job so they don't all hit the DB at once.
recurringJobManager.AddOrUpdate<InvoiceDueReminderJob>(
    "invoice-due-reminders",
    job => job.RunAsync(default),
    "15 8 * * *",
    jobOptions);

// Nudges the owner about quotes sent a week ago that the client hasn't answered.
recurringJobManager.AddOrUpdate<QuoteFollowUpJob>(
    "quote-follow-ups",
    job => job.RunAsync(default),
    "45 8 * * 1-5",
    jobOptions);

// Reminds clients (not the technician) about tomorrow's scheduled intervention.
recurringJobManager.AddOrUpdate<AppointmentReminderJob>(
    "appointment-reminders",
    job => job.RunAsync(default),
    "30 8 * * *",
    jobOptions);

app.Run();

// Replaces the {token} segment of public token-based routes (invoice pay link
// /api/v1/invoices/public/{token}[/checkout] and quote approval /api/v1/quotes/public/{token}[/…]) with a fixed
// placeholder before it's ever handed to the logger.
static string RedactTokenSegments(string path)
{
    return System.Text.RegularExpressions.Regex.Replace(
        path,
        "(?<=/(invoices|quotes)/public/)[^/]+",
        "[REDACTED]");
}

static TimeZoneInfo ResolveTimeZone(string? id, Microsoft.Extensions.Logging.ILogger logger)
{
    const string defaultTimeZone = "Europe/Lisbon";
    try
    {
        return TimeZoneInfo.FindSystemTimeZoneById(string.IsNullOrWhiteSpace(id) ? defaultTimeZone : id);
    }
    catch (TimeZoneNotFoundException)
    {
        logger.LogWarning("Time zone {TimeZone} not found — scheduling recurring jobs in UTC.", id ?? defaultTimeZone);
        return TimeZoneInfo.Utc;
    }
}

public partial class Program { }
