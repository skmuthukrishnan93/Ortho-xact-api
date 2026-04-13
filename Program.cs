using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.IdentityModel.Tokens;
using Ortho_xact_api.Models;
using Ortho_xact_api.SysModels;
using System.Text;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;


var builder = WebApplication.CreateBuilder(args);

// Run as a Windows Service
//builder.Host.UseWindowsService();

//// Setup logging to file (optional but useful for services)
//var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
//Directory.CreateDirectory(logPath);
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
//builder.Logging.AddFile($"{logPath}/log.txt"); // You can add Serilog or similar if needed

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost", "http://localhost:3000","http://41.87.206.94", "http://192.168.23.157", "http://192.168.23.155", "http://192.168.16.70") // 👈 or "http://localhost:3000" if you're using that port
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddDbContext<OrthoxactContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<SysproContext>(options =>
   options.UseSqlServer(builder.Configuration.GetConnectionString("SysproConnection")));

var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
//builder.WebHost.UseUrls("http://192.168.23.157:5000");
//builder.WebHost.UseUrls("http://192.168.16.70:5000");
//builder.WebHost.UseUrls("http://41.87.206.94:5000");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
