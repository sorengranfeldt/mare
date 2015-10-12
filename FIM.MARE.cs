// jan 10, 2015 | soren granfeldt
//  -initial version started
// jan 20, 2015 | soren granfeldt
//  -added transforms
//  -extended trace events
// jan 25, 2015 | soren granfeldt
//  -added conditions and started externals
// jan 26, 2015 | soren granfeldt
//  -added BitIsSet and BitIsNotSet flow rules
//  -added support for copying and renaming DLL and reading corresponding configuration file
// jan 29, 2015 | soren granfeldt
//  -reduced number of rules and made more generic by moving fuctionality to Transforms instead
// feb 4, 2015 | soren granfeldt
//  -fixed bug in date conversion transform
// feb 12, 2015 | soren granfeldt
//  -added transform LookupMVValue
// feb 12, 2015 | soren granfeldt
//	-removed trace source and replaced with built-in trace

using FIM.MARE;
using Microsoft.MetadirectoryServices;
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace FIM.MARE
{
	public class RulesExtension : IMASynchronization
	{
		public Configuration config = null;

		public RulesExtension()
		{
			Trace.IndentLevel = 0;
			Trace.TraceInformation("enter-instantiation");
			Trace.Indent();
			try
			{
				string ConfigFileName = string.Concat(Path.GetFileNameWithoutExtension(this.GetType().Assembly.CodeBase), @".config.xml");
#if DEBUG
                string configurationFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), ConfigFileName);
#else
				string configurationFilePath = Path.Combine(Utils.ExtensionsDirectory, ConfigFileName);
#endif
				Trace.TraceInformation("Loading configuration from {0}", configurationFilePath);
				using (ConfigurationManager cfg = new ConfigurationManager())
				{
					cfg.LoadSettingsFromFile(configurationFilePath, ref config);
				}
				Trace.TraceInformation("loading-configuration");
				Trace.TraceInformation("loading-assemblies");
				config.ManagementAgent.ForEach(ma => ma.LoadAssembly());
				Trace.TraceInformation("loaded-assemblies");
			}
			catch (Exception ex)
			{
				Trace.TraceError("instantiation {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-instantiation");
			}
		}
		void IMASynchronization.Initialize()
		{
			Trace.IndentLevel = 0;
			Trace.TraceInformation("enter-initialize");
			Trace.TraceInformation("exit-initialize");
		}
		void IMASynchronization.Terminate()
		{
			Trace.IndentLevel = 0;
			Trace.TraceInformation("enter-terminate");
			Trace.Indent();
			try
			{
				config = null;
				Trace.TraceInformation("pre-gc-allocated-memory '{0:n}'", GC.GetTotalMemory(true) / 1024M);
				GC.Collect();
				Trace.TraceInformation("post-gc-allocated-memory '{0:n}'", GC.GetTotalMemory(true) / 1024M);
			}
			catch (Exception ex)
			{
				Trace.TraceError("terminate {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-terminate");
			}
		}

		#region not implemented
		bool IMASynchronization.ShouldProjectToMV(CSEntry csentry, out string MVObjectType)
		{
			throw new EntryPointNotImplementedException();
		}
		DeprovisionAction IMASynchronization.Deprovision(CSEntry csentry)
		{
			throw new EntryPointNotImplementedException();
		}
		bool IMASynchronization.FilterForDisconnection(CSEntry csentry)
		{
			throw new EntryPointNotImplementedException();
		}
		void IMASynchronization.MapAttributesForJoin(string FlowRuleName, CSEntry csentry, ref ValueCollection values)
		{
			throw new EntryPointNotImplementedException();
		}
		bool IMASynchronization.ResolveJoinSearch(string joinCriteriaName, CSEntry csentry, MVEntry[] rgmventry, out int imventry, ref string MVObjectType)
		{
			throw new EntryPointNotImplementedException();
		}
		#endregion

		public void MapAttributesForImportExportDetached(string FlowRuleName, CSEntry csentry, MVEntry mventry, Direction direction)
		{
			Trace.TraceInformation("enter-{0} [{1}]", "mapattributesforimportexportdetached", direction);
			Trace.Indent();

			List<FlowRule> rules = null;
			try
			{
				string maName = csentry.MA.Name;
				Trace.TraceInformation("mvobjectid: {0}, ma: {0}, rule: {1}", mventry.ObjectID, maName, FlowRuleName);

				ManagementAgent ma = config.ManagementAgent.Where(m => m.Name.Equals(maName)).FirstOrDefault();
				if (ma == null) throw new NotImplementedException("management-agent-" + maName + "-not-found");
				rules = ma.FlowRule.Where(r => r.Name.Equals(FlowRuleName) && r.Direction.Equals(direction)).ToList<FlowRule>();
				if (rules == null) throw new NotImplementedException(direction.ToString() + "-rule-'" + FlowRuleName + "'-not-found-on-ma-" + maName);
				Trace.TraceInformation("found-{0}-matching-rule(s)", rules.Count);
				foreach (FlowRule r in rules) Trace.TraceInformation("found-rule {0}", r.Name);
				FlowRule rule = rules.Where(ru => ru.Conditions.AreMet(csentry, mventry)).FirstOrDefault();
				if (rule == null) throw new DeclineMappingException("no-" + direction.ToString() + "-rule-'" + FlowRuleName + "'-not-found-on-ma-'" + maName + "'-where-conditions-were-met");

				#region FlowRuleCode
				if (rule.GetType().Equals(typeof(FlowRuleCode)))
				{
					InvokeFlowRuleCode(ma, rule, csentry, mventry);
					return;
				}
				#endregion
				#region FlowRuleDefault
				if (rule.GetType().Equals(typeof(FlowRule)))
				{
					InvokeFlowRule(rule, csentry, mventry);
					return;
				}
				#endregion

				rule = null;
			}
			catch (Exception ex)
			{
				Trace.TraceError("mapattributesforimportexportdetached {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				if (rules != null)
				{
					rules.Clear();
					rules = null;
				}

				Trace.Unindent();
				Trace.TraceInformation("exit-{0} [{1}]", "mapattributesforimportexportdetached", direction);
			}
		}
		void IMASynchronization.MapAttributesForImport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
		{
			this.MapAttributesForImportExportDetached(FlowRuleName, csentry, mventry, Direction.Import);
		}
		void IMASynchronization.MapAttributesForExport(string FlowRuleName, MVEntry mventry, CSEntry csentry)
		{
			this.MapAttributesForImportExportDetached(FlowRuleName, csentry, mventry, Direction.Export);
		}

		public void InvokeFlowRule(FlowRule rule, CSEntry csentry, MVEntry mventry)
		{
			Trace.TraceInformation("enter-invokeflowrule");
			Trace.Indent();
			try
			{
				FlowRule r = (FlowRule)rule;
				string targetValue = null;
				foreach (Value value in r.SourceExpression.Source)
				{
					if (value.GetType().Equals(typeof(Attribute)))
					{
						Attribute attr = (Attribute)value;
						string concateValue = attr.GetValueOrDefault(r.Direction, csentry, mventry);
						concateValue = attr.Transform(concateValue);
						targetValue = targetValue + concateValue;
						attr = null;
						continue;
					}
					if (value.GetType().Equals(typeof(Constant)))
					{
						targetValue = targetValue + ((Constant)value).Value;
						continue;
					}
				}
				targetValue = r.Target.Transform(targetValue);
				r.Target.SetTargetValue(r.Direction, csentry, mventry, targetValue);
				r = null;
			}
			catch (Exception ex)
			{
				Trace.TraceError("invokeflowrule {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-invokeflowrule");
			}
		}
		public void InvokeFlowRuleCode(ManagementAgent ma, FlowRule rule, CSEntry csentry, MVEntry mventry)
		{
			Trace.TraceInformation("enter-invokeflowrulecode");
			Trace.Indent();
			try
			{
				FlowRuleCode r = (FlowRuleCode)rule;
				if (r.Direction.Equals(Direction.Import))
					ma.InvokeMapAttributesForImport(r.Name, csentry, mventry);
				else
					ma.InvokeMapAttributesForExport(r.Name, csentry, mventry);
				r = null;
			}
			catch (Exception ex)
			{
				Trace.TraceError("invokeflowrulecode {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				Trace.Unindent();
				Trace.TraceInformation("exit-invokeflowrulecode");
			}
		}
	}
}


#region Source

#endregion
