using System.Xml.Serialization;

namespace FIM.MARE
{
    public class ReplaceAfter : Transform
    {
        [XmlAttribute("IndexOf")]
        public string IndexOf { get; set; }
        [XmlAttribute("ReplaceValue")]
        public string ReplaceValue { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;
            string s = value.ToString();
            Tracer.TraceInformation("s {0}", s);
            int idx = s.IndexOf(IndexOf);
            Tracer.TraceInformation("idx {0}", idx);
            s = string.Concat(s.Remove(idx + IndexOf.Length), ReplaceValue);
            return s;
        }
    }
}
