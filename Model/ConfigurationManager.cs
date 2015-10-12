using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FIM.MARE
{
	public class ConfigurationManager : IDisposable
	{
		public void LoadSettingsFromFile(string Filename, ref Configuration configuration)
		{
			XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
			StreamReader textReader = new StreamReader(Filename);
			configuration = (Configuration)serializer.Deserialize(textReader);
			textReader.Close();
			textReader.Dispose();
		}
		public void Dispose()
		{
		}
	}
}
