using DevExpress.Mvvm;
using System;

namespace TSP.PackageVersionChanger
{
    public class EditPackageItemViewModel : ViewModelBase
    {
        public PackageItem Package { get; }

        public bool IsChecked { get => GetValue<bool>(); set => SetValue(value); }

        public EditPackageItemViewModel(PackageItem package)
        {
            if (package == null)
                throw new ArgumentNullException(nameof(package));
            Package = package;
            IsChecked = true;
        }

        public override string ToString() => Package.Id;
    }
}
