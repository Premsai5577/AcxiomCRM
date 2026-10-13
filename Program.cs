using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

var builder = WebApplication.CreateBuilder(args);

// Cloud / Container dynamic PORT support (Render, Railway, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://+:{port}");
}

// Support reverse proxies (Render, Cloudflare, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=AcxiomCRM.db";

var match = System.Text.RegularExpressions.Regex.Match(connectionString, @"(?:Data Source|DataSource)\s*=\s*([^;]+)");
if (match.Success)
{
    var dbPath = match.Groups[1].Value.Trim();
    var dir = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ASP.NET Core Identity with Password Policy & Account Lockout
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password Policy (PDF Page 8: Min 8 chars, character classes)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Account Lockout (PDF Page 8: Lockout after 5 failed attempts for 15 mins)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User options
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Application Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Register services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUserScopeService, UserScopeService>();

builder.Services.AddControllersWithViews();

// OpenAPI / Swagger Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed Database (Roles, Users, Initial CRM records)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// HTTP request pipeline
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
    app.UseHttpsRedirection();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

// Swagger UI accessible at /swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AcxiomCRM API v1");
    c.RoutePrefix = "swagger";
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
