using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSerilog(logger);

// CORS FIX
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {

         policy.WithOrigins(
                    "http://localhost:5173",
                    "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<MijmsContext>(ctx => ctx.UseSqlServer(connectionString));
builder.Services.AddSwaggerGen();
builder.Services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// ENABLE CORS BEFORE authorization
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
