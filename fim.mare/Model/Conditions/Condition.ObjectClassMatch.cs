// july 12, 2018, soren granfeldt
//	- moved condition to seperate file

using Microsoft.MetadirectoryServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class ObjectClassMatch : ConditionBase
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }

        public override bool IsMet(CSEntry csentry, MVEntry mventry)
        {
            return Source.Equals(EvaluateAttribute.CSEntry) ? Regex.IsMatch(csentry.ObjectType, this.Pattern) : Regex.IsMatch(mventry.ObjectType, this.Pattern);
        }
    }

}
