using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Core;
using InvEntry.Metadata;
using InvEntry.Utils;
using Serilog;
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

        public App()
        {
            foreach (Theme theme in Theme.Themes.ToList())
            {
                if (theme.Category == Theme.Office2007Category ||
                    theme.Category == Theme.MetropolisCategory ||
                    theme.Name == "DeepBlue") theme.ShowInThemeSelector = false;
            }

        }

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

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception?.Message, e.Exception);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.CloseAndFlush();
        }
    }
}
