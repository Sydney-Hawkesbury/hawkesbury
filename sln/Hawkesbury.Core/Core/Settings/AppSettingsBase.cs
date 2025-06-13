using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Hawkesbury.Core.Collections.Generic;
using Hawkesbury.Core.ComponentModel;

namespace Hawkesbury.Core.Settings
{
    public abstract class AppSettingsBase : INotifyPropertyChanged
    {
        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        public string Path { get; }
        public string Directory { get; }

        private MruCollection<string> _MostResecentlyUsedFiles;

        /// <summary>
        /// List for most recently used files
        /// </summary>
        public MruCollection<string> MostResecentlyUsedFiles
        {
            get
            {
                if (_MostResecentlyUsedFiles == null) _MostResecentlyUsedFiles = new MruCollection<string>();
                return _MostResecentlyUsedFiles;
            }
            set => this.SetPropertyValue(ref _MostResecentlyUsedFiles, value, nameof(MostResecentlyUsedFiles));
        }

        private MruCollection<string> _MostResecentlyUsedFolders;

        /// <summary>
        /// List for most recently used folders
        /// </summary>
        public MruCollection<string> MostResecentlyUsedFolders
        {
            get
            {
                if (_MostResecentlyUsedFolders == null) _MostResecentlyUsedFolders = new MruCollection<string>();
                return _MostResecentlyUsedFolders;
            }
            set => this.SetPropertyValue(ref _MostResecentlyUsedFolders, value, nameof(MostResecentlyUsedFolders));
        }

        private ObservableDictionary<string, string> _SimpleSettings;

        /// <summary>
        /// String dictionary for simple named settings
        /// </summary>
        public ObservableDictionary<string, string> SimpleSettings
        {
            get { if (_SimpleSettings == null) _SimpleSettings = new ObservableDictionary<string, string>(); return _SimpleSettings; }
            set => this.SetPropertyValue(ref _SimpleSettings, value, nameof(SimpleSettings));
        }

        #endregion

        #region Constructors

        protected AppSettingsBase()
        {
            Path = AssemblyInfo.Instance.ConfigFilename;
            Directory = AssemblyInfo.Instance.ConfigDir;
        }

        ~AppSettingsBase()
        {
            Save();
        }

        #endregion

        #region Methods

        protected static T Load<T>() where T : AppSettingsBase
        {
            var filename = AssemblyInfo.Instance.ConfigFilename;
            T appSettings = null;
            if (File.Exists(filename))
            {
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    using (var ms = new MemoryStream(File.ReadAllBytes(filename)))
                    {
                        xmlDocument.Load(ms);
                        ms.Position = 0;
                        var serializer = new XmlSerializer(typeof(T));
                        appSettings = serializer.Deserialize(ms) as T;
                        appSettings.AfterLoad(xmlDocument);
                    }
                }
                catch (Exception ex)
                {
                }
            }
            if (appSettings == null) appSettings = Activator.CreateInstance(typeof(T), true) as T;
            return appSettings;
        }

        /// <summary>
        /// Method called after settings are loaded from file. Use it to read settings, that cannot deserialized.
        /// </summary>
        /// <param name="xmlDocument"></param>
        protected virtual void AfterLoad(XmlDocument xmlDocument) { }

        /// <summary>
        /// Method called before saving to file. Use is to store settings, that cannot be serialized.
        /// </summary>
        /// <param name="xmlDocument"></param>
        protected virtual void BeforeSave(XmlDocument xmlDocument) { }

        private void Save()
        {
            try
            {
                if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);
                XmlSerializer serializer = new XmlSerializer(GetType());
                var xmlSettings = new XmlWriterSettings
                {
                    Indent = true,
                    Encoding = Encoding.UTF8,
                    OmitXmlDeclaration = false,
                };
                XmlDocument xmlDocument = new XmlDocument();
                using (var ms = new MemoryStream())
                {
                    using (var xmlWriter = XmlWriter.Create(ms, xmlSettings))
                    {
                        xmlWriter.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"");
                        serializer.Serialize(xmlWriter, this);
                        ms.Position = 0;
                        xmlDocument.Load(ms);
                    }
                }
                BeforeSave(xmlDocument);
                xmlDocument.Save(Path);
            }
            catch (Exception ex)
            {
            }
        }

        #endregion
    }
}
