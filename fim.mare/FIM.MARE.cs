// jan 10, 2015 | soren granfeldt
//  - initial version started
// jan 20, 2015 | soren granfeldt
//  - added transforms
//  - extended trace events
// jan 25, 2015 | soren granfeldt
//  - added conditions and started externals
// jan 26, 2015 | soren granfeldt
//  - added BitIsSet and BitIsNotSet flow rules
//  - added support for copying and renaming DLL and reading corresponding configuration file
// jan 29, 2015 | soren granfeldt
//  - reduced number of rules and made more generic by moving fuctionality to Transforms instead
// feb 4, 2015 | soren granfeldt
//  - fixed bug in date conversion transform
// oct 16, 2015 | soren granfeldt
//	- added static tracer through project
//	- added logging of version information to instantiation function

using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FIM.MARE
{

	public class RulesExtension : IMASynchronization
	{
		public Configuration config = null;

		public RulesExtension()
		{
			Tracer.IndentLevel = 0;
			Tracer.TraceInformation("enter-instantiation");
			Tracer.Indent();
			try
			{
				System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
				Tracer.TraceInformation("fim-mare-version {0}", fvi.FileVersion);

				string ConfigFileName = string.Concat(Path.GetFileNameWithoutExtension(this.GetType().Assembly.CodeBase), @".config.xml");
#if DEBUG
                string configurationFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), ConfigFileName);
#else
				string configurationFilePath = Path.Combine(Utils.ExtensionsDirectory, ConfigFileName);
#endif
				Tracer.TraceInformation("loading-configuration-from {0}", configurationFilePath);
				using (ConfigurationManager cfg = new ConfigurationManager())
				{
					cfg.LoadSettingsFromFile(configurationFilePath, ref config);
				}
				Tracer.TraceInformation("loading-configuration");
				config.ManagementAgent.ForEach(ma => ma.LoadAssembly());
			}
			catch (Exception ex)
			{
				Tracer.TraceError("instantiation {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Tracer.Unindent();
				Tracer.TraceInformation("exit-instantiation");
			}
		}
		void IMASynchronization.Initialize()
		{
			// intentionally left blank
		}
		void IMASynchronization.Terminate()
		{
			Tracer.IndentLevel = 0;
			Tracer.TraceInformation("enter-terminate");
			Tracer.Indent();
			try
			{
				config = null;
				Tracer.TraceInformation("pre-gc-allocated-memory '{0:n}'", GC.GetTotalMemory(true) / 1024M);
				GC.Collect();
				Tracer.TraceInformation("post-gc-allocated-memory '{0:n}'", GC.GetTotalMemory(true) / 1024M);
			}
			catch (Exception ex)
			{
				Tracer.TraceError("terminate {0}", ex.GetBaseException());
				throw;
			}
			finally
			{
				Tracer.Unindent();
				Tracer.TraceInformation("exit-terminate");
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
			Tracer.TraceInformation("enter-mapattributesforimportexportdetached [{0}]", direction);
			Tracer.Indent();

			List<FlowRule> rules = null;
			try
			{
				string maName = csentry.MA.Name;
				Tracer.TraceInformation("mvobjectid: {0}, ma: {1}, rule: {2}", mventry.ObjectID, maName, FlowRuleName);

				ManagementAgent ma = config.ManagementAgent.Where(m => m.Name.Equals(maName)).FirstOrDefault();
				if (ma == null) throw new NotImplementedException("management-agent-" + maName + "-not-found");
				rules = ma.FlowRule.Where(r => r.Name.Equals(FlowRuleName) && r.Direction.Equals(direction)).ToList<FlowRule>();
				if (rules == null) throw new NotImplementedException(direction.ToString() + "-rule-'" + FlowRuleName + "'-not-found-on-ma-" + maName);
				foreach (FlowRule r in rules) Tracer.TraceInformation("found-rule {0}", r.Name);
				FlowRule rule = rules.Where(ru => ru.Conditions.AreMet(csentry, mventry)).FirstOrDefault();
				if (rule == null) throw new DeclineMappingException("no-" + direction.ToString() + "-rule-'" + FlowRuleName + "'-not-found-on-ma-'" + maName + "'-where-conditions-were-met");

				#region FlowRuleDefault
				if (rule.GetType().Equals(typeof(FlowRule)))
				{
					InvokeFlowRule(rule, csentry, mventry);
					return;
				}
				#endregion
				#region FlowRuleCode
				if (rule.GetType().Equals(typeof(FlowRuleCode)))
				{
					InvokeFlowRuleCode(ma, rule, csentry, mventry);
					return;
				}
				#endregion

				rule = null;
			}
			catch (Exception ex)
			{
				Tracer.TraceError("mapattributesforimportexportdetached {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				if (rules != null)
				{
					rules.Clear();
					rules = null;
				}

				Tracer.Unindent();
				Tracer.TraceInformation("exit-mapattributesforimportexportdetached [{0}]", direction);
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
			Tracer.TraceInformation("enter-invokeflowrule {0}", rule.Name);
			Tracer.Indent();
			try
			{
				FlowRule r = (FlowRule)rule;
				object targetValue = null;
				foreach (Value value in r.SourceExpression.Source)
				{
					if (value.GetType().Equals(typeof(MultiValueAttribute)))
					{
						MultiValueAttribute attr = (MultiValueAttribute)value;
						object mv = attr.GetValueOrDefault(r.Direction, csentry, mventry);
						targetValue = attr.Transform(mv, TransformDirection.Source);
					}
					if (value.GetType().Equals(typeof(Attribute)))
					{
						Attribute attr = (Attribute)value;
						object concateValue = attr.GetValueOrDefault(r.Direction, csentry, mventry);
						concateValue = attr.Transform(concateValue, TransformDirection.Source);
						targetValue = targetValue as string + concateValue;
						attr = null;
						continue;
					}
					if (value.GetType().Equals(typeof(Constant)))
					{
						targetValue = targetValue + ((Constant)value).Value;
						continue;
					}
				}
				targetValue = r.Target.Transform(targetValue, TransformDirection.Target);
				r.Target.SetTargetValue(r.Direction, csentry, mventry, targetValue);
				r = null;
			}
			catch (Exception ex)
			{
				Tracer.TraceError("invokeflowrule {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				Tracer.Unindent();
				Tracer.TraceInformation("exit-invokeflowrule {0}", rule.Name);
			}
		}
		public void InvokeFlowRuleCode(ManagementAgent ma, FlowRule rule, CSEntry csentry, MVEntry mventry)
		{
			Tracer.TraceInformation("enter-invokeflowrulecode {0}", rule.Name);
			Tracer.Indent();
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
				Tracer.TraceError("invokeflowrulecode {0}", ex.GetBaseException());
				throw ex;
			}
			finally
			{
				Tracer.Unindent();
				Tracer.TraceInformation("exit-invokeflowrulecode {0}", rule.Name);
			}
		}
	}
}
