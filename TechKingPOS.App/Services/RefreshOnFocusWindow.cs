using System;
using System.Windows;

namespace TechKingPOS.App.Services
{
    public abstract class RefreshOnFocusWindow : Window
    {
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            Refresh();
        }

        protected abstract void Refresh();
    }
}
