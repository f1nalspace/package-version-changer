using System;
using System.Windows;

namespace TSP.PackageVersionChanger
{
    class WpfDialogCloseService : IDialogCloseService
    {
        private readonly Window _window;

        public WpfDialogCloseService(Window window)
        {
            if (window == null)
                throw new ArgumentException(nameof(window));
            _window = window;
        }

        public void Close(bool? dialogResult)
        {
            _window.DialogResult = dialogResult;
            _window.Close();
        }
    }
}
