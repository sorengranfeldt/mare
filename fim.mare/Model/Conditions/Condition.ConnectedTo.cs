// july 12, 2018, soren granfeldt
//	- added ConnectedTo condition

using Microsoft.MetadirectoryServices;
using System.Xml.Serialization;

namespace FIM.MARE
{

    public class ConnectedTo : ConditionBase
    {
        [XmlAttribute("MA")]
        public string MA { get; set; }

        public override bool IsMet(CSEntry csentry, MVEntry mventry)
        {
            if (Source.Equals(EvaluateAttribute.MVEntry))
            {
                return mventry.ConnectedMAs[MA].Connectors.Count > 0;
            }
            if (Source.Equals(EvaluateAttribute.CSEntry))
            {
                throw new ObjectTypeNotSupportedException("condition-only-supported-for-mventry");
            }
            return false; // we should never get here
        }
    }

}
