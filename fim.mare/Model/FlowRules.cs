// feb 10, 2016 | soren granfeldt
//	-added Description attribute to flowrule to allow for better logging information

using System.Xml.Serialization;

namespace FIM.MARE
{
	public enum Direction
	{
		[XmlEnum(Name = "Import")]
		Import,
		[XmlEnum(Name = "Export")]
		Export
	}
	public enum AttributeAction
	{
		[XmlEnum(Name = "None")]
		None,
		[XmlEnum(Name = "Delete")]
		Delete,
		[XmlEnum(Name = "SetDefault")]
		SetDefault
	}

	[XmlInclude(typeof(FlowRuleCode))]
	public class FlowRule
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlAttribute("Description")]
		public string Description { get; set; }

		[XmlAttribute("Direction")]
		[XmlTextAttribute()]
		public Direction Direction { get; set; }

		[XmlElement("Conditions")]
		public Conditions Conditions { get; set; }

		[XmlElement("SourceExpression")]
		public SourceExpression SourceExpression { get; set; }
		[XmlElement("Target")]
		public Attribute Target { get; set; }

		public FlowRule()
		{
			this.Conditions = new Conditions();
		}
	}
	public class FlowRuleCode : FlowRule
	{
	}
}
