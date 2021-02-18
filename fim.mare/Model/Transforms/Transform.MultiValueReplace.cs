using System.Collections.Generic;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class MultiValueReplace : Transform
    {
        [XmlAttribute("OldValue")]
        public string OldValue { get; set; }
        [XmlAttribute("NewValue")]
        public string NewValue { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            List<object> values = FromValueCollection(value);
            List<object> returnValues = new List<object>();
            foreach (object val in values)
            {
                Tracer.TraceInformation($"replaces {val}");
                returnValues.Add(string.IsNullOrEmpty(val as string) ? val : val.ToString().Replace(OldValue, NewValue));
            }
            return returnValues;
        }
    }
}
