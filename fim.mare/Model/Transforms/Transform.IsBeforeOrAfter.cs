using System;
using System.Xml.Serialization;

namespace FIM.MARE
{
    public enum DateTimeRelativity
    {
        [XmlEnum(Name = "After")]
        After,
        [XmlEnum(Name = "Before")]
        Before
    }

    public class IsBeforeOrAfter : Transform
    {
        [XmlAttribute("AddDays")]
        public int AddDays { get; set; }

        [XmlAttribute("AddHours")]
        public int AddHours { get; set; }
        [XmlAttribute("AddMonths")]
        public int AddMonths { get; set; }

        [XmlAttribute("Relativity")]
        [XmlTextAttribute()]
        public DateTimeRelativity Relativity { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string input = value as string;
            DateTime dateValue;
            DateTime now = DateTime.Now;
            if (DateTime.TryParse(input, out dateValue))
            {
                bool returnValue = false;
                dateValue = dateValue.AddHours(this.AddHours);
                Tracer.TraceInformation("date-after-addhours {0}", dateValue);

                dateValue = dateValue.AddDays(this.AddDays);
                Tracer.TraceInformation("date-after-adddays {0}", dateValue);

                dateValue = dateValue.AddMonths(this.AddMonths);
                Tracer.TraceInformation("date-after-addmonths {0}", dateValue);

                if (this.Relativity == DateTimeRelativity.After)
                {
                    returnValue = now > dateValue;
                    Tracer.TraceInformation("compare-dates now: {0}, value: {1}, is-after: {2}", now, dateValue, returnValue);
                    return returnValue;
                }
                else if (this.Relativity == DateTimeRelativity.Before)
                {
                    returnValue = now < dateValue;
                    Tracer.TraceInformation("compare-dates now: {0}, value: {1}, is-before: {2}", now, dateValue, returnValue);
                    return returnValue;
                }
                Tracer.TraceInformation("is-{0}-{1}-{2}: {3}", now, this.Relativity, dateValue, returnValue);
                return returnValue;
            }
            else
            {
                Tracer.TraceWarning("could-not-parse-to-date {0}", 1, input);
            }
            return input;
        }
    }
}
