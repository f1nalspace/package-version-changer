using System;

namespace TSP.PackageVersionChanger
{
    public readonly struct ConfigurationPlatform : IEquatable<ConfigurationPlatform>
    {
        public string Configuration { get; }
        public string Platform { get; }
        public string Condition { get; }

        public ConfigurationPlatform(string configuration, string platform, string condition = null)
        {
            Configuration = !string.IsNullOrEmpty(configuration) ? configuration : null;
            Platform = !string.IsNullOrEmpty(platform) ? platform : null;
            Condition = !string.IsNullOrEmpty(condition) ? condition : null;
        }

        public override string ToString() => $"{Configuration}/{Platform}";

        public override int GetHashCode()
            => HashCode.Combine(Configuration, Platform);

        public bool Equals(ConfigurationPlatform other)
            => string.Equals(Configuration, other.Configuration) && 
               string.Equals(Platform, other.Platform);

        public override bool Equals(object obj)
            => obj is ConfigurationPlatform platformConfiguration && Equals(platformConfiguration);
    }
}
