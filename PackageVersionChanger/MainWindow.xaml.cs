using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TSP.PackageVersionChanger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();

            if (DataContext is ISupportServices supportServices)
            {
                supportServices.ServiceContainer.RegisterService(new WPFWindowDialogService(this));
                supportServices.ServiceContainer.RegisterService(new WPFOpenFileDialogService(this));
            }
        }

        private void OnPackagesListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not MainViewModel viewModel)
                return;

            foreach (PackageItem packageItem in e.RemovedItems ?? Array.Empty<PackageItem>())
                viewModel.SelectedPackages.Remove(packageItem);

            foreach (PackageItem packageItem in e.AddedItems ?? Array.Empty<PackageItem>())
                viewModel.SelectedPackages.Add(packageItem);


        }
    }
}
