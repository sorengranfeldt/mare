// july 12, 2018, soren granfeldt
//	- moved condition to seperate file

using Microsoft.MetadirectoryServices;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{

    public class SourceValueMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			string value = SourceValue(csentry, mventry);
			Tracer.TraceInformation("value-is: {0}", value);
			return string.IsNullOrEmpty(value) ? false : Regex.IsMatch(value, Pattern, RegexOptions.IgnoreCase);
		}
	}

}
