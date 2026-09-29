using CommunityToolkit.Mvvm.ComponentModel;
using InvEntry.Models;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace InvEntry.Services;

public interface IApplicationIdentityService
{
    string CompanyName { get; }

    string? BranchName { get; }

    string ApplicationName { get; }

    string ApplicationVersion { get; }

    string WindowTitle { get; }

    Task InitializeAsync();
}

public sealed class ApplicationIdentityService : ObservableObject, IApplicationIdentityService
{
    public const string LoadingCompanyName = "Starting...";
    public const string MissingCompanyName = "Company not configured";
    public const string CompanyLoadFailedMessage = "Unable to load company configuration.";

    private readonly IOrgThisCompanyViewService _companyService;
    private string _companyName = LoadingCompanyName;
    private string? _branchName;

    public ApplicationIdentityService(IOrgThisCompanyViewService companyService)
        : this(companyService, Assembly.GetEntryAssembly() ?? typeof(ApplicationIdentityService).Assembly)
    {
    }

    public ApplicationIdentityService(IOrgThisCompanyViewService companyService, Assembly assembly)
    {
        _companyService = companyService ?? throw new ArgumentNullException(nameof(companyService));
        ArgumentNullException.ThrowIfNull(assembly);

        ApplicationName = GetApplicationName(assembly);
        ApplicationVersion = FormatVersion(assembly.GetName().Version);
    }

    public string CompanyName
    {
        get => _companyName;
        private set
        {
            if (SetProperty(ref _companyName, value))
            {
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
    }

    public string? BranchName
    {
        get => _branchName;
        private set => SetProperty(ref _branchName, value);
    }

    public string ApplicationName { get; }

    public string ApplicationVersion { get; }

    public string WindowTitle => $"{CompanyName} - {ApplicationName}";

    public async Task InitializeAsync()
    {
        CompanyName = LoadingCompanyName;
        BranchName = null;

        try
        {
            OrgThisCompanyView? company = await _companyService.GetOrgThisCompany();
            CompanyName = NormalizeCompanyName(company?.CompanyName);

            // OrgThisCompanyView currently has no company-level branch field.
            // Keep this optional until the existing configuration supplies one.
            BranchName = null;
        }
        catch (Exception ex)
        {
            CompanyName = CompanyLoadFailedMessage;
            BranchName = null;
            Serilog.Log.Warning(ex, "Unable to load application company identity");
        }
    }

    public static string FormatVersion(Version? version)
    {
        if (version is null)
        {
            return "Unknown";
        }

        return version.Revision >= 0
            ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
            : version.Build >= 0
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : $"{version.Major}.{version.Minor}";
    }

    private static string NormalizeCompanyName(string? companyName) =>
        string.IsNullOrWhiteSpace(companyName)
            ? MissingCompanyName
            : companyName.Trim();

    private static string GetApplicationName(Assembly assembly)
    {
        string? productName = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
        return string.IsNullOrWhiteSpace(productName)
            ? assembly.GetName().Name ?? "InvEntry"
            : productName.Trim();
    }
}
