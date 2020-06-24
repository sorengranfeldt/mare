using System.Xml.Serialization;

namespace FIM.MARE
{
    public class ConvertFromTrueFalse : Transform
    {
        [XmlAttribute("TrueValue")]
        public string TrueValue { get; set; }
        [XmlAttribute("FalseValue")]
        public string FalseValue { get; set; }
        [XmlAttribute("MissingValue")]
        public string MissingValue { get; set; }
        public override object Convert(object value)
        {
            if (value is null) return MissingValue;
            if (bool.Parse(value as string)) return TrueValue;
            if (!bool.Parse(value as string)) return FalseValue;
            return value;
        }
    }
}


