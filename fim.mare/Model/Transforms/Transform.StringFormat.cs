using System.Xml.Serialization;

namespace FIM.MARE
{
    public class StringFormat : Transform
    {
        [XmlAttribute("ConvertToNumber")]
        public bool ConvertToNumber { get; set; }

        [XmlAttribute("Format")]
        public string Format { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            try
            {
                if (this.ConvertToNumber)
                {
                    return System.Convert.ToInt64(value).ToString(this.Format);
                }
                return string.Format(this.Format, value);
            }
            catch (System.FormatException fe)
            {
                Tracer.TraceError("unable-to-format-string", fe);
                return value;
            }

        }
    }
}
