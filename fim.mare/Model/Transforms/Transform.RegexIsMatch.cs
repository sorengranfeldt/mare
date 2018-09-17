using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class RegexIsMatch : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }

        [XmlAttribute("TrueValue")]
        public string TrueValue { get; set; }

        [XmlAttribute("FalseValue")]
        public string FalseValue { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return FalseValue;
            return Regex.IsMatch(value as string, Pattern, RegexOptions.IgnoreCase) ? TrueValue : FalseValue;
        }
    }
}
