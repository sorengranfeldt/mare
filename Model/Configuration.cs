using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
