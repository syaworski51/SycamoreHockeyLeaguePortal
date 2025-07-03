using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SycamoreHockeyLeaguePortal.Data;
using SycamoreHockeyLeaguePortal.Models;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using SycamoreHockeyLeaguePortal.Services;

System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);
bool isDevelopmentEnvironment = builder.Environment.IsDevelopment();

//var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri")!);
//builder.Configuration.AddAzureKeyVault(keyVaultEndpoint, new DefaultAzureCredential());

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options => 
{
    string connectionString = isDevelopmentEnvironment ?
        builder.Configuration.GetConnectionString("Local") ??
            throw new InvalidOperationException("Connection string 'Local' not found.") :
        builder.Configuration.GetConnectionString("Live") ??
            throw new InvalidOperationException("Connection string 'Live' not found.");

    options.UseSqlServer(connectionString, options =>
    {
        options.EnableRetryOnFailure(maxRetryCount: 5);
    });
});

builder.Services.AddDbContext<LiveDbContext>(options =>
{
    string connectionString = builder.Configuration.GetConnectionString("LiveSQLAuth") ??
        throw new InvalidOperationException("Connection string 'LiveSQLAuth' not found.");

    options.UseSqlServer(connectionString, options =>
    {
        options.EnableRetryOnFailure(maxRetryCount: 5);
    });
});


//builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
