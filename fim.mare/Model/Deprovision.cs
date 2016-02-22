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

	public class Deprovision
	{
		[XmlAttribute("DefaultAction")]
		[XmlTextAttribute()]
		public DeprovisionOperation DefaultAction { get; set; }



		[XmlElement("Rule")]
		public List<DeprovisionRule> DeprovisionRule { get; set; }

		public Deprovision()
		{
			this.DeprovisionRule = new List<DeprovisionRule>();
		}
	}

	public class DeprovisionRule
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
