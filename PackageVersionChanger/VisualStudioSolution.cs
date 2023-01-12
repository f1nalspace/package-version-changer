using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TSP.PackageVersionChanger
{
    class VisualStudioSolution
    {
        // Project\(\"(?<id1>\{[0-9A-F]{8}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{12}\})\"\)\s*=\s*\"(?<name>[^\"]+)\"\s*\,\s*\"(?<path>[^\"]+)\"\s*\,\s*\"(?<id2>\{[0-9A-F]{8}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{12}\})\"
        // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "PackageVersionChanger", "PackageVersionChanger\PackageVersionChanger.csproj", "{63B9724A-8FDE-4B33-A16E-13D71ADB4C5C}"
        // Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "DISCOVERY Rapid Viewer", "Rapid Viewer\DISCOVERY Rapid Viewer.csproj", "{AED75A5A-F3FE-4E46-A4D8-563827DBB925}"
        // Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "Map.Demo", "iTeamWork.UI.Map.Demo\Map.Demo.csproj", "{D10F0202-B530-45C8-8D88-E5E83139B55A}"
        private static readonly Regex _rexProjectLine = new Regex(@"Project\(\""(?<id1>\{[0-9A-F]{8}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{12}\})\""\)\s*=\s*\""(?<name>[^\""]+)\""\s*\,\s*\""(?<path>[^\""]+)\""\s*\,\s*\""(?<id2>\{[0-9A-F]{8}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{4}\-[0-9A-F]{12}\})\""", RegexOptions.Compiled);

        // GlobalSection(SolutionConfigurationPlatforms) = preSolution
        private static readonly Regex _rexGlobalSection = new Regex(@"GlobalSection\((?<kind>\w+)\)\s*=\s*(?<value>\w+)", RegexOptions.Compiled);

        // Debug|Any CPU = Debug|Any CPU
        private static readonly Regex _rexSolutionConfigPlatformLine = new Regex(@"(?<config1>[\w ]+)\|(?<platform1>[\w ]+)\s*\=\s*(?<config2>[\w ]+)\|(?<platform2>[\w ]+)", RegexOptions.Compiled);

        public class Project
        {
            public Guid Id { get; }
            public string Name { get; }
            public string Path { get; }

            public Project(Guid id, string name, string path)
            {
                Id = id;
                Name = name;
                Path = path;
            }

            public override string ToString() => $"{Name} => {Path}";
        }

        public IEnumerable<Project> Projects => _projects;
        private readonly Project[] _projects;

        public IEnumerable<ConfigurationPlatform> ConfigPlatforms => _configPlatforms;
        private readonly ConfigurationPlatform[] _configPlatforms;

        public IEnumerable<string> Configurations => _configurations;
        private readonly string[] _configurations;

        public IEnumerable<string> Platforms => _platforms;
        private readonly string[] _platforms;

        public VisualStudioSolution(IEnumerable<Project> projects, IEnumerable<ConfigurationPlatform> configPlatforms)
        {
            if (projects == null)
                throw new ArgumentNullException(nameof(projects));
            _projects = projects.ToArray();
            _configPlatforms = configPlatforms.ToArray();
            _configurations = configPlatforms.Select(p => p.Configuration).Distinct().ToArray();
            _platforms = configPlatforms.Select(p => p.Platform).Distinct().ToArray();
        }

        enum GlobalSectionKind
        {
            None = 0,
            SolutionConfigurationPlatforms,
        }

        public static VisualStudioSolution LoadFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The solution file '{filePath}' does not exists");

            List<Project> projects = new List<Project>();
            List<ConfigurationPlatform> configPlatforms = new List<ConfigurationPlatform>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                bool inProject = false;
                bool inGlobal = false;
                bool inGlobalSection = false;
                GlobalSectionKind globalSectionKind = GlobalSectionKind.None;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.StartsWith("#") || line.Length == 0)
                        continue;

                    if (!inProject)
                    {
                        if (line.StartsWith("Project"))
                        {
                            inProject = true;
                            Match m = _rexProjectLine.Match(line);
                            if (m.Success)
                            {
                                Guid id1 = Guid.Parse(m.Groups["id1"].Value);
                                Guid id2 = Guid.Parse(m.Groups["id2"].Value);
                                string name = m.Groups["name"].Value;
                                string path = m.Groups["path"].Value;
                                Project project = new Project(id2, name, path);
                                projects.Add(project);
                            }
                            else
                            {
                                Debug.Fail($"Failed to parse project line '{line}' in solution file '{filePath}'");
                            }
                            continue;
                        }
                    }
                    else
                    {
                        if ("EndProject".Equals(line) && inProject)
                        {
                            inProject = false;
                            continue;
                        }
                    }

                    if (!inGlobal)
                    {
                        if ("Global".Equals(line))
                        {
                            inGlobal = true;
                            continue;
                        }
                    }
                    else
                    {
                        if ("EndGlobal".Equals(line))
                        {
                            inGlobal = false;
                            continue;
                        }
                        else
                        {
                            if (!inGlobalSection)
                            {
                                if (line.StartsWith("GlobalSection"))
                                {
                                    inGlobalSection = true;
                                    globalSectionKind = GlobalSectionKind.None;

                                    Match m = _rexGlobalSection.Match(line);
                                    if (m.Success)
                                    {
                                        string kind = m.Groups["kind"].Value;
                                        string value = m.Groups["value"].Value;
                                        if (Enum.TryParse(kind, false, out GlobalSectionKind kindEnum))
                                            globalSectionKind = kindEnum;
                                    }

                                    continue;
                                }
                            }
                            else
                            {
                                if ("EndGlobalSection".Equals(line))
                                {
                                    inGlobalSection = false;
                                    globalSectionKind = GlobalSectionKind.None;
                                    continue;
                                }
                                else
                                {
                                    if (globalSectionKind == GlobalSectionKind.SolutionConfigurationPlatforms)
                                    {
                                        Match m = _rexSolutionConfigPlatformLine.Match(line);
                                        if (m.Success)
                                        {
                                            string config = m.Groups["config1"].Value.Trim();
                                            string platform = m.Groups["platform1"].Value.Trim();
                                            configPlatforms.Add(new ConfigurationPlatform(config, platform));
                                        }
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            VisualStudioSolution result = new VisualStudioSolution(projects, configPlatforms);
            return result;
        }
    }
}
