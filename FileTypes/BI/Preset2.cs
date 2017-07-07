using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace BSU.Sync.FileTypes.BI
{
    [XmlRoot(ElementName = "addons-presets")]
    public class Preset2
    {
        [XmlElement(ElementName = "last-update5")]
        public DateTime LastUpdated;

        [XmlArray(ElementName = "published-ids")]
        [XmlArrayItem(ElementName = "id")]
        public List<String> PublishedId = new List<String>();

    }
}
