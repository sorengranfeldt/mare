using System.Collections.Generic;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public class MultiValueConcatenate : Transform
    {
        [XmlAttribute("Separator")]
        public string Separator { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            string returnValue = null;
            List<object> values = FromValueCollection(value);
            foreach (object val in values)
            {
                Tracer.TraceInformation("source-value {0}", val.ToString());
                returnValue = returnValue + val.ToString() + this.Separator;
            }
            returnValue = returnValue.Substring(0, returnValue.LastIndexOf(this.Separator));
            return returnValue;
        }
    }
}
