using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class MultiValueRemoveIfNotMatch : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            List<object> values = FromValueCollection(value);
            List<object> returnValues = new List<object>();
            foreach (object val in values)
            {
                if (Regex.IsMatch(val.ToString(), this.Pattern, RegexOptions.IgnoreCase))
                {
                    Tracer.TraceInformation("removing-value {0}", val);
                }
                else
                {
                    Tracer.TraceInformation("keeping-value {0}", val.ToString());
                    returnValues.Add(val);
                }
            }
            return returnValues;
        }
    }
}
