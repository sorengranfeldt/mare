using System.Collections.Generic;
using System.Xml.Serialization;

namespace FIM.MARE
{
	[XmlRoot("Rules")]
	public class Configuration
	{
		[XmlElement("ManagementAgent")]
		public List<ManagementAgent> ManagementAgent { get; set; }
		[XmlElement("ExternalFiles")]
		public ExternalFiles ExternalFiles { get; set; }
	}
}
