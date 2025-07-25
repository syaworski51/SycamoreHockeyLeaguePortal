using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SHLAPI.Data;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var secrets = builder.Configuration.GetSection("Secrets");
var mvcDomain = secrets.GetValue<string>("MVC:LocalURL")!;

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", 
        policy => policy.WithOrigins(mvcDomain)
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var connectionString = builder.Configuration.GetConnectionString("SHL_DB") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<SHLPortalDbContext>(options =>
    options.UseSqlServer(connectionString));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
