using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TSP.PackageVersionChanger
{
    class WPFWindowDialogService : IWindowDialogService
    {
        private readonly Window _parent;

        public WPFWindowDialogService(Window parent)
        {
            _parent = parent;
        }

        public bool? ShowEditPackage(EditPackagesViewModel viewModel)
        {
            EditPackagesWindow window = new EditPackagesWindow() { DataContext = viewModel, Owner = _parent };
            return window.ShowDialog();
        }
    }
}
