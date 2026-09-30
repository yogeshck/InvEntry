using DataAccess.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface ICurrentCompanyGstinProvider
{
    Task<string?> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class CurrentCompanyGstinProvider : ICurrentCompanyGstinProvider
{
    private readonly MijmsContext _context;

    public CurrentCompanyGstinProvider(MijmsContext context)
    {
        _context = context;
    }

    public Task<string?> GetAsync(CancellationToken cancellationToken = default) =>
        _context.OrgThisCompanyViews
            .AsNoTracking()
            .Where(x => x.ThisCompany == true)
            .Select(x => x.GstNbr)
            .SingleOrDefaultAsync(cancellationToken);
}
