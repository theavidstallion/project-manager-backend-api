using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectManager.Data;
using ProjectManager.Filters;
using ProjectManager.Middleware;
using ProjectManager.Models;
using ProjectManager.Services; 
using Serilog;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// --- 1. Service Registration ---

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext() // Important for Correlation ID!
    .CreateLogger();

builder.Host.UseSerilog();


// 1a. Core Services
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AuditLogActionFilter>(); // Add global filter for Audit Logging
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();

// 1b. Database & Identity
// Pre 500.30 error
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)
    ));

// IMPORTANT: Configure Identity to NOT add default authentication scheme
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Disable automatic cookie-based authentication redirect
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Identity to not use cookies for API
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = 403;
        return Task.CompletedTask;
    };
});

// 1c. Custom Application Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();

// 1d. JWT Authentication Configuration
var key = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured.");

// Set JWT Bearer as the DEFAULT authentication scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

        // Set to false for development simplicity, but necessary to set true and configure in production
        ValidateIssuer = false,
        ValidateAudience = false,

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 1e. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanCloseTask", policy =>
    policy.RequireClaim(DataSeeder.claimType, DataSeeder.claimValue));
});


var app = builder.Build();

// --- 2. Middleware Configuration ---

// Execute Data Seeder (Role/Claim/Admin User Creation)
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await DataSeeder.InitializeAsync(userManager, roleManager);
}

// 2a. Development Middleware
if (app.Environment.IsDevelopment())
{
    // Swagger UI: Necessary for API testing via browser/Postman
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2b. Custom Middleware (Uncomment and configure when ready)
// app.UseMiddleware<ErrorHandlingMiddleware>();
// app.UseMiddleware<CorrelationIdMiddleware>();

// 2c. Standard Middleware
app.UseHttpsRedirection();

// CORS policy, allow for frontend client.
app.UseCors(policy =>
    policy.WithOrigins("https://humble-project-manager.netlify.app", "http://localhost:4200") 
          .AllowAnyHeader()
          .AllowAnyMethod());

// 1. Error Handling (Must be high up to catch errors from below)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 2. Correlation ID (Must be before Auth/Controllers so logs have IDs)
app.UseMiddleware<CorrelationIdMiddleware>();

// 2d. Security Middleware (Order is CRITICAL: Authentication BEFORE Authorization)
app.UseAuthentication();
app.UseAuthorization();

// 2e. Routing
app.MapControllers();

app.Run();