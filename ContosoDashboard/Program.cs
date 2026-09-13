using System.Data;
using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
var sqliteDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "ContosoDashboard.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={sqliteDbPath}"));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        var requiredTables = new[] { "Users", "Projects", "Tasks", "Documents" };

        using var context = services.GetRequiredService<ApplicationDbContext>();
        var dbExists = File.Exists(sqliteDbPath);

        if (!dbExists)
        {
            logger.LogInformation("Creating fresh SQLite database for the dashboard.");
            context.Database.EnsureCreated();
        }
        else if (!requiredTables.All(tableName => DatabaseHasTable(context, tableName, sqliteDbPath)))
        {
            logger.LogWarning("SQLite schema is missing required tables. Resetting the local development database.");
            ResetSqliteDatabase(sqliteDbPath, context);

            var rebuiltOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={sqliteDbPath}")
                .Options;

            using var rebuiltContext = new ApplicationDbContext(rebuiltOptions);
            rebuiltContext.Database.EnsureCreated();
        }
        else
        {
            context.Database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

static bool DatabaseHasTable(ApplicationDbContext context, string tableName, string databasePath)
{
    try
    {
        if (!File.Exists(databasePath))
        {
            return false;
        }

        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @tableName LIMIT 1;";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        using var reader = command.ExecuteReader();
        return reader.Read();
    }
    catch
    {
        return false;
    }
}

static void ResetSqliteDatabase(string databasePath, ApplicationDbContext context)
{
    try
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State == ConnectionState.Open)
        {
            connection.Close();
        }
    }
    catch
    {
        // Ignore connection-state issues during reset.
    }

    var databaseFiles = new[]
    {
        databasePath,
        databasePath + "-wal",
        databasePath + "-shm",
        databasePath + "-journal"
    };

    foreach (var filePath in databaseFiles)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}

app.Run();
