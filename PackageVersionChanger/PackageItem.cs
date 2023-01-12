using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TSP.PackageVersionChanger
{
    public class PackageItem : ViewModelBase
    {
        public ConfigurationPlatform ConfigPlatform { get; }

        public string Id { get; }
        public string Version { get => GetValue<string>(); set => SetValue(value); }
        public bool IsModified { get => GetValue<bool>(); set => SetValue(value); }

        public IEnumerable<CSharpProject> ReferencedProjects => _referencedProjects;
        private readonly List<CSharpProject> _referencedProjects = new List<CSharpProject>();

        public string NamesOfReferencedProjects
        {
            get => GetValue<string>();
            private set => SetValue(value);
        }

        public PackageItem(ConfigurationPlatform configPlatform, string id, string version)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException(nameof(version));
            ConfigPlatform = configPlatform;
            Id = id;
            Version = version;
            IsModified = false;
        }

        internal void AddReferencedProject(CSharpProject project)
        {
            _referencedProjects.Add(project);
            NamesOfReferencedProjects = string.Join(", ", _referencedProjects.Select(p => p.Name).OrderBy(p => p));
        }

        public bool Matches(ConfigurationPlatform testConfigPlatform)
            => (string.IsNullOrEmpty(ConfigPlatform.Configuration) || string.Equals(ConfigPlatform.Configuration, testConfigPlatform.Configuration)) &&
               (string.IsNullOrEmpty(ConfigPlatform.Platform) || string.Equals(ConfigPlatform.Platform, testConfigPlatform.Platform));

        public override string ToString() => $"{Id}/{Version} [{ConfigPlatform}]";
    }
}
