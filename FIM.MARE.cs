// Jan 10, 2015 | soren granfeldt
//  -initial version started
// Jan 20, 2015 | soren granfeldt
//  -added transforms
//  -extended trace events
// Jan 25, 2015 | soren granfeldt
//  -added conditions and externals
using Microsoft.MetadirectoryServices;
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

/*
<source name"HomeDirServer">
<Transforms>
	<Transform xsi:type="Trim"/>
	<Transform xsi:type="RegExReplace" Pattern="^[a-z]" Replacement=""/>
	<Transform xsi:type="RegExSelect"/>
		<Item Pattern="001|003" Value="Server1" />
		<Item Pattern="002" Value="Server2" />
	</Transform>
</Transforms>
</source>
 */
namespace FIM.MARE
{

    public class RulesExtension : IMASynchronization
    {
        TraceSource source = new TraceSource("FIM.MARE", SourceLevels.All);

        public Configuration config = null;

        public RulesExtension()
        {
            try
            {
                string DLLName = Path.GetFileNameWithoutExtension(this.GetType().Assembly.CodeBase); //System.Reflection.Assembly.GetExecutingAssembly().GetName();
#if DEBUG
                string configurationFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), string.Concat(DLLName, @".config.xml"));
#else
                string configurationFilePath = Path.Combine(Utils.ExtensionsDirectory, @"FIM.MARE.config.xml");
#endif
                source.TraceInformation("Loading configuration from", configurationFilePath);
                ConfigurationManager cfg = new ConfigurationManager();
                cfg.LoadSettingsFromFile(configurationFilePath, ref config);
                source.TraceInformation("Loaded configuration");
                source.TraceInformation("Loading assemblies", configurationFilePath);
                config.ManagementAgent.ForEach(ma => ma.LoadAssembly());
                source.TraceInformation("Loaded assemblies");
            }
            catch (Exception ex)
            {
                source.TraceEvent(TraceEventType.Critical, ex.HResult, ex.Message);
                throw ex;
            }
        }
        void IMASynchronization.Initialize()
        {
            source.TraceInformation("Initialize");
        }
        void IMASynchronization.Terminate()
        {
            source.TraceInformation("Terminate");
            source.Close();
        }

        #region Not Implemented
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
            try
            {
                string maName = csentry.MA.Name;
                source.TraceInformation("Enter {0} [{1}]", "MapAttributesForImportExportDetached", direction);
                source.TraceInformation("MA: '{0}', Rule '{0}'", maName, FlowRuleName);

                ManagementAgent ma = config.ManagementAgent.Where(m => m.Name.Equals(maName)).FirstOrDefault();
                if (ma == null) throw new NotImplementedException("MA '" + maName + "' not found");
                List<FlowRule> rules = ma.FlowRule.Where(r => r.Name.Equals(FlowRuleName) && r.Direction.Equals(direction)).ToList<FlowRule>();
                if (rules == null) throw new NotImplementedException(direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "'. Please note that rule names are case-sensitive.");
                source.TraceInformation("Found {0} matching rule(s)", rules.Count);
                FlowRule rule = rules.Where(ru => ru.Conditions.AreMet(csentry, mventry, source)).FirstOrDefault();
                //if (rule == null) throw new NotImplementedException("No " + direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "' where conditions were met.");
                if (rule == null) throw new DeclineMappingException("No " + direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "' where conditions were met.");

                #region FlowRuleCode
                if (rule.GetType().Equals(typeof(FlowRuleCode)))
                {
                    InvokeFlowRuleCode(ma, rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleConvertFromFileTimeUtc
                if (rule.GetType().Equals(typeof(FlowRuleConvertFromFileTimeUtc)))
                {
                    InvokeFlowRuleConvertFromFileTimeUtc(rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleConcatenateString
                if (rule.GetType().Equals(typeof(FlowRuleConcatenateString)))
                {
                    InvokeFlowRuleConcatenateString(rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleGUIDToString
                if (rule.GetType().Equals(typeof(FlowRuleGUIDToString)))
                {
                    InvokeFlowRuleGUIDToString(rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleSIDToString
                if (rule.GetType().Equals(typeof(FlowRuleSIDToString)))
                {
                    InvokeFlowRuleSIDToString(rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleToString
                if (rule.GetType().Equals(typeof(FlowRuleToString)))
                {
                    InvokeFlowRuleToString(rule, csentry, mventry);
                    return;
                }
                #endregion
                #region FlowRuleToNumber
                if (rule.GetType().Equals(typeof(FlowRuleToNumber)))
                {
                    InvokeFlowRuleToNumber(rule, csentry, mventry);
                    return;
                }
                #endregion
            }
            catch (Exception ex)
            {
                source.TraceEvent(TraceEventType.Error, ex.HResult, ex.Message);
                throw ex;
            }
            finally
            {
                source.TraceInformation("Enter {0} [{1}]", "MapAttributesForImportExportDetached", direction);
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

        #region Rule implementations

        #region Target value helpers
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, string Value)
        {
            source.TraceInformation("Set {0} [{1}] value to '{2}'", AttributeName, direction, Value);
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].Value = Value;
            else
                csentry[AttributeName].Value = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, long Value)
        {
            source.TraceInformation("Set {0} [{1}] value to '{2}'", AttributeName, direction, Value);
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].IntegerValue = Value;
            else
                csentry[AttributeName].IntegerValue = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, bool Value)
        {
            source.TraceInformation("Set {0} [{1}] value to '{2}'", AttributeName, direction, Value);
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].BooleanValue = Value;
            else
                csentry[AttributeName].BooleanValue = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, byte[] Value)
        {
            source.TraceInformation("Set {0} [{1}] value to '{2}'", AttributeName, direction, Value);
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].BinaryValue = Value;
            else
                csentry[AttributeName].BinaryValue = Value;
        }
        public void DeleteTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName)
        {
            source.TraceInformation("Deleting {0} [{1}] value", AttributeName, direction);
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].Delete();
            else
                csentry[AttributeName].Delete();
        }
        #endregion
        #region Source value helpers
        public string GetSourceOrDefaultValue(Direction direction, CSEntry csentry, MVEntry mventry, string sourceName, string defaultValue)
        {
            string returnValue = null;
            bool sourceValueIsPresent = direction.Equals(Direction.Import) ? csentry[sourceName].IsPresent : mventry[sourceName].IsPresent;
            if (sourceValueIsPresent)
            {
                returnValue = direction.Equals(Direction.Import) ? csentry[sourceName].Value : mventry[sourceName].Value;
            }
            else
            {
                returnValue = defaultValue;
            }
            return returnValue;
        }
        #endregion

        public void InvokeFlowRuleGUIDToString(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleGUIDToString r = (FlowRuleGUIDToString)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                string value = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].BinaryValue.ToString() : mventry[r.Source.Name].BinaryValue.ToString();
                value = r.Source.Transform(value, source);
                value = r.Target.Transform(value, source);
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, value);
            }
            else
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, r.Target.DefaultValue);
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
        }
        public void InvokeFlowRuleConcatenateString(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleConcatenateString r = (FlowRuleConcatenateString)rule;
            string targetValue = null;
            foreach (Value value in r.SourceList.Source)
            {
                if (value.GetType().Equals(typeof(Attribute)))
                {
                    Attribute attr = (Attribute)value;
                    bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[attr.Name].IsPresent : mventry[attr.Name].IsPresent;
                    if (sourceValueIsPresent)
                    {
                        string concateValue = attr.GetValueOrDefault(r.Direction, csentry, mventry);
                        concateValue = attr.Transform(concateValue, source);
                        targetValue = targetValue + attr.Transform(concateValue, source);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(value.DefaultValue))
                        {
                            targetValue = targetValue + value.DefaultValue;
                        }
                    }
                    continue;
                }
                if (value.GetType().Equals(typeof(Constant)))
                {
                    targetValue = targetValue + ((Constant)value).Value;
                    continue;
                }
            }
            if (string.IsNullOrEmpty(targetValue))
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, r.Target.DefaultValue);
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
            else
            {
                targetValue = r.Target.Transform(targetValue, source);
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, targetValue);
            }
        }
        public void InvokeFlowRuleConvertFromFileTimeUtc(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleConvertFromFileTimeUtc r = (FlowRuleConvertFromFileTimeUtc)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                string value = r.Source.GetValueOrDefault(r.Direction, csentry, mventry);
                value = r.Source.Transform(value, source);
                value = r.Target.Transform(value, source);
                DateTime dtFileTimeUTC = DateTime.FromFileTimeUtc(long.Parse(value));
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, dtFileTimeUTC.ToUniversalTime().ToString(r.Target.DateFormat));
            }
            else
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, r.Target.DefaultValue);
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
        }
        public void InvokeFlowRuleCode(ManagementAgent ma, FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleCode r = (FlowRuleCode)rule;
            if (r.Direction.Equals(Direction.Import))
                ma.InvokeMapAttributesForImport(r.Name, csentry, mventry);
            else
                ma.InvokeMapAttributesForExport(r.Name, csentry, mventry);
        }
        public void InvokeFlowRuleSIDToString(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            source.TraceInformation("Enter {0}", "InvokeRuleSIDToString");
            FlowRuleSIDToString r = (FlowRuleSIDToString)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                var sidInBytes = r.Direction.Equals(Direction.Import) ? (byte[])csentry[r.Source.Name].BinaryValue : (byte[])mventry[r.Source.Name].BinaryValue;
                var sid = new SecurityIdentifier(sidInBytes, 0);
                string value = r.Source.Transform(sid.ToString(), source);
                value = r.Target.Transform(value, source);
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, value);
            }
            else
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, r.Target.DefaultValue);
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
            source.TraceInformation("Exit {0}", "InvokeRuleSIDToString");
        }
        public void InvokeFlowRuleToString(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            source.TraceInformation("Enter {0}", "InvokeFlowRuleToString");
            FlowRuleToString r = (FlowRuleToString)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                string value = r.Source.GetValueOrDefault(r.Direction, csentry, mventry);
                value = r.Source.Transform(value, source);
                value = r.Target.Transform(value, source);
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, value);
            }
            else
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, r.Target.DefaultValue);
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
            source.TraceInformation("Exit {0}", "InvokeFlowRuleToString");
        }
        public void InvokeFlowRuleToNumber(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            source.TraceInformation("Enter {0}", "InvokeFlowRuleToNumber");
            FlowRuleToNumber r = (FlowRuleToNumber)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                string value = r.Source.GetValueOrDefault(r.Direction, csentry, mventry);
                value = r.Source.Transform(value, source);
                value = r.Target.Transform(value, source);
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, long.Parse(value));
            }
            else
            {
                switch (r.Target.ActionOnNullSource)
                {
                    case AttributeAction.None:
                        throw new DeclineMappingException("No default action");
                    case AttributeAction.Delete:
                        DeleteTargetValue(r.Direction, csentry, mventry, r.Target.Name);
                        break;
                    case AttributeAction.SetDefault:
                        SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, long.Parse(r.Target.DefaultValue));
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
            source.TraceInformation("Exit {0}", "InvokeFlowRuleToNumber");
        }
        #endregion
    }
}

public class ConfigurationManager
{
    public void LoadSettingsFromFile(string Filename, ref Configuration configuration)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
        StreamReader textReader = new StreamReader(Filename);
        configuration = (Configuration)serializer.Deserialize(textReader);
        textReader.Close();
    }
}

#region Configuration
[XmlRoot("Rules")]
public class Configuration
{
    [XmlElement("ManagementAgent")]
    public List<ManagementAgent> ManagementAgent { get; set; }
    [XmlElement("ExternalFiles")]
    public ExternalFiles ExternalFiles { get; set; }
}
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
    public void Load()
    {
    }
}
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
        if (string.IsNullOrEmpty(this.CustomDLL))
        {
            // nothing to do
        }
        else
        {
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

}
#endregion


#region Conditions
public enum EvaluateAttribute
{
    [XmlEnum(Name = "CSEntry")]
    CSEntry,
    [XmlEnum(Name = "MVEntry")]
    MVEntry
}
public enum ConditionOperator
{
    [XmlEnum(Name = "And")]
    And,
    [XmlEnum(Name = "Or")]
    Or
}
[XmlInclude(typeof(ObjectClassMatch)), XmlInclude(typeof(SourceValueMatch)), XmlInclude(typeof(TargetValueMatch)), XmlInclude(typeof(SubCondition))]
public class ConditionBase
{
    [XmlAttribute("Source")]
    [XmlTextAttribute()]
    public EvaluateAttribute Source { get; set; }
    [XmlAttribute("Target")]
    [XmlTextAttribute()]
    public EvaluateAttribute Target { get; set; }

    [XmlAttribute("AttributeName")]
    public string AttributeName { get; set; }

    public string SourceValue(CSEntry csentry, MVEntry mventry)
    {
        if (Source.Equals(EvaluateAttribute.CSEntry))
        {
            return csentry[AttributeName].IsPresent ? csentry[AttributeName].Value : null;
        }
        else
        {
            return mventry[AttributeName].IsPresent ? mventry[AttributeName].Value : null;
        }
    }
    public string TargetValue(CSEntry csentry, MVEntry mventry)
    {
        if (Target.Equals(EvaluateAttribute.CSEntry))
        {
            return csentry[AttributeName].IsPresent ? csentry[AttributeName].Value : null;
        }
        else
        {
            return mventry[AttributeName].IsPresent ? mventry[AttributeName].Value : null;
        }
    }
    public virtual bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        return true;
    }
}
public class SubCondition : ConditionBase
{
    [XmlAttribute]
    public ConditionOperator Operator { get; set; }
    [XmlElement("Condition")]
    public List<ConditionBase> Conditions { get; set; }
    public SubCondition()
    {
        this.Conditions = new List<ConditionBase>();
    }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        if (Operator.Equals(ConditionOperator.And))
        {
            bool met = true;
            foreach (ConditionBase condition in Conditions)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("Condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == false) break;
            }
            source.TraceInformation("All conditions {0} met", met ? "were" : "were not");
            return met;
        }
        else
        {
            bool met = false;
            foreach (ConditionBase condition in Conditions)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("Condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == true) break;
            }
            source.TraceInformation("All conditions {0} met", met ? "were" : "were not");
            return met;
        }
    }
}
public class ObjectClassMatch : ConditionBase
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        return Source.Equals(EvaluateAttribute.CSEntry) ? Regex.IsMatch(csentry.ObjectType, this.Pattern) : Regex.IsMatch(mventry.ObjectType, this.Pattern);
    }
}
public class ObjectClassNotMatch : ConditionBase
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        return Source.Equals(EvaluateAttribute.CSEntry) ? !Regex.IsMatch(csentry.ObjectType, this.Pattern) : !Regex.IsMatch(mventry.ObjectType, this.Pattern);
    }
}
public class SourceValueMatch : ConditionBase
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        string value = SourceValue(csentry, mventry);
        return string.IsNullOrEmpty(value) ? false : Regex.IsMatch(value, Pattern);
    }
}
public class TargetValueMatch : ConditionBase
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        string value = TargetValue(csentry, mventry);
        return string.IsNullOrEmpty(value) ? false : Regex.IsMatch(value, Pattern);
    }
}
public class Conditions
{
    [XmlAttribute("Operator")]
    [XmlTextAttribute()]
    public ConditionOperator Operator { get; set; }
    [XmlElement("Condition")]
    public List<ConditionBase> ConditionBase { get; set; }
    public Conditions()
    {
        this.ConditionBase = new List<ConditionBase>();
    }

    public bool AreMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        if (Operator.Equals(ConditionOperator.And))
        {
            bool met = true;
            foreach (ConditionBase condition in ConditionBase)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("Condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == false) break;
            }
            source.TraceInformation("All conditions {0} met", met ? "were" : "were not");
            return met;
        }
        else
        {
            bool met = false;
            foreach (ConditionBase condition in ConditionBase)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("Condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == true) break;
            }
            source.TraceInformation("All conditions {0} met", met ? "were" : "were not");
            return met;
        }
    }
}
#endregion
#region FlowRules

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

[XmlInclude(typeof(FlowRuleConcatenateString)), XmlInclude(typeof(FlowRuleConvertFromFileTimeUtc)), XmlInclude(typeof(FlowRuleCode)), XmlInclude(typeof(FlowRuleGUIDToString)), XmlInclude(typeof(FlowRuleSIDToString)), XmlInclude(typeof(FlowRuleToString))]
public class FlowRule
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlAttribute("Direction")]
    [XmlTextAttribute()]
    public Direction Direction { get; set; }

    [XmlElement("Conditions")]
    public Conditions Conditions { get; set; }
}
public class FlowRuleConcatenateString : FlowRule
{
    [XmlElement("SourceList")]
    public SourceList SourceList { get; set; }
    public Attribute Target { get; set; }
}
public class FlowRuleGUIDToString : FlowRule
{
    public Attribute Source { get; set; }
    public Attribute Target { get; set; }
}
public class FlowRuleSIDToString : FlowRule
{
    public Attribute Source { get; set; }
    public Attribute Target { get; set; }
}
public class FlowRuleCode : FlowRule
{
}
public class FlowRuleToString : FlowRule
{
    public Attribute Source { get; set; }
    public Attribute Target { get; set; }
}
public class FlowRuleToNumber : FlowRule
{
    public Attribute Source { get; set; }
    public Attribute Target { get; set; }
}
public class FlowRuleConvertFromFileTimeUtc : FlowRule
{
    [XmlElement("Source")]
    public Attribute Source { get; set; }

    [XmlElement("Target")]
    public Attribute Target { get; set; }
}
#endregion
#region Transforms
[XmlInclude(typeof(ToUpper)), XmlInclude(typeof(ToLower)), XmlInclude(typeof(Trim)), XmlInclude(typeof(TrimEnd)), XmlInclude(typeof(TrimStart)), XmlInclude(typeof(Replace)), XmlInclude(typeof(PadLeft)), XmlInclude(typeof(PadRight)), XmlInclude(typeof(RegexReplace)), XmlInclude(typeof(Substring)), XmlInclude(typeof(RegexSelect))]
public abstract class Transform
{
    public abstract string Convert(string value);
}

public class ToUpper : Transform
{
    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.ToUpper();
    }
}
public class ToLower : Transform
{
    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.ToLower();
    }
}
public class Trim : Transform
{
    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.Trim();
    }
}
public class TrimEnd : Transform
{
    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.TrimEnd();
    }
}
public class TrimStart : Transform
{
    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.TrimStart();
    }
}
public class Replace : Transform
{
    [XmlAttribute("OldValue")]
    public string OldValue { get; set; }
    [XmlAttribute("NewValue")]
    public string NewValue { get; set; }

    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.Replace(OldValue, NewValue);
    }
}
public class PadLeft : Transform
{
    [XmlAttribute("TotalWidth")]
    public int TotalWidth { get; set; }
    [XmlAttribute("PaddingChar")]
    public string PaddingChar { get; set; }

    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.PadLeft(TotalWidth, PaddingChar[0]);
    }
}
public class PadRight : Transform
{
    [XmlAttribute("TotalWidth")]
    public int TotalWidth { get; set; }
    [XmlAttribute("PaddingChar")]
    public string PaddingChar { get; set; }

    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : value.PadRight(TotalWidth, PaddingChar[0]);
    }
}
public class RegexReplace : Transform
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }
    [XmlAttribute("Replacement")]
    public string Replacement { get; set; }

    public override string Convert(string value)
    {
        return string.IsNullOrEmpty(value) ? value : Regex.Replace(value, this.Pattern, this.Replacement);
    }
}
public class Substring : Transform
{
    [XmlAttribute("StartIndex")]
    public int StartIndex { get; set; }
    [XmlAttribute("Length")]
    public int Length { get; set; }

    public override string Convert(string text)
    {
        return text.Length <= StartIndex ? "" : text.Length - StartIndex <= Length ? text.Substring(StartIndex) : text.Substring(StartIndex, Length);
    }
}
public class RegexSelect : Transform
{
    public override string Convert(string value)
    {
        throw new NotImplementedException();
    }
}
public class Transforms
{
    [XmlElement("Transform")]
    public List<Transform> Transform { get; set; }
}
#endregion
#region Source
[XmlInclude(typeof(Attribute)), XmlInclude(typeof(Constant))]
public class Value
{
    [XmlAttribute("DefaultValue")]
    public string DefaultValue { get; set; }

    [XmlAttribute("ActionOnNullSource")]
    [XmlTextAttribute()]
    public AttributeAction ActionOnNullSource { get; set; }

    [XmlAttribute("DateFormat")]
    public string DateFormat { get; set; }

    [XmlElement("Transforms")]
    public Transforms Transforms { get; set; }

    public string Transform(string value, TraceSource source)
    {
        if (this.Transforms != null)
        {
            source.TraceInformation("Transforming value: '{0}'", value);
            foreach (Transform t in Transforms.Transform)
            {
                source.TraceInformation("Input[{0}]: '{1}'", t.GetType(), value);
                value = t.Convert(value);
                source.TraceInformation("Output[{0}]: '{1}'", t.GetType(), value);
            }
        }
        else
        {
            source.TraceInformation("No transform entries");
        }
        source.TraceInformation("Returning value: '{0}'", value);
        return value;
    }

}
public class Attribute : Value
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

    public string GetValueOrDefault(Direction direction, CSEntry csentry, MVEntry mventry)
    {
        string value = this.DefaultValue;
        bool sourceValueIsPresent = false;
        if (Name.Equals("[DN]") || Name.Equals("[RDN]") || Name.Equals("[ObjectType]"))
        {
            sourceValueIsPresent = true;
        }
        else
        {
            sourceValueIsPresent = direction.Equals(Direction.Import) ? csentry[Name].IsPresent : mventry[Name].IsPresent;
        }
        if (sourceValueIsPresent)
        {
            if (direction.Equals(Direction.Import))
            {
                switch (Name)
                {
                    case "[DN]":
                        value = csentry.DN.ToString();
                        break;
                    case "[RDN]":
                        value = csentry.RDN;
                        break;
                    case "[ObjectType]":
                        value = csentry.ObjectType;
                        break;
                    default:
                        value = csentry[Name].Value;
                        break;
                }
            }
            else
            {
                switch (Name)
                {
                    case "[DN]":
                        value = mventry.ObjectID.ToString();
                        break;
                    case "[ObjectType]":
                        value = mventry.ObjectType;
                        break;
                    case "[RDN]":
                        throw new Exception("[RDN] is not valid on MVEntry");
                    default:
                        value = mventry[Name].Value;
                        break;
                }
            }
        }
        return value;
    }
}
public class Constant : Value
{
    [XmlAttribute("Value")]
    public string Value { get; set; }
}
public class SourceList
{
    [XmlElement("Source")]
    public List<Value> Source { get; set; }
}
#endregion