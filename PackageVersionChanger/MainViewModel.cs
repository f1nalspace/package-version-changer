using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace TSP.PackageVersionChanger
{
    public class MainViewModel : ViewModelBase
    {
        private IDispatcherService Dispatcher => GetService<IDispatcherService>();
        private IWindowDialogService WinDialog => GetService<IWindowDialogService>();
        private IOpenFileDialogService OpenFileDlg => GetService<IOpenFileDialogService>();

        public ObservableCollection<string> Configurations { get; }
        public ObservableCollection<string> Platforms { get; }

        public ICollectionView PackagesView { get; }
        public IEnumerable<PackageItem> Packages => _packages;
        private readonly List<PackageItem> _packages = new List<PackageItem>();

        public ICollectionView ProjectsView { get; }
        public IEnumerable<CSharpProject> Projects => _projects;
        private readonly List<CSharpProject> _projects = new List<CSharpProject>();

        public string SelectedPath
        {
            get => _selectedPath; 
            set
            {
                _selectedPath = value;
                RaisePropertyChanged(nameof(SelectedPath));
                SelectedPathChanged(value);
            }
        }
        private string _selectedPath;

        public string SelectedConfiguration
        {
            get => GetValue<string>();
            set => SetValue(value, () => UpdatedConfiguration(value));
        }
        public string SelectedPlatform { get => GetValue<string>(); set => SetValue(value, () => UpdatedPlatform(value)); }

        public ConfigurationPlatform SelectedConfigPlatform { get => GetValue<ConfigurationPlatform>(); set => SetValue(value, () => UpdatedConfigPlatform(value)); }

        public ObservableCollection<PackageItem> SelectedPackages { get; }

        public DelegateCommand OnLoadedCommand { get; }
        public DelegateCommand ChangePathCommand { get; }
        public DelegateCommand ReloadPathCommand { get; }
        public DelegateCommand<IEnumerable<PackageItem>> EditPackagesCommand { get; }

        public MainViewModel()
        {
            Configurations = new ObservableCollection<string>();
            Platforms = new ObservableCollection<string>();
            SelectedPackages = new ObservableCollection<PackageItem>();

            PackagesView = CollectionViewSource.GetDefaultView(_packages);
            PackagesView.SortDescriptions.Add(new SortDescription(nameof(PackageItem.Id), ListSortDirection.Ascending));

            ProjectsView = CollectionViewSource.GetDefaultView(_projects);
            ProjectsView.SortDescriptions.Add(new SortDescription(nameof(CSharpProject.Name), ListSortDirection.Ascending));

            OnLoadedCommand = new DelegateCommand(OnLoaded);
            ChangePathCommand = new DelegateCommand(ChangePath);
            ReloadPathCommand = new DelegateCommand(ReloadPath);
            EditPackagesCommand = new DelegateCommand<IEnumerable<PackageItem>>(EditPackages, CanEditPackages);
        }

        private void ChangePath()
        {
            OpenFileDlg.Title = "Select Solution File";
            OpenFileDlg.Filter = "Solution Files (*.sln)|*.sln";
            OpenFileDlg.FilterIndex = 0;
            OpenFileDlg.Multiselect = false;
            OpenFileDlg.InitialDirectory = SelectedPath;
            if (OpenFileDlg.ShowDialog() == true)
            {
                IFileInfo file = OpenFileDlg.File;
                SelectedPath = file.GetFullName();
            }
        }

        private async void ReloadPath() => await LoadProjectsAsync(SelectedPath);

        private bool CanEditPackages(IEnumerable<PackageItem> packages)
        {
            if (packages == null || !packages.Any())
                return false;
            IEnumerable<IGrouping<string, string>> versions = packages.Select(p => p.Version).GroupBy(p => p);
            if (versions.Count() == 1)
            {
                IGrouping<string, string> group = versions.First();
                string version = group.First();
                bool result = packages.All(p => string.Equals(p.Version, version));
                return result;
            }
            return false;
        }
        private void EditPackages(IEnumerable<PackageItem> packages)
        {
            PackageItem first = packages.First();
            string version = first.Version;

            EditPackagesViewModel viewModel = new EditPackagesViewModel(packages, version, SelectedConfigPlatform);

            if (WinDialog.ShowEditPackage(viewModel) == true)
            {
                string oldVersion = viewModel.InitialVersion;
                string newVersion = viewModel.NewVersion;
                foreach (EditPackageItemViewModel editPackage in viewModel.Packages)
                {
                    PackageItem package = editPackage.Package;
                    package.Version = newVersion;

                    foreach (CSharpProject project in package.ReferencedProjects)
                    {
                        project.ChangePackageVersion(SelectedConfigPlatform, package.Id, oldVersion, newVersion);
                    }
                }

                foreach (CSharpProject project in Projects)
                {
                    if (project.IsModified)
                        project.Save();
                }
            }
        }

        private async void SelectedPathChanged(string path)
        {
            await LoadProjectsAsync(path);
        }

        private void UpdatedConfiguration(string config)
        {
            SelectedConfigPlatform = new ConfigurationPlatform(config, SelectedPlatform);
        }

        private void UpdatedPlatform(string platform)
        {
            SelectedConfigPlatform = new ConfigurationPlatform(SelectedConfiguration, platform);
        }

        private void UpdatedConfigPlatform(ConfigurationPlatform configPlatform)
        {
            RefreshPackages(Projects, configPlatform);
        }

        private void OnLoaded()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                string path = args[1];
                SelectedPath = path;
            }
        }

        readonly struct PackageIdVersion : IEquatable<PackageIdVersion>
        {
            public string Id { get; }
            public string Version { get; }

            public PackageIdVersion(string id, string version) : this()
            {
                Id = id;
                Version = version;
            }

            public override string ToString() => $"{Id}/{Version}";

            public override int GetHashCode() => HashCode.Combine(Id?.ToLower(), Version?.ToLower());
            public bool Equals(PackageIdVersion other)
                => string.Equals(Id, other.Id, StringComparison.InvariantCultureIgnoreCase) && string.Equals(Version, other.Version, StringComparison.InvariantCultureIgnoreCase);
            public override bool Equals([NotNullWhen(true)] object obj) => obj is PackageIdVersion other && Equals(other);
        }

        private async void RefreshPackages(IEnumerable<CSharpProject> projects, ConfigurationPlatform currentConfigPlatform)
        {
            _packages.Clear();

            var allPackages = new List<PackageItem>();
            var packageMap = new Dictionary<PackageIdVersion, PackageItem>();

            foreach (CSharpProject project in projects)
            {
                IEnumerable<PackageItem> filteredPackages =
                    project.Packages.Where(p => p.Matches(currentConfigPlatform));
                foreach (PackageItem projectPackage in filteredPackages)
                {
                    string id = projectPackage.Id;
                    string version = projectPackage.Version;
                    PackageIdVersion idVer = new PackageIdVersion(id, version);

                    if (!packageMap.TryGetValue(idVer, out PackageItem rootPackage))
                    {
                        rootPackage = new PackageItem(projectPackage.ConfigPlatform, id, version);
                        packageMap.Add(idVer, rootPackage);
                        allPackages.Add(rootPackage);
                    }

                    rootPackage.AddReferencedProject(project);
                }
            }

            _packages.AddRange(allPackages);

            await Dispatcher.BeginInvoke(() => PackagesView.Refresh());
        }

        private Task LoadProjectsAsync(string path) => Task.Run(() =>
        {
            Dispatcher.Invoke(() => Configurations.Clear());
            Dispatcher.Invoke(() => Platforms.Clear());
            Dispatcher.Invoke(() => SelectedPackages.Clear());

            _packages.Clear();
            _projects.Clear();

            SelectedConfiguration = null;
            SelectedPlatform = null;

            List<CSharpProject> loadedProjects = new List<CSharpProject>();

            FileInfo[] projectFiles;

            FileInfo solutionFile = null;

            FileInfo testFile = new FileInfo(path);
            if (testFile.Exists && string.Equals(".sln", testFile.Extension, StringComparison.InvariantCultureIgnoreCase))
                solutionFile = testFile;

            if (solutionFile == null && testFile.Attributes.HasFlag(FileAttributes.Directory))
            {
                DirectoryInfo dir = new DirectoryInfo(testFile.FullName);
                FileInfo[] solutionFiles = dir.GetFiles("*.sln");
                if (solutionFiles.Length == 1)
                    solutionFile = solutionFiles[0];
            }

            var allConfigs = new List<string>();
            var allPlatforms = new List<string>();

            if (solutionFile != null)
            {
                string rootPath = solutionFile.Directory.FullName;
                VisualStudioSolution solution = VisualStudioSolution.LoadFromFile(solutionFile.FullName);
                projectFiles = solution.Projects
                    .Where(p => p.Path.EndsWith(".csproj", StringComparison.InvariantCultureIgnoreCase))
                    .Select(p => new FileInfo(Path.Combine(rootPath, p.Path)))
                    .ToArray();
                allConfigs.AddRange(solution.Configurations);
                allPlatforms.AddRange(solution.Platforms);
            }
            else
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                projectFiles = dir.GetFiles("*.csproj", SearchOption.AllDirectories);
            }

            foreach (FileInfo projectFile in projectFiles)
            {
                CSharpProject project = CSharpProject.LoadFromFile(projectFile.FullName);
                loadedProjects.Add(project);
            }

            var allPackages = new List<PackageItem>();
            var packageMap = new Dictionary<string, PackageItem>();
            foreach (CSharpProject project in loadedProjects)
            {
                foreach (var config in project.Configurations)
                {
                    if (!allConfigs.Contains(config))
                        allConfigs.Add(config);
                }

                foreach (var platform in project.Platforms)
                {
                    if (!allPlatforms.Contains(platform))
                        allPlatforms.Add(platform);
                }

                foreach (PackageItem projectPackage in project.Packages)
                {
                    string id = projectPackage.Id;
                    string version = projectPackage.Version;
                    if (!packageMap.ContainsKey(id))
                    {
                        PackageItem rootPackage = new PackageItem(projectPackage.ConfigPlatform, id, version);
                        packageMap.Add(id, rootPackage);
                        allPackages.Add(rootPackage);
                    }
                }
            }

            foreach (var config in allConfigs)
            {
                Dispatcher.Invoke(() => Configurations.Add(config));
            }

            foreach (var platform in allPlatforms)
            {
                Dispatcher.Invoke(() => Platforms.Add(platform));
            }

            foreach (CSharpProject loadedProject in loadedProjects)
            {
                _projects.Add(loadedProject);
            }

            Dispatcher.Invoke(() => ProjectsView.Refresh());

            SelectedConfiguration = allConfigs.FirstOrDefault();
            SelectedPlatform = allPlatforms.FirstOrDefault();
        });
    }
}
