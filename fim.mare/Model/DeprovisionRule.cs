// feb 16, 2016 | soren granfeldt
//	-added Deprovision class to prepare for deprov rules

using System.Collections.Generic;
using System.Xml.Serialization;

namespace FIM.MARE
{
	public enum DeprovisionOperation
	{
		[XmlEnum(Name = "Disconnect")]
		Disconnect,
		[XmlEnum(Name = "Delete")]
		Delete,
		[XmlEnum(Name = "ExplicitDisconnect")]
		ExplicitDisconnect
	}

    [XmlInclude(typeof(DeprovisionRuleCode))]
    
    public class DeprovisionRule
	{
		[XmlAttribute("DefaultOperation")]
		[XmlTextAttribute()]
		public DeprovisionOperation DefaultOperation { get; set; }

		[XmlElement("Option")]
		public List<DeprovisionOption> DeprovisionOption { get; set; }

		public DeprovisionRule()
		{
			this.DeprovisionOption = new List<DeprovisionOption>();
		}
	}

	public class DeprovisionRuleCode : DeprovisionRule { }

    public class DeprovisionOption
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlAttribute("Description")]
		public string Description { get; set; }
		[XmlAttribute("Action")]
		[XmlTextAttribute()]
		public DeprovisionOperation Action { get; set; }

		[XmlElement("Conditions")]
		public Conditions Conditions { get; set; }
	}
}
