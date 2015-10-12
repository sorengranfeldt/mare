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

		public void InvokeMapAttributesForImport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
		{
			instance.MapAttributesForImport(FlowRuleName, csentry, mventry);
		}
		public void InvokeMapAttributesForExport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
		{
			instance.MapAttributesForExport(FlowRuleName, mventry, csentry);
		}
		public void LoadAssembly()
		{
			Trace.TraceInformation("enter-loadassembly");
			Trace.Indent();
			try
			{
				if (string.IsNullOrEmpty(this.CustomDLL))
				{
					// nothing to do
				}
				else
				{
					Trace.TraceInformation("loading-assembly {0}", Path.Combine(Utils.ExtensionsDirectory, this.CustomDLL));
#if DEBUG
            this.Assembly = Assembly.LoadFile(Path.Combine(System.IO.Directory.GetCurrentDirectory(), this.CustomDLL));
#else
					this.Assembly = Assembly.LoadFile(Path.Combine(Utils.ExtensionsDirectory, this.CustomDLL));
#endif
					Type[] types = Assembly.GetExportedTypes();
					Type type = types.Where(u => u.GetInterface("Microsoft.MetadirectoryServices.IMASynchronization") != null).FirstOrDefault();
					if (type != null)
					{
						instance = Activator.CreateInstance(type) as IMASynchronization;
					}
				}
			}
			catch (Exception ex)
			{
				Trace.TraceError("loadassembly {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-loadassembly");
			}
		}
	}
}
