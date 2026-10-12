using DataAccess.Inventory.ProductStock;
using DataAccess.Models;
using DataAccess.Repository;
using DataAccess.Services;
using DataAccess.Services.StockMovement;
using DataAccess.Workflows;
using InvEntry.Gst.Core.Classification;
using InvEntry.Gst.Core.Rules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using DataAccess.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine(
                $"JWT AUTH FAILED: {context.Exception.Message}");

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            Console.WriteLine("JWT VALIDATED SUCCESSFULLY");
            return Task.CompletedTask;
        }
    };
})
.AddScheme<AuthenticationSchemeOptions, ManagementServiceAuthenticationHandler>(
    ManagementServiceAuthenticationDefaults.Scheme, _ => { });

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
                     "http://127.0.0.1:5173",
                    "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<MijmsContext>(
        ctx => ctx.UseSqlServer(connectionString));
builder.Services.AddSwaggerGen(options =>
{
    const string bearerScheme = "Bearer";
    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT issued by /api/auth/login."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = bearerScheme
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddScoped(
        typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

var financeTrackerEnabled =
    builder.Configuration.GetValue<bool>("FinanceTracker:Enabled");
builder.Services.Configure<FinanceSyncOptions>(
    builder.Configuration.GetSection(FinanceSyncOptions.SectionName));
if (financeTrackerEnabled && string.IsNullOrWhiteSpace(
        builder.Configuration["FinanceTracker:ApiKey"]))
    throw new InvalidOperationException(
        "FinanceTracker service authentication is required when synchronization is enabled.");
if (financeTrackerEnabled && string.IsNullOrWhiteSpace(builder.Configuration[
        ManagementServiceAuthenticationDefaults.ConfigurationKey]))
    throw new InvalidOperationException(
        "InvEntry management service authentication is required when synchronization is enabled.");

builder.Services.AddHttpClient(
    "FinanceTracker",
    client =>
    {
        var baseAddress = new Uri(
            builder.Configuration["FinanceTracker:BaseUrl"]
            ?? throw new InvalidOperationException(
                "FinanceTracker BaseUrl is not configured."));
        var apiKey = builder.Configuration["FinanceTracker:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException(
                "FinanceTracker service authentication is not configured.");
        if (!builder.Environment.IsDevelopment() &&
            baseAddress.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException(
                "FinanceTracker service communication must use HTTPS outside development.");

        client.BaseAddress = baseAddress;
        client.DefaultRequestHeaders.Add(
            "X-FinanceTracker-Integration-Key", apiKey);

        client.Timeout = TimeSpan.FromSeconds(10);
    });

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IStockMovementService, StockMovementService>();
builder.Services.AddScoped<IVoucherNumberService, VoucherNumberService>();
builder.Services.AddScoped<IAuditIdentityProvider, AuditIdentityProvider>();
builder.Services.AddScoped<ICustomerOrderWorkflow, CustomerOrderWorkflow>();
builder.Services.AddScoped<IInvoiceWorkflow, InvoiceWorkflow>();
builder.Services.AddScoped<IEstimateWorkflow, EstimateWorkflow>();
builder.Services.AddScoped<IGstRuleProvider, GstRuleProvider>();
builder.Services.AddScoped<IGstClassificationService, GstClassificationService>();
builder.Services.AddScoped<IGstr1StagingEnrichmentService, Gstr1StagingEnrichmentService>();
builder.Services.AddScoped<IGstr1ReportQueryService, Gstr1ReportQueryService>();
builder.Services.AddScoped<IGstr1ValidationService, Gstr1ValidationService>();
builder.Services.AddScoped<IGstr1StagingService, Gstr1StagingService>();
builder.Services.AddSingleton<Gstr1Table12Policy>();
builder.Services.AddScoped<IGstr1HsnSummaryService, Gstr1HsnSummaryService>();
builder.Services.AddScoped<IGstr1DocumentsIssuedService, Gstr1DocumentsIssuedService>();
builder.Services.AddScoped<IGstr1B2csSummaryService, Gstr1B2csSummaryService>();
builder.Services.AddScoped<IGstr1B2bSummaryService, Gstr1B2bSummaryService>();
builder.Services.AddScoped<IGstr1ExportPreparationService, Gstr1ExportPreparationService>();
builder.Services.AddScoped<IGstr1JsonExportService, Gstr1JsonExportService>();
builder.Services.AddScoped<IGstr1BackfillService, Gstr1BackfillService>(); 
builder.Services.AddScoped<IHistoricalInvoiceAuditService, HistoricalInvoiceAuditService>();
builder.Services.AddScoped<IHistoricalMigrationPreviewService, HistoricalMigrationPreviewService>();
builder.Services.AddSingleton<IHistoricalMigrationPreviewTokenStore, HistoricalMigrationPreviewTokenStore>();
builder.Services.AddScoped<IHistoricalMigrationStageService, HistoricalMigrationStageService>();
builder.Services.AddScoped<ICurrentCompanyGstinProvider, CurrentCompanyGstinProvider>();
builder.Services.AddScoped<IStockAdjustmentWorkflow, StockAdjustmentWorkflow>();
builder.Services.AddScoped<IStockTransferWorkflow, StockTransferWorkflow>();
builder.Services.AddScoped<IOldMetalTransferPostingService, OldMetalTransferPostingService>();
builder.Services.AddScoped<IProductStockMovementService,ProductStockMovementService>();

builder.Services.AddScoped<FinanceInvoicePayloadBuilder>();
builder.Services.AddScoped<FinanceSyncService>();
builder.Services.AddScoped<FinanceSyncReconciliationService>();
builder.Services.AddScoped<FinanceSyncManagementService>();
builder.Services.AddScoped<FinanceClosingEligibilityService>();
builder.Services.AddScoped<FinanceClosingFenceService>();
builder.Services.AddSingleton<FinanceSyncDispatchJobQueue>();
builder.Services.AddScoped<IFinanceSyncSchemaReadinessChecker,
    FinanceSyncSchemaReadinessChecker>();

if (financeTrackerEnabled)
{
    // Registered first so schema compatibility is verified before the
    // dispatcher starts and before Kestrel accepts invoice finalisations.
    builder.Services.AddHostedService<FinanceSyncSchemaReadinessService>();
    builder.Services.AddHostedService<FinanceSyncBackgroundService>();
    builder.Services.AddHostedService<FinanceSyncManualDispatchWorker>();
}

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
