// january 14, 2020 | soren granfeldt
//	- initial version
// january 23, 2020 | soren granfeldt
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

        [XmlAttribute("FromTimeZone")]
        public string FromTimeZone { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            long returnValue = 0;
            if (string.IsNullOrEmpty(FromTimeZone)) FromTimeZone = "UTC";

            DateTime date = DateTime.ParseExact(value.ToString(), FromFormat, CultureInfo.InvariantCulture);
            returnValue = TimeZoneInfo.ConvertTimeToUtc(date, TimeZoneInfo.FindSystemTimeZoneById(FromTimeZone)).ToFileTimeUtc();

            return returnValue;
        }
    }
}
