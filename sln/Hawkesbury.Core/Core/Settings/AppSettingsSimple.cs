using System;
using System.Xml;
using System.Xml.Serialization;
using Hawkesbury.Core.Collections.Generic;

namespace Hawkesbury.Core.Settings
{
    public class AppSettingsSimple : AppSettingsBase
    {
        private static Lazy<AppSettingsSimple> _Instance = new Lazy<AppSettingsSimple>(() => Load<AppSettingsSimple>(), true);
        public static AppSettingsSimple Instance => _Instance.Value;

        public string Example1 { get; set; }

        [XmlIgnore]
        public string Example2 { get; set; }

        public Tripel<string, int, double> Example3 { get; set; }

        private AppSettingsSimple() { }
        public static AppSettingsSimple GetInstance() => _Instance.Value;

        protected override void AfterLoad(XmlDocument xmlDocument)
        {
            XmlElement e = xmlDocument.DocumentElement.SelectSingleNode($"./{nameof(Example2)}") as XmlElement;
            Example2 = e?.InnerText;
        }
        protected override void BeforeSave(XmlDocument xmlDocument)
        {
            if (Example2 != null)
            {
                XmlElement e = xmlDocument.CreateElement(nameof(Example2));
                e.InnerText = Example2;
                xmlDocument.DocumentElement.AppendChild(e);
            }
        }
    }
}
