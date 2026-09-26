using InvEntry.Models;
using InvEntry.Services;
using System.Reflection;

namespace InvEntry.Test;

public class ApplicationIdentityServiceTests
{
    [Test]
    public async Task InitializeAsync_UsesConfiguredCompanyName()
    {
        var service = CreateService(new StubCompanyService(
            new OrgThisCompanyView { CompanyName = "  Matha Jewellery  " }));

        await service.InitializeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(service.CompanyName, Is.EqualTo("Matha Jewellery"));
            Assert.That(service.BranchName, Is.Null);
            Assert.That(service.WindowTitle, Is.EqualTo("Matha Jewellery - InvEntry.Test"));
        });
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task InitializeAsync_UsesFallbackForMissingCompanyName(string? companyName)
    {
        var service = CreateService(new StubCompanyService(
            new OrgThisCompanyView { CompanyName = companyName }));

        await service.InitializeAsync();

        Assert.That(service.CompanyName, Is.EqualTo(ApplicationIdentityService.MissingCompanyName));
    }

    [Test]
    public void InitializeAsync_UsesFallbackWhenCompanyLookupFails()
    {
        var service = CreateService(new StubCompanyService(new InvalidOperationException("Unavailable")));

        Assert.DoesNotThrowAsync(service.InitializeAsync);
        Assert.That(service.CompanyName, Is.EqualTo(ApplicationIdentityService.MissingCompanyName));
    }

    [TestCase(1, 2, 3, 4, "1.2.3.4")]
    [TestCase(9, 0, -1, -1, "9.0")]
    public void FormatVersion_FormatsAvailableAssemblyComponents(
        int major,
        int minor,
        int build,
        int revision,
        string expected)
    {
        var version = build < 0
            ? new Version(major, minor)
            : revision < 0
                ? new Version(major, minor, build)
                : new Version(major, minor, build, revision);

        Assert.That(ApplicationIdentityService.FormatVersion(version), Is.EqualTo(expected));
    }

    [Test]
    public void FormatVersion_HandlesMissingMetadata()
    {
        Assert.That(ApplicationIdentityService.FormatVersion(null), Is.EqualTo("Unknown"));
    }

    private static ApplicationIdentityService CreateService(IOrgThisCompanyViewService companyService) =>
        new(companyService, Assembly.GetExecutingAssembly());

    private sealed class StubCompanyService : IOrgThisCompanyViewService
    {
        private readonly OrgThisCompanyView? _company;
        private readonly Exception? _exception;

        public StubCompanyService(OrgThisCompanyView company)
        {
            _company = company;
        }

        public StubCompanyService(Exception exception)
        {
            _exception = exception;
        }

        public Task<OrgThisCompanyView> GetOrgThisCompany() =>
            _exception is null
                ? Task.FromResult(_company!)
                : Task.FromException<OrgThisCompanyView>(_exception);
    }
}
