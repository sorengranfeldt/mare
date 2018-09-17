// may 2, 2018 | soren granfeldt
//  - added DateTimeAdd transform

using System;
using System.Xml.Serialization;

namespace FIM.MARE
{

    public class DateTimeAdd : Transform
	{
		[XmlAttribute("AddSeconds")]
		public int AddSeconds { get; set; }
		[XmlAttribute("AddMinutes")]
		public int AddMinutes { get; set; }
		[XmlAttribute("AddHours")]
		public int AddHours { get; set; }
		[XmlAttribute("AddDays")]
		public int AddDays { get; set; }
		[XmlAttribute("AddMonths")]
		public int AddMonths { get; set; }
		[XmlAttribute("AddYears")]
		public int AddYears { get; set; }

		public override object Convert(object value)
		{
			if (value == null) return value;
			string input = value.ToString();
			Tracer.TraceInformation("adddays {0}", AddDays);
			DateTime dateValue;
			if (DateTime.TryParse(input, out dateValue))
			{
				if (!AddSeconds.Equals(0))
				{
					dateValue = dateValue.AddSeconds(this.AddSeconds);
					Tracer.TraceInformation("date-after-addseconds {0}", dateValue);
				}
				if (!AddMinutes.Equals(0))
				{
					dateValue = dateValue.AddMinutes(this.AddMinutes);
					Tracer.TraceInformation("date-after-addminutes {0}", dateValue);
				}
				if (!AddHours.Equals(0))
				{
					dateValue = dateValue.AddHours(this.AddHours);
					Tracer.TraceInformation("date-after-addhours {0}", dateValue);
				}
				if (!AddDays.Equals(0))
				{
					dateValue = dateValue.AddDays(this.AddDays);
					Tracer.TraceInformation("date-after-adddays {0}", dateValue);
				}
				if (!AddMonths.Equals(0))
				{
					dateValue = dateValue.AddMonths(this.AddMonths);
					Tracer.TraceInformation("date-after-addmonths {0}", dateValue);
				}
				if (!AddYears.Equals(0))
				{
					dateValue = dateValue.AddYears(this.AddYears);
					Tracer.TraceInformation("date-after-addyears {0}", dateValue);
				}
				return dateValue;
			}
			else
			{
				Tracer.TraceWarning("could-not-parse-to-date {0}", 1, input);
			}
			return value;
		}
	}

}
