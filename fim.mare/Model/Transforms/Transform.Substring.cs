using System.Xml.Serialization;

namespace FIM.MARE
{
    public class Substring : Transform
    {
        [XmlAttribute("StartIndex")]
        public int StartIndex { get; set; }
        [XmlAttribute("Length")]
        public int Length { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string val = value as string;
            return val.Length <= StartIndex ? "" : val.Length - StartIndex <= Length ? val.Substring(StartIndex) : val.Substring(StartIndex, Length);
        }
    }
}
