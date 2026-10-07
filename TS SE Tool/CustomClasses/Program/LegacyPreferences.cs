using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace TS_SE_Tool
{
    internal static class LegacyPreferences
    {
        internal static Dictionary<string, bool> Read(string path)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>(StringComparer.Ordinal);
            using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 }))
            {
                XDocument document = XDocument.Load(reader);
                foreach (XElement section in document.Descendants("TS_SE_Tool.Properties.Settings"))
                    foreach (XElement setting in section.Elements("setting"))
                    {
                        string name = (string)setting.Attribute("name");
                        if (name != "ShowSplashOnStartup" && name != "CheckUpdatesOnStartup" && name != "AutoInstallUpdates") continue;
                        if (bool.TryParse((string)setting.Element("value"), out bool value)) result[name] = value;
                    }
            }
            return result;
        }
    }
}
