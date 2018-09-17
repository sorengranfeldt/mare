using System.Xml.Serialization;

namespace FIM.MARE
{
    public class PadLeft : Transform
    {
        [XmlAttribute("TotalWidth")]
        public int TotalWidth { get; set; }
        [XmlAttribute("PaddingChar")]
        public string PaddingChar { get; set; }

        public override object Convert(object value)
        {
            PaddingChar = string.IsNullOrEmpty(PaddingChar) ? " " : PaddingChar;
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().PadLeft(TotalWidth, PaddingChar[0]);
        }
    }
}
