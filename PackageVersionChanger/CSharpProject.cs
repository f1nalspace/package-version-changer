using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace TSP.PackageVersionChanger
{
    public class CSharpProject : ViewModelBase
    {
        /*
        '$(Configuration)|$(Platform)'=='Debug|AnyCPU'
        '$(Configuration)|$(Platform)' == 'Debug|AnyCPU'
        '$(Platform)|$(Configuration)'=='AnyCPU|Debug'
        '$(Platform)|$(Configuration)' == 'AnyCPU|Debug'
        '$(Platform)=='AnyCPU'
        '$(Platform) == 'AnyCPU'
        '$(Configuration)'=='Debug'
        '$(Configuration)' == 'Debug'
        */
        private static readonly Regex _conditionRex = new Regex(@"(\'\$\(Configuration\)\|\$\(Platform\)\'\s*==\s*\'(?<config>\w+)\|(?<platform>\w+)\')|(\'\$\(Platform\)\|\$\(Configuration\)\'\s*==\s*\'(?<platform>\w+)\|(?<config>\w+)\')|(\'\$\(Platform\)\s*==\s*\'(?<platform>\w+)\')|(\'\$\(Configuration\)\'\s*==\s*\'(?<config>\w+)\')", RegexOptions.Compiled);

        public IEnumerable<string> Configurations => _configurations;
        private readonly string[] _configurations;

        public IEnumerable<string> Platforms => _platforms;
        private readonly string[] _platforms;

        public IEnumerable<ConfigurationPlatform> ConfigPlatforms => _configPlatforms;
        private readonly ConfigurationPlatform[] _configPlatforms;

        public IEnumerable<PackageItem> Packages => _packages;
        private readonly List<PackageItem> _packages = new List<PackageItem>();

        public string DefaultConfiguration { get; }
        public string DefaultPlatform { get; }

        public string Name { get; }
        public string FilePath { get; }

        public bool IsModified { get => GetValue<bool>(); set => SetValue(value); }

        public CSharpProject(string filePath, IEnumerable<string> configurations, IEnumerable<string> platforms, string defaultConfiguration, string defaultPlatform)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            _configurations = configurations.ToArray();
            _platforms = platforms.ToArray();

            List<ConfigurationPlatform> list = new List<ConfigurationPlatform>();
            foreach (var config in configurations)
            {
                foreach (var platform in platforms)
                {
                    ConfigurationPlatform pc = new ConfigurationPlatform(config, platform);
                    if (!list.Contains(pc))
                        list.Add(pc);
                }
            }
            _configPlatforms = list.ToArray();

            Name = Path.GetFileNameWithoutExtension(filePath);
            FilePath = filePath;
            DefaultConfiguration = defaultConfiguration;
            DefaultPlatform = defaultPlatform;
            IsModified = false;
        }

        private static ConfigurationPlatform GetConditionalConfigPlatform(XmlNode itemGroupNode)
        {
            ConfigurationPlatform result = new ConfigurationPlatform();
            if (itemGroupNode.Attributes.Count > 0 &&
                itemGroupNode.Attributes.GetNamedItem("Condition") is XmlAttribute conditionAttr)
            {
                Match m = _conditionRex.Match(conditionAttr.Value);
                if (m.Success)
                {
                    string config = m.Groups["config"].Value;
                    string platform = m.Groups["platform"].Value;
                    result = new ConfigurationPlatform(config, platform, conditionAttr.Value);
                }
            }
            return result;
        }

        readonly struct PackageReferenceInfo
        {
            public string Id { get; }
            public string Version { get; }

            public PackageReferenceInfo(string id, string version)
            {
                Id = id;
                Version = version;
            }
        }

        private static PackageReferenceInfo GetPackageReferenceInfo(XmlNode packageNode)
        {
            string id = packageNode.Attributes.GetNamedItem("Include")?.InnerText;
            XmlNode includeNode = packageNode.SelectSingleNode("Include");
            if (includeNode != null && !string.IsNullOrEmpty(includeNode.InnerText))
                id = includeNode.InnerText;

            string version = packageNode.Attributes.GetNamedItem("Version")?.InnerText;
            XmlNode versionNode = packageNode.SelectSingleNode("Version");
            if (versionNode != null && !string.IsNullOrEmpty(versionNode.InnerText))
                version = versionNode.InnerText;

            return new PackageReferenceInfo(id, version);
        }

        private static bool SetPackageReferenceVersion(XmlNode packageNode, string version)
        {
            if (packageNode == null)
                throw new ArgumentNullException(nameof(packageNode));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentNullException(nameof(version));

            XmlAttribute attr = packageNode.Attributes.GetNamedItem("Version") as XmlAttribute;
            if (attr != null)
            {
                attr.Value = version;
                return true;
            }

            XmlNode versionNode = packageNode.SelectSingleNode("Version");
            if (versionNode != null)
            {
                versionNode.InnerText = version;
                return true;
            }

            return false;
        }

        public static CSharpProject LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The project file '{filePath}' does not exists");

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.PreserveWhitespace = true;
                doc.Load(filePath);
            }
            catch (Exception e)
            {
                throw new FormatException($"Failed to load project file '{filePath}'", e);
            }

            XmlNode projectNode = doc.SelectSingleNode("Project");
            if (projectNode == null)
                throw new FormatException($"No project node found in file '{filePath}'");

            List<string> configList = new List<string>();
            List<string> platformList = new List<string>();
            List<PackageItem> packageItems = new List<PackageItem>();

            string defaultConfiguration = null;
            string defaultPlatform = null;

            XmlNodeList propertyGroupNodeList = projectNode.SelectNodes("PropertyGroup");
            foreach (XmlNode propertyGroupNode in propertyGroupNodeList)
            {
                if (propertyGroupNode.Attributes.Count == 0)
                {
                    foreach (XmlNode childNode in propertyGroupNode.ChildNodes)
                    {
                        if (childNode.NodeType != XmlNodeType.Element)
                            continue;

                        string name = childNode.Name;
                        string value = childNode.InnerText;
                        if (string.IsNullOrWhiteSpace(value))
                            continue;

                        if ("Platforms".Equals(name))
                        {
                            string[] splitted = value.Split(";");
                            if (splitted.Length > 0)
                            {
                                foreach (string platform in splitted)
                                {
                                    if (!platformList.Contains(platform))
                                        platformList.Add(platform);
                                }
                            }
                        }
                        else if ("Platform".Equals(name))
                        {
                            if (!platformList.Contains(value))
                            {
                                defaultPlatform = value;
                                platformList.Add(value);
                            }
                        }

                        if ("Configurations".Equals(name))
                        {
                            string[] splitted = value.Split(";");
                            if (splitted.Length > 0)
                            {
                                foreach (string config in splitted)
                                {
                                    if (!configList.Contains(config))
                                        configList.Add(config);
                                }
                            }
                        }
                        else if ("Configuration".Equals(name))
                        {
                            if (!configList.Contains(value))
                            {
                                defaultConfiguration = value;
                                configList.Add(value);
                            }
                        }
                    }
                }
            }

            if (configList.Count == 0)
                configList.AddRange(new[] { "Debug", "Release" });
            if (string.IsNullOrWhiteSpace(defaultPlatform))
                defaultConfiguration = configList.FirstOrDefault();

            if (platformList.Count == 0)
                platformList.AddRange(new[] { "AnyCPU" });
            if (string.IsNullOrWhiteSpace(defaultPlatform))
                defaultPlatform = platformList.FirstOrDefault();

            XmlNodeList itemGroupNodeList = projectNode.SelectNodes("ItemGroup");
            foreach (XmlNode itemGroupNode in itemGroupNodeList)
            {
                ConfigurationPlatform conditionConfigPlatform = GetConditionalConfigPlatform(itemGroupNode);

                XmlNodeList packageNodeList = itemGroupNode.SelectNodes("PackageReference");
                foreach (XmlNode packageNode in packageNodeList)
                {
                    PackageReferenceInfo info = GetPackageReferenceInfo(packageNode);
                    if (string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version))
                        continue;
                    PackageItem packageItem = new PackageItem(conditionConfigPlatform, info.Id, info.Version);
                    packageItems.Add(packageItem);
                }
            }

            CSharpProject project = new CSharpProject(filePath, configList, platformList, defaultConfiguration, defaultPlatform);

            project._packages.AddRange(packageItems);

            return project;
        }

        public bool ChangePackageVersion(ConfigurationPlatform configPlatform, string id, string prevVersion, string newVersion)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(prevVersion))
                throw new ArgumentNullException(nameof(prevVersion));
            if (string.IsNullOrWhiteSpace(newVersion))
                throw new ArgumentNullException(nameof(newVersion));

            IEnumerable<PackageItem> filteredPackages = _packages.Where(p => p.Matches(configPlatform) && string.Equals(p.Id, id) && string.Equals(p.Version, prevVersion));

            if (filteredPackages.Count() == 1)
            {
                PackageItem package = filteredPackages.First();
                package.Version = newVersion;
                package.IsModified = true;
                IsModified = true;
                return true;
            }

            return false;
        }

        public void Save()
        {
            if (IsModified)
            {
                Encoding encoding = Encoding.UTF8;
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.PreserveWhitespace = true;
                    using (StreamReader reader = new StreamReader(FilePath, encoding))
                    {
                        encoding = reader.CurrentEncoding;
                        doc.Load(reader);
                    }
                }
                catch (Exception e)
                {
                    throw new FormatException($"Failed to load project file '{FilePath}'", e);
                }

                XmlNode projectNode = doc.SelectSingleNode("Project");
                if (projectNode == null)
                    throw new FormatException($"No project node found in file '{FilePath}'");

                XmlNodeList itemGroupNodeList = projectNode.SelectNodes("ItemGroup");

                foreach (PackageItem package in Packages.Where(p => p.IsModified))
                {
                    // Find package node
                    foreach (XmlNode itemGroupNode in itemGroupNodeList)
                    {
                        ConfigurationPlatform conditionConfigPlatform = GetConditionalConfigPlatform(itemGroupNode);
                        if (!package.ConfigPlatform.Equals(conditionConfigPlatform))
                            continue;
                        XmlNodeList packageNodeList = itemGroupNode.SelectNodes("PackageReference");
                        foreach (XmlNode packageNode in packageNodeList)
                        {
                            PackageReferenceInfo refInfo = GetPackageReferenceInfo(packageNode);
                            if (string.Equals(package.Id, refInfo.Id))
                                SetPackageReferenceVersion(packageNode, package.Version);
                        }
                    }

                    package.IsModified = false;
                }

                doc.PreserveWhitespace = true;

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = encoding;
                settings.OmitXmlDeclaration = true;

                using (FileStream stream = File.Create(FilePath))
                {
                    using XmlWriter writer = XmlWriter.Create(stream, settings);
                    doc.Save(writer);
                }

                IsModified = false;
            }
        }
    }
}
