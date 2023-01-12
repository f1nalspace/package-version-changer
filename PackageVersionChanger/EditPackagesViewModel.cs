using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Packaging;
using System.Linq;
using System.Windows.Documents;
using DevExpress.Mvvm;
using DevExpress.Mvvm.UI;
using NuGet.Packaging;
using NuGet.Versioning;

namespace TSP.PackageVersionChanger
{
    public class EditPackagesViewModel : ViewModelBase
    {
        private IDispatcherService DispatcherService => GetService<IDispatcherService>();
        private IDialogCloseService CloseService => GetService<IDialogCloseService>();

        public List<EditPackageItemViewModel> Packages { get; }

        public ObservableCollection<EditProjectViewModel> Projects { get; }

        public ConfigurationPlatform ConfigPlatform { get; }
        public string InitialVersion { get; }
        public string NewVersion { get => GetValue<string>(); set => SetValue(value, () => NewVersionChanged(value)); }

        public DelegateCommand ApplyCommand { get; }
        public DelegateCommand ResetCommand { get; }

        public DelegateCommand OnCheckedPackageCommand { get; }
        public DelegateCommand OnUncheckedPackageCommand { get; }

        private EditPackagesViewModel(string version, ConfigurationPlatform configPlatform)
        {
            Projects = new ObservableCollection<EditProjectViewModel>();
            Packages = new List<EditPackageItemViewModel>();

            ConfigPlatform = configPlatform;
            InitialVersion = NewVersion = version;

            ApplyCommand = new DelegateCommand(Apply, CanApply);
            ResetCommand = new DelegateCommand(Reset);
            OnCheckedPackageCommand = new DelegateCommand(CheckPackageUpdated);
            OnUncheckedPackageCommand = new DelegateCommand(CheckPackageUpdated);
        }

        public EditPackagesViewModel() : this("1.2.3", new ConfigurationPlatform("Debug", "AnyCPU"))
        {
            CSharpProject testProject = new CSharpProject(@"C:\ProgramData\Git\rapid-viewer\iTeamWork.UI.Chart.Demo\Chart.Demo.csproj", new[] { "Debug", "Release" }, new[] { "AnyCPU" }, "Debug", "AnyCPU");

            PackageItem testPackage = new PackageItem(new ConfigurationPlatform("Debug", null), "hello-world", "1.2.3");
            testPackage.AddReferencedProject(testProject);
            Packages.Add(new EditPackageItemViewModel(testPackage));

            Projects.Add(new EditProjectViewModel(testProject));
        }

        public EditPackagesViewModel(IEnumerable<PackageItem> packages, string version, ConfigurationPlatform configPlatform) : this(version, configPlatform)
        {
            Packages.AddRange(packages.Select(p => new EditPackageItemViewModel(p)));

            IEnumerable<CSharpProject> projects = packages.SelectMany(p => p.ReferencedProjects).Distinct();
            Projects.AddRange(projects.Select(p => new EditProjectViewModel(p)));
        }

        private void CheckPackageUpdated()
        {
            IEnumerable<CSharpProject> projects = Packages.Where(p => p.IsChecked).SelectMany(p => p.Package.ReferencedProjects).Distinct();
            DispatcherService.Invoke(() => Projects.Clear());
            DispatcherService.Invoke(() => Projects.AddRange(projects.Select(p => new EditProjectViewModel(p))));
            ApplyCommand?.RaiseCanExecuteChanged();
        }

        private void NewVersionChanged(string version)
        {
            ApplyCommand?.RaiseCanExecuteChanged();
        }

        private static bool IsPackageVersion(string value) => NuGetVersion.TryParse(value, out _);

        private bool CanApply()
        {
            bool result = !string.IsNullOrWhiteSpace(NewVersion) &&
                          !string.Equals(NewVersion, InitialVersion) &&
                          IsPackageVersion(NewVersion) &&
                          Projects.Any();
            return result;
        }

        private void Apply() => CloseService.Close(true);
        private void Reset() => NewVersion = InitialVersion;
    }
}
