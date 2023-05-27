﻿using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace FIM.MARE
{
	public class ManagementAgent
	{
		[XmlIgnore]
		Assembly Assembly = null;
		[XmlIgnore]
		IMASynchronization instance = null;
		[XmlAttribute("Name")]
		public string Name { get; set; }
		[XmlElement("CustomDLL")]
		public string CustomDLL { get; set; }
		[XmlElement("FlowRule")]
		public List<FlowRule> FlowRule { get; set; }
        [XmlElement("JoinRule")]
        public List<JoinRule> JoinRule { get; set; }
        [XmlElement("DeprovisionRule")]
        public DeprovisionRule DeprovisionRule { get; set; }

        public void InvokeMapAttributesForImport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
		{
			instance.MapAttributesForImport(FlowRuleName, csentry, mventry);
		}
		public void InvokeMapAttributesForExport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
		{
			instance.MapAttributesForExport(FlowRuleName, mventry, csentry);
		}
        public DeprovisionAction InvokeDeprovision(CSEntry csentry)
        {
			return instance.Deprovision(csentry);
        }

        public void LoadAssembly()
		{
			if (string.IsNullOrEmpty(this.CustomDLL))
			{
				return;
			}
			Tracer.TraceInformation("enter-loadassembly");
			try
			{
				{
					Tracer.TraceInformation("loading-assembly {0}", Path.Combine(Utils.ExtensionsDirectory, this.CustomDLL));
#if DEBUG
            this.Assembly = Assembly.LoadFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), this.CustomDLL));
#else
					this.Assembly = Assembly.LoadFile(Path.Combine(Utils.ExtensionsDirectory, this.CustomDLL));
#endif
					Type[] types = Assembly.GetExportedTypes();
					Type type = types.FirstOrDefault(u => u.GetInterface("Microsoft.MetadirectoryServices.IMASynchronization") != null);
					if (type != null)
					{
						instance = Activator.CreateInstance(type) as IMASynchronization;
						instance.Initialize();
					}
				}
			}
			catch (Exception ex)
			{
				Tracer.TraceError("loadassembly {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Tracer.TraceInformation("exit-loadassembly");
			}
		}
	}
}
