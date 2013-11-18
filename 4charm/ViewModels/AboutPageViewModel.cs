using System.Collections.Generic;
using System.Windows.Navigation;
using System.Xml;

namespace _4charm.ViewModels
{
    class AboutPageViewModel : PageViewModelBase
    {
        public string Version
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Title
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Publisher
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            Version = GetAppAttribute("Version");
            Title = GetAppAttribute("Title");
            Publisher = GetAppAttribute("Publisher");
        }

        private static string GetAppAttribute(string attributeName)
        {
            string appManifestName = "WMAppManifest.xml";
            string appNodeName = "App";

            var settings = new XmlReaderSettings();
            settings.XmlResolver = new XmlXapResolver();

            using (XmlReader rdr = XmlReader.Create(appManifestName, settings))
            {
                rdr.ReadToDescendant(appNodeName);
                if (!rdr.IsStartElement())
                {
                    throw new System.FormatException(appManifestName + " is missing " + appNodeName);
                }

                return rdr.GetAttribute(attributeName);
            }
        }
    }
}
