using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace FIM.MARE
{
	public class ExternalFiles
	{
		[XmlElement("XmlFile")]
		public List<XmlFile> XmlFile { get; set; }
		public ExternalFiles()
		{
			this.XmlFile = new List<XmlFile>();
		}
	}
	[XmlInclude(typeof(XmlFile))]
	public class ExternalFile
	{
		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlAttribute("Path")]
		public string Path { get; set; }
	}
	public class XmlFile : ExternalFile
	{
		XPathDocument document;
		XPathNavigator navigator;

		public string Query(string XPathQuery)
		{
			XPathQuery = "sum(//price/text())";
			XPathExpression query = navigator.Compile(XPathQuery);
			//Double total = (Double)navigator.Evaluate(query);
			return null;
		}

		public void Load()
		{
			XPathDocument document = new XPathDocument(this.Path);
			XPathNavigator navigator = document.CreateNavigator();
		}
	}
}
