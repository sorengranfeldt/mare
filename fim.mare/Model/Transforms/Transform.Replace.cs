using System.Xml.Serialization;

namespace FIM.MARE
{
    public class Replace : Transform
    {
        [XmlAttribute("OldValue")]
        public string OldValue { get; set; }
        [XmlAttribute("NewValue")]
        public string NewValue { get; set; }
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().Replace(OldValue, NewValue);
        }
    }
}
 

