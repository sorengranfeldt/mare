using System;
using System.Globalization;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public enum DateType
    {
        [XmlEnum(Name = "BestGuess")]
        BestGuess,
        [XmlEnum(Name = "DateTime")]
        DateTime,
        [XmlEnum(Name = "FileTimeUTC")]
        FileTimeUTC
    }
    public class FormatDate : Transform
    {
        [XmlAttribute("DateType")]
        [XmlTextAttribute()]
        public DateType DateType { get; set; }

        [XmlAttribute("FromFormat")]
        public string FromFormat { get; set; }
        [XmlAttribute("ToFormat")]
        public string ToFormat { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string returnValue = value.ToString();
            Tracer.TraceInformation("formatdate-from {0} / {1}", DateType, returnValue);
            if (DateType.Equals(DateType.FileTimeUTC))
            {
                returnValue = DateTime.FromFileTimeUtc(long.Parse(value.ToString())).ToString(ToFormat);
                return returnValue;
            }
            if (DateType.Equals(DateType.BestGuess))
            {
                returnValue = DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture).ToString(ToFormat);
                return returnValue;
            }
            if (DateType.Equals(DateType.DateTime))
            {
                returnValue = DateTime.ParseExact(value.ToString(), FromFormat, CultureInfo.InvariantCulture).ToString(ToFormat);
                return returnValue;
            }
            return returnValue;
        }
    }
}
