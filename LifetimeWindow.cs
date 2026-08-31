using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace PowerDesktopApp
{
    /// <summary>
    /// Hidden window that owns WinUI dispatcher lifetime so closing Settings
    /// does not shut down the tray process.
    /// </summary>
    internal sealed class LifetimeWindow : Window
    {
        public LifetimeWindow()
        {
            Title = "VoltDeskLifetime";
            Content = new Microsoft.UI.Xaml.Controls.Grid();
            AppWindow.IsShownInSwitchers = false;
            try
            {
                if (AppWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.SetBorderAndTitleBar(false, false);
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                    presenter.IsResizable = false;
                }
            }
            catch
            {
            }

            AppWindow.MoveAndResize(new RectInt32(-32000, -32000, 1, 1));
            AppWindow.Closing += (_, e) =>
            {
                if (!_allowClose)
                    e.Cancel = true;
            };
            Activate();
            AppWindow.Hide();
        }

        private bool _allowClose;

        public void AllowClose()
        {
            _allowClose = true;
        }
    }
}
