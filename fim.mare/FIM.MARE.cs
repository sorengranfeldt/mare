// Jan 10, 2015 | soren granfeldt
//  -initial version started

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
using System.Xml.Serialization;

namespace FIM.MARE
{

    public class RulesExtension : IMASynchronization
    {
        // code from - http://blogs.msdn.com/b/sergeim/archive/2008/12/10/how-to-do-etw-logging-from-net-application.aspx 
        // debugging tools - http://msdn.microsoft.com/en-US/windows/desktop/bg162891 
        TraceSource source = new TraceSource("FIM.MARE", SourceLevels.All);

        public Configuration config = null;

        private string FIMInstallationDirectory()
        {
            return Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\FIMSynchronizationService\Parameters", false).GetValue("Path").ToString();
        }

        public RulesExtension()
        {
            source.TraceInformation("Initialize", "Enter");
#if DEBUG
            string configurationFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"FIM.MARE.config.xml");
#else
            string configurationFilePath = Path.Combine(Utils.ExtensionsDirectory, @"FIM.MARE.config.xml");
#endif
            ConfigurationManager cfg = new ConfigurationManager();
            cfg.LoadSettingsFromFile(configurationFilePath, ref config);
            config.ManagementAgent.ForEach(ma => ma.LoadAssembly());
        }

        void IMASynchronization.Initialize()
        {
        }
        void IMASynchronization.Terminate()
        {
            source.TraceInformation("Terminate");
            source.Close();
        }
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

        public void MapAttributesForImportExportDetached(string FlowRuleName, CSEntry csentry, MVEntry mventry, Direction direction)
        {
            source.TraceInformation("Enter {0} [{1}]", "MapAttributesForImportExportDetached", direction);

            string maName = csentry.MA.Name;
            ManagementAgent ma = config.ManagementAgent.Where(m => m.Name.Equals(maName)).FirstOrDefault();
            if (ma == null) throw new NotImplementedException("MA '" + maName + "' not found");
            FlowRule rule = ma.FlowRule.FirstOrDefault(r => r.Name.Equals(FlowRuleName) && r.Direction == direction);
            if (rule == null) throw new NotImplementedException(direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "'. Please note that rule names are case-sensitive.");

            #region FlowRuleCode
            if (rule.GetType().Equals(typeof(FlowRuleCode)))
            {
                InvokeFlowRuleCode(ma, rule, csentry, mventry);
                return;
            }
            #endregion
            #region FlowRuleConvertFromFileTimeUtc
            if (rule.GetType().Equals(typeof(FlowRuleConvertFromFileTimeUtc)) && direction.Equals(Direction.Import))
            {
                InvokeFlowRuleConvertFromFileTimeUtc(rule, csentry, mventry);
                return;
            }
            #endregion
            #region FlowRuleConcatenateString
            if (rule.GetType().Equals(typeof(FlowRuleConcatenateString)) && direction.Equals(Direction.Import))
            {
                InvokeFlowRuleConcatenateString(rule, csentry, mventry);
                return;
            }
            #endregion
            #region FlowRuleGUIDToString
            if (rule.GetType().Equals(typeof(FlowRuleGUIDToString)) && direction.Equals(Direction.Import))
            {
                InvokeFlowRuleGUIDToString(rule, csentry, mventry);
                return;
            }
            #endregion
            #region FlowRuleSIDToString
            if (rule.GetType().Equals(typeof(FlowRuleSIDToString)) && direction.Equals(Direction.Import))
            {
                InvokeFlowRuleSIDToString(rule, csentry, mventry);
                return;
            }
            #endregion

            #region FlowRuleToString
            if (rule.GetType().Equals(typeof(FlowRuleToString)) && direction.Equals(Direction.Import))
            {
                InvokeFlowRuleToString(rule, csentry, mventry);
                return;
            }
            #endregion

            source.TraceInformation("Enter {0} [{1}]", "MapAttributesForImportExportDetached", direction);
        }

        void IMASynchronization.MapAttributesForImport(string FlowRuleName, CSEntry csentry, MVEntry mventry)
        {
            source.TraceInformation("Enter {0}", "MapAttributesForImport");
            this.MapAttributesForImportExportDetached(FlowRuleName, csentry, mventry, Direction.Import);
            source.TraceInformation("Exit {0}", "MapAttributesForImport");
        }
        void IMASynchronization.MapAttributesForExport(string FlowRuleName, MVEntry mventry, CSEntry csentry)
        {
            source.TraceInformation("Enter {0}", "MapAttributesForExport");
            this.MapAttributesForImportExportDetached(FlowRuleName, csentry, mventry, Direction.Export);
            source.TraceInformation("Exit {0}", "MapAttributesForExport");
        }

        #region Rule implementations

        #region Target Value Helpers
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, string Value)
        {
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].Value = Value;
            else
                csentry[AttributeName].Value = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, long Value)
        {
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].IntegerValue = Value;
            else
                csentry[AttributeName].IntegerValue = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, bool Value)
        {
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].BooleanValue = Value;
            else
                csentry[AttributeName].BooleanValue = Value;
        }
        public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName, byte[] Value)
        {
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].BinaryValue = Value;
            else
                csentry[AttributeName].BinaryValue = Value;
        }
        public void DeleteTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string AttributeName)
        {
            if (direction.Equals(Direction.Import))
                mventry[AttributeName].Delete();
            else
                csentry[AttributeName].Delete();
        }
        #endregion

        public void InvokeFlowRuleGUIDToString(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleGUIDToString r = (FlowRuleGUIDToString)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                if (r.Direction.Equals(Direction.Import))
                    mventry[r.Target.Name].Value = new Guid(csentry[r.Source.Name].BinaryValue).ToString();
                else
                    csentry[r.Target.Name].Value = new Guid(mventry[r.Source.Name].BinaryValue).ToString();
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
                    if (csentry[attr.Name].IsPresent)
                    {
                        targetValue = targetValue + attr.Transform(csentry[attr.Name].Value);
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
                        mventry[r.Target.Name].Delete();
                        break;
                    case AttributeAction.SetDefault:
                        mventry[r.Target.Name].Value = r.Target.DefaultValue;
                        break;
                    default:
                        throw new DeclineMappingException("No default action");
                }
            }
            else
            {
                mventry[r.Target.Name].Value = targetValue;
            }
        }
        public void InvokeFlowRuleConvertFromFileTimeUtc(FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            FlowRuleConvertFromFileTimeUtc r = (FlowRuleConvertFromFileTimeUtc)rule;
            bool sourceValueIsPresent = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IsPresent : mventry[r.Source.Name].IsPresent;
            if (sourceValueIsPresent)
            {
                long fileTime = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].IntegerValue : mventry[r.Source.Name].IntegerValue;
                DateTime dtFileTimeUTC = DateTime.FromFileTimeUtc(fileTime);
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
                SetTargetValue(r.Direction, csentry, mventry, r.Target.Name, sid.ToString());
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
                string value = r.Direction.Equals(Direction.Import) ? csentry[r.Source.Name].Value : mventry[r.Source.Name].Value;
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
                long value = r.Direction.Equals(Direction.Import) ? long.Parse(csentry[r.Source.Name].Value) : long.Parse(mventry[r.Source.Name].Value);
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

[XmlRoot("Rules")]
public class Configuration
{
    [XmlElement("ManagementAgent")]
    public List<ManagementAgent> ManagementAgent { get; set; }
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
#region Enums
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
#endregion
#region FlowRules
[XmlInclude(typeof(FlowRuleConcatenateString)), XmlInclude(typeof(FlowRuleConvertFromFileTimeUtc)), XmlInclude(typeof(FlowRuleCode)), XmlInclude(typeof(FlowRuleGUIDToString)), XmlInclude(typeof(FlowRuleSIDToString)), XmlInclude(typeof(FlowRuleToString))]
public class FlowRule
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlIgnore]
    public Direction Direction { get; set; }

    [XmlAttribute("Direction")]
    private string DirectionAsString
    {
        get { return Direction.ToString(); }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Direction = default(Direction);
            }
            else
            {
                Direction = (Direction)Enum.Parse(typeof(Direction), value);
            }
        }
    }
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

#region Source
[XmlInclude(typeof(Attribute)), XmlInclude(typeof(Constant)), XmlInclude(typeof(Lookup))]
public class Value
{
    [XmlAttribute("ConvertToLowercase")]
    public bool ConvertToLowercase { get; set; }

    [XmlAttribute("ConvertToUppercase")]
    public bool ConvertToUppercase { get; set; }

    [XmlAttribute("Trim")]
    public bool Trim { get; set; }

    [XmlAttribute("DefaultValue")]
    public string DefaultValue { get; set; }
    [XmlIgnore]
    public AttributeAction ActionOnNullSource { get; set; }
    [XmlAttribute("ActionOnNullSource")]
    private string ActionOnNullSourceAsString
    {
        get { return ActionOnNullSource.ToString(); }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                ActionOnNullSource = default(AttributeAction);
            }
            else
            {
                ActionOnNullSource = (AttributeAction)Enum.Parse(typeof(AttributeAction), value);
            }
        }
    }

    [XmlAttribute("DateFormat")]
    public string DateFormat { get; set; }

    public string Transform(string value)
    {
        if (this.Trim) value = value.Trim();
        if (this.ConvertToUppercase) value = value.ToUpper();
        return value;
    }
}
public class Attribute : Value
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

}
public class Constant : Value
{
    [XmlAttribute("Value")]
    public string Value { get; set; }
}
public class Lookup : Value
{
}
public class SourceList
{
    [XmlElement("Source")]
    public List<Value> Source { get; set; }
}
#endregion