// january 14, 2020 | soren granfeldt
//	- initial version

namespace FIM.MARE
{
    using System;
    using System.Xml.Serialization;
    public class Base64ToGUIDFormat : Transform
    {
        [XmlAttribute("FormatSpecifier")]
        public string FormatSpecifier { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            Guid guid = new Guid(System.Convert.FromBase64String(value as string));
            if (string.IsNullOrEmpty(FormatSpecifier))
                return guid;
            else
                return guid.ToString(FormatSpecifier);
        }
    }

}
