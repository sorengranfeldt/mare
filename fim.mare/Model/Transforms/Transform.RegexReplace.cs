using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class RegexReplace : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        [XmlAttribute("Replacement")]
        public string Replacement { get; set; }

        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : Regex.Replace(value as string, this.Pattern, this.Replacement);
        }
    }
}
