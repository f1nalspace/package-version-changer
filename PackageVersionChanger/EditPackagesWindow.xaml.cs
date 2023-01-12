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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TSP.PackageVersionChanger
{
    /// <summary>
    /// Interaction logic for EditPackagesWindow.xaml
    /// </summary>
    public partial class EditPackagesWindow : Window
    {
        private readonly WPFDispatcherService _dispatcherSrv;
        private readonly WpfDialogCloseService _dlgCloseSrv;

        public EditPackagesWindow()
        {
            InitializeComponent();

            _dispatcherSrv = new WPFDispatcherService(Dispatcher);
            _dlgCloseSrv = new WpfDialogCloseService(this);

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            DialogResult = null;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ISupportServices oldSupportServices)
            {
                oldSupportServices.ServiceContainer.UnregisterService(_dispatcherSrv);
                oldSupportServices.ServiceContainer.UnregisterService(_dlgCloseSrv);
            }
            if (e.NewValue is ISupportServices newSupportServices)
            {
                newSupportServices.ServiceContainer.RegisterService(_dispatcherSrv);
                newSupportServices.ServiceContainer.RegisterService(_dlgCloseSrv);
            }
        }
    }
}
