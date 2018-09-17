using Microsoft.MetadirectoryServices;
using System.Linq;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class LookupMVValue : Transform
    {
        [XmlAttribute("LookupAttributeName")]
        public string LookupAttributeName { get; set; }

        [XmlAttribute("ExtractValueFromAttribute")]
        public string ExtractValueFromAttribute { get; set; }

        [XmlAttribute("MAName")]
        public string MAName { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            MVEntry mventry = Utils.FindMVEntries(LookupAttributeName, value as string, 1).FirstOrDefault();
            if (mventry != null)
            {
                if (this.ExtractValueFromAttribute.Equals("[DN]"))
                {
                    ConnectorCollection col = mventry.ConnectedMAs[MAName].Connectors;
                    if (col != null && col.Count.Equals(1))
                    {
                        value = mventry.ConnectedMAs[MAName].Connectors.ByIndex[0].DN.ToString();
                    }
                }
                else
                {
                    value = mventry[ExtractValueFromAttribute].IsPresent ? mventry[ExtractValueFromAttribute].Value : null;
                }
            }
            return value;
        }
    }
}
