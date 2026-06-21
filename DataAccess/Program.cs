using DataAccess.Models;
using DataAccess.Repository;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
