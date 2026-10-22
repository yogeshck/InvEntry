using DevExpress.Xpf.Core;
using DevExpress.Mvvm;
using DevExpress.Xpf.Docking;
using InvEntry.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InvEntry
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ThemedWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // The flyout is a popup window: it does not follow the main window.
            LocationChanged += (_, _) => CloseRdFlyout();
            SizeChanged += (_, _) => CloseRdFlyout();
        }

        // =========================================================
        // FINANCE > RD FLYOUT
        // =========================================================

        private void OnRdMenuPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Handling the press stops the button's Click, so the hamburger
            // neither navigates nor closes the Finance flyout. It must be the
            // press, not the release: a button captures the mouse on press and
            // only lets go in its own release handling, so a handled release
            // would leave RD holding the mouse and swallowing the next click.
            e.Handled = true;

            if (RdFlyout.IsOpen)
                CloseRdFlyout();
            else
                OpenRdFlyout(focusFirstItem: false);
        }

        private void OnRdMenuPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Space or Key.Right)
            {
                e.Handled = true;
                OpenRdFlyout(focusFirstItem: true);
            }
        }

        private void OnRdMenuIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Finance flyout closed. Keep RD open while the mouse is on it,
            // or a click that is in progress there would be lost.
            if (!RdMenuButton.IsVisible && !RdFlyout.IsMouseOver)
                CloseRdFlyout();
        }

        private void OnRdFlyoutMouseLeave(object sender, MouseEventArgs e)
        {
            if (!RdMenuButton.IsVisible)
                CloseRdFlyout();
        }

        private void OnSubMenuItemMouseEnter(object sender, MouseEventArgs e)
        {
            if (!ReferenceEquals(sender, RdMenuButton))
                CloseRdFlyout();
        }

        private void OnRdItemClick(object sender, RoutedEventArgs e)
        {
            CloseRdFlyout();
            FinanceMenu.HideFlyoutCommand?.Execute(null);

            // Same route as the rest of the app: FrameNavigationService resolves
            // the page name through the view locator.
            if (DataContext is ViewModels.MainWindowViewModel vm && ((Button)sender).CommandParameter is string page)
                vm.NavigationService.Navigate(page);
        }

        private void OpenRdFlyout(bool focusFirstItem)
        {
            RdFlyout.PlacementTarget = RdMenuButton;
            RdFlyout.IsOpen = true;

            if (focusFirstItem && RdFlyout.Child is Border { Child: Panel items } && items.Children.Count > 0)
                items.Children[0].Focus();
        }

        private void CloseRdFlyout() => RdFlyout.IsOpen = false;
    }
}
