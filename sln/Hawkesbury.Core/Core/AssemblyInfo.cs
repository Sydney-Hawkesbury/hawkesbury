using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Hawkesbury.Core.Linq;
using Hawkesbury.Core.Text.Translations;

namespace Hawkesbury.Core
{
    public sealed class AssemblyInfo
    {
        #region Properties

        private static readonly Lazy<AssemblyInfo> _Instance = new Lazy<AssemblyInfo>(() => new AssemblyInfo(), true);
        public static AssemblyInfo Instance => _Instance.Value;

        public Assembly Assembly { get; }
        public string Location => Assembly.Location;
        public string Directory { get; }
        public string Name { get; }
        public string Extension { get; }

        private readonly string _Title;
        private string _TitleKey;
        public string Title => _TitleKey?.Translate() ?? _Title;

        private readonly string _Description;
        private string _DescriptionKey;
        public string Description => _DescriptionKey?.Translate() ?? _Description;

        public string Company { get; }
        public string Copyright { get; }
        public string Version { get; }
        public string Framework { get; }
        public string ProcessorArchitecture { get; }
        public Guid Guid { get; }
        public DateTime? BuildTime { get; private set; }

        public string ConfigDir { get; }
        public string ConfigFilename { get; }

        public Dictionary<string, string> Metadata { get; }

        #endregion

        #region Constructors

        private AssemblyInfo() : this(Assembly.GetEntryAssembly()) { }

        private AssemblyInfo(Assembly assembly)
        {
            Assembly = assembly;
            Directory = Path.GetDirectoryName(Location);
            Name = Path.GetFileNameWithoutExtension(Location);
            Extension = Path.GetExtension(Location).TrimStart('.');

            _Title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            _Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;

            Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
            Version = assembly.GetCustomAttribute<AssemblyVersionAttribute>()?.Version;
            Framework = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkDisplayName;
            string guid = assembly.GetCustomAttribute<GuidAttribute>()?.Value;
            Guid = string.IsNullOrWhiteSpace(guid) ? Guid.NewGuid() : new Guid(guid);

            ProcessorArchitecture = $"{assembly.GetName().ProcessorArchitecture} ({System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture})";

            Metadata = new Dictionary<string, string>();
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ForEach(a =>
            {
                switch (a.Key)
                {
                    case nameof(BuildTime):
                        {
                            DateTime dt;
                            if (DateTime.TryParse(a.Value, out dt)) BuildTime = dt;
                            break;
                        }
                    case "TitleKey":
                        _TitleKey = a.Value; break;
                    case "DescriptionKey":
                        _DescriptionKey = a.Value; break;
                    default:
                        Metadata[a.Key] = a.Value; break;
                }
            });

            ConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Company ?? string.Empty, Name);
            ConfigFilename = Path.Combine(ConfigDir, "AppConfig.xml");
        }

        public static AssemblyInfo GetInstance(Assembly assembly = null)
        {
            if (assembly == null) return Instance;
            if (assembly == Assembly.GetEntryAssembly()) return Instance;
            return new AssemblyInfo(assembly);
        }

        #endregion
    }
}
