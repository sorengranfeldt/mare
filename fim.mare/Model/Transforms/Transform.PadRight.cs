using System.Xml.Serialization;

namespace FIM.MARE
{
    public class PadRight : Transform
    {
        [XmlAttribute("TotalWidth")]
        public int TotalWidth { get; set; }
        [XmlAttribute("PaddingChar")]
        public string PaddingChar { get; set; }

        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().PadRight(TotalWidth, PaddingChar[0]);
        }
    }
}
