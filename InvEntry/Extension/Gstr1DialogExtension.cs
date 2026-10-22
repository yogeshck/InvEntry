using DevExpress.Mvvm;
using InvEntry.Contracts.Gst;
using InvEntry.ViewModels;
using InvEntry.Views;
using System;
using System.Threading.Tasks;

namespace InvEntry.Extension;

public static class Gstr1DialogExtension
{
    /// <summary>
    /// Opens the line correction popup for a validation issue.
    /// Returns true when the correction was saved.
    /// </summary>
    public static async Task<bool> CorrectGstr1LineAsync(
        this IDialogService dialogService,
        Gstr1ValidationIssueResponse issue,
        string supplierGstin,
        string returnPeriod)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(issue);

        var dialogVM =
            DISource.Resolve<Gstr1LineCorrectionViewModel>()
            ?? throw new InvalidOperationException(
                "Gstr1LineCorrectionViewModel could not be resolved from DI.");

        await dialogVM.InitializeAsync(issue, supplierGstin, returnPeriod);

        dialogService.ShowDialog(
            null,
            $"Correct GSTR-1 Line — {issue.DocumentNbr} / Line {issue.LineNbr}",
            nameof(Gstr1LineCorrectionView),
            dialogVM);

        return dialogVM.WasSaved;
    }
}
