using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class MultiValueContains : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            string returnValue = null;
            List<object> values = FromValueCollection(value);
            foreach (object val in values)
            {
                if (Regex.IsMatch(val.ToString(), this.Pattern, RegexOptions.IgnoreCase))
                {
                    Tracer.TraceInformation("Contains-value {0}", val);
                    returnValue = "true";
                    break;
                }
            }
            return returnValue;
        }
    }
}
