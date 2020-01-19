// january 14, 2020 | soren granfeldt
//	- initial version

namespace FIM.MARE
{
    using System;
    using System.Globalization;
    using System.Xml.Serialization;

    public class ToFileTimeUTC : Transform
    {
        [XmlAttribute("FromFormat")]
        public string FromFormat { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            long returnValue = 0;

            returnValue = DateTime.ParseExact(value.ToString(), FromFormat, CultureInfo.InvariantCulture).ToFileTimeUtc();
            return returnValue;
        }
    }
}
