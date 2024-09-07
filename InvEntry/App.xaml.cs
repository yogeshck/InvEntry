using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using InvEntry.Metadata;
using InvEntry.Utils;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Tern.MI.InvEntry.Models;

namespace InvEntry
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            CompatibilitySettings.UseLightweightThemes = true;
            ApplicationThemeHelper.Preload(PreloadCategories.Core);

            MetadataLocator.Default = MetadataLocator.Create()
                .AddMetadata<InvoiceHeader, InvoiceHeaderMetadata>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //DISource.Resolver = Resolve;
        }
    }
}
