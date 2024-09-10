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

namespace InvEntry
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string LoggerDistince = "InvEntry-DEV";

        static App()
        {
            ApplicationThemeHelper.Preload(PreloadCategories.Controls);
            ApplicationThemeHelper.ApplicationThemeName = Theme.Win11System.Name;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Bootstrapper.Run(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ApplicationThemeHelper.SaveApplicationThemeName();
            base.OnExit(e);
        }
    }
}
