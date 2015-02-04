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
        TraceSource traceSource = new TraceSource("FIM.MARE", SourceLevels.All);

        public Configuration config = null;

        public RulesExtension()
        {
            try
            {
                string ConfigFileName = string.Concat(Path.GetFileNameWithoutExtension(this.GetType().Assembly.CodeBase), @".config.xml"); //System.Reflection.Assembly.GetExecutingAssembly().GetName();
#if DEBUG
                string configurationFilePath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), ConfigFileName);
#else
                string configurationFilePath = Path.Combine(Utils.ExtensionsDirectory, ConfigFileName);
#endif
                traceSource.TraceInformation("Loading configuration from {0}", configurationFilePath);
                ConfigurationManager cfg = new ConfigurationManager();
                cfg.LoadSettingsFromFile(configurationFilePath, ref config);
                traceSource.TraceInformation("Loaded configuration");
                traceSource.TraceInformation("Loading assemblies");
                config.ManagementAgent.ForEach(ma => ma.LoadAssembly());
                traceSource.TraceInformation("Loaded assemblies");
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "{0}: {1}", ex.GetType(), ex.Message);
                throw ex;
            }
        }
        void IMASynchronization.Initialize()
        {
            traceSource.TraceInformation("Initialize");
        }
        void IMASynchronization.Terminate()
        {
            traceSource.TraceInformation("Terminate");
            traceSource.Close();
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
            try
            {
                string maName = csentry.MA.Name;
                traceSource.TraceInformation("Enter {0} [{1}]", "MapAttributesForImportExportDetached", direction);
                traceSource.TraceInformation("MV: '{0}'", mventry.ObjectID);
                traceSource.TraceInformation("MA: '{0}', Rule '{1}'", maName, FlowRuleName);

                ManagementAgent ma = config.ManagementAgent.Where(m => m.Name.Equals(maName)).FirstOrDefault();
                if (ma == null) throw new NotImplementedException("MA '" + maName + "' not found");
                List<FlowRule> rules = ma.FlowRule.Where(r => r.Name.Equals(FlowRuleName) && r.Direction.Equals(direction)).ToList<FlowRule>();
                if (rules == null) throw new NotImplementedException(direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "'. Please note that rule names are case-sensitive.");
                traceSource.TraceInformation("Found {0} matching rule(s)", rules.Count);
                foreach (FlowRule r in rules) traceSource.TraceInformation("Found rule '{0}'", r.Name);
                FlowRule rule = rules.Where(ru => ru.Conditions.AreMet(csentry, mventry, traceSource)).FirstOrDefault();
                if (rule == null) throw new DeclineMappingException("No " + direction.ToString() + " rule '" + FlowRuleName + "' not found on MA '" + maName + "' where conditions were met.");

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
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Error, ex.HResult, "{0}: {1}", ex.GetType(), ex.Message);
                throw ex;
            }
            finally
            {
                traceSource.TraceInformation("Exit {0} [{1}]", "MapAttributesForImportExportDetached", direction);
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
            traceSource.TraceInformation("Enter {0}", "InvokeFlowRule");

            FlowRule r = (FlowRule)rule;
            string targetValue = null;
            foreach (Value value in r.SourceExpression.Source)
            {
                if (value.GetType().Equals(typeof(Attribute)))
                {
                    Attribute attr = (Attribute)value;
                    string concateValue = attr.GetValueOrDefault(r.Direction, csentry, mventry, traceSource);
                    concateValue = attr.Transform(concateValue, traceSource);
                    targetValue = targetValue + concateValue;
                    continue;
                }
                if (value.GetType().Equals(typeof(Constant)))
                {
                    targetValue = targetValue + ((Constant)value).Value;
                    continue;
                }
            }
            targetValue = r.Target.Transform(targetValue, traceSource);
            r.Target.SetTargetValue(r.Direction, csentry, mventry, targetValue, traceSource);

            traceSource.TraceInformation("Exit {0}", "InvokeFlowRule");
        }
        public void InvokeFlowRuleCode(ManagementAgent ma, FlowRule rule, CSEntry csentry, MVEntry mventry)
        {
            traceSource.TraceInformation("Enter {0}", "InvokeFlowRuleCode");
            FlowRuleCode r = (FlowRuleCode)rule;
            if (r.Direction.Equals(Direction.Import))
                ma.InvokeMapAttributesForImport(r.Name, csentry, mventry);
            else
                ma.InvokeMapAttributesForExport(r.Name, csentry, mventry);
            traceSource.TraceInformation("Exit {0}", "InvokeFlowRuleCode");
        }
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
[XmlInclude(typeof(ObjectClassMatch)), XmlInclude(typeof(SourceValueMatch)), XmlInclude(typeof(SourceValueNotMatch)), XmlInclude(typeof(TargetValueMatch)), XmlInclude(typeof(SubCondition))]
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
                source.TraceInformation("'And' condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == false) break;
            }
            source.TraceInformation("All 'And' conditions {0} met", met ? "were" : "were not");
            return met;
        }
        else
        {
            bool met = false;
            foreach (ConditionBase condition in Conditions)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("'Or' condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == true) break;
            }
            source.TraceInformation("One or more 'Or' conditions {0} met", met ? "were" : "were not");
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
public class SourceValueNotMatch : ConditionBase
{
    [XmlAttribute("Pattern")]
    public string Pattern { get; set; }

    public override bool IsMet(CSEntry csentry, MVEntry mventry, TraceSource source)
    {
        string value = SourceValue(csentry, mventry);
        return string.IsNullOrEmpty(value) ? false : !Regex.IsMatch(value, Pattern);
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
                source.TraceInformation("'And' condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == false) break;
            }
            source.TraceInformation("All 'And' conditions {0} met", met ? "were" : "were not");
            return met;
        }
        else
        {
            bool met = false;
            foreach (ConditionBase condition in ConditionBase)
            {
                met = condition.IsMet(csentry, mventry, source);
                source.TraceInformation("'Or' condition '{0}' returned: {1}", condition.GetType(), met);
                if (met == true) break;
            }
            source.TraceInformation("One or more 'Or' conditions {0} met", met ? "were" : "were not");
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

[XmlInclude(typeof(FlowRuleCode))]
public class FlowRule
{
    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlAttribute("Direction")]
    [XmlTextAttribute()]
    public Direction Direction { get; set; }

    [XmlElement("Conditions")]
    public Conditions Conditions { get; set; }

    [XmlElement("SourceExpression")]
    public SourceExpression SourceExpression { get; set; }
    [XmlElement("Target")]
    public Attribute Target { get; set; }

    public FlowRule()
    {
        this.Conditions = new Conditions();
    }
}
public class FlowRuleCode : FlowRule
{
}

#endregion
#region Transforms
[XmlInclude(typeof(ToUpper)), XmlInclude(typeof(ToLower)), XmlInclude(typeof(Trim)), XmlInclude(typeof(TrimEnd)), XmlInclude(typeof(TrimStart)), XmlInclude(typeof(Replace)), XmlInclude(typeof(PadLeft)), XmlInclude(typeof(PadRight)), XmlInclude(typeof(RegexReplace)), XmlInclude(typeof(Substring)), XmlInclude(typeof(RegexSelect)), XmlInclude(typeof(FormatDate)), XmlInclude(typeof(Base64ToGUID)), XmlInclude(typeof(IsBitSet)), XmlInclude(typeof(IsBitNotSet)), XmlInclude(typeof(SIDToString)), XmlInclude(typeof(SetBit))]
public abstract class Transform
{
    public abstract string Convert(string value);
}
public class Base64ToGUID : Transform
{
    public override string Convert(string value)
    {
        Guid guid = new Guid(System.Convert.FromBase64String(value));
        return guid.ToString();
    }
}

public enum SecurityIdentifierType
{
    [XmlEnum(Name = "AccountSid")]
    AccountSid,
    [XmlEnum(Name = "AccountDomainSid")]
    AccountDomainSid
}
public class SIDToString : Transform
{
    [XmlAttribute("SIDType")]
    [XmlTextAttribute()]
    public SecurityIdentifierType SIDType { get; set; }

    public override string Convert(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var sidInBytes = System.Convert.FromBase64String(value);
        var sid = new SecurityIdentifier(sidInBytes, 0);
        value = SIDType.Equals(SecurityIdentifierType.AccountSid) ? sid.Value : sid.AccountDomainSid.Value;
        return value;
    }
}
public class IsBitSet : Transform
{
    [XmlAttribute("BitPosition")]
    public int BitPosition { get; set; }

    public override string Convert(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        long longValue = long.Parse(value);
        value = ((longValue & (1 << this.BitPosition)) != 0).ToString();
        return value;
    }
}
public class IsBitNotSet : Transform
{
    [XmlAttribute("BitPosition")]
    public int BitPosition { get; set; }

    public override string Convert(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        long longValue = long.Parse(value);
        value = ((longValue & (1 << this.BitPosition)) == 0).ToString();
        return value;
    }
}
public class SetBit : Transform
{
    [XmlAttribute("BitPosition")]
    public int BitPosition { get; set; }

    [XmlAttribute("Value")]
    public bool Value { get; set; }

    private int SetBitAt(int value, int index)
    {
        if (index < 0 || index >= sizeof(long) * 8)
        {
            throw new ArgumentOutOfRangeException();
        }

        return value | (1 << index);
    }
    private int UnsetBitAt(int value, int index)
    {
        if (index < 0 || index >= sizeof(int) * 8)
        {
            throw new ArgumentOutOfRangeException();
        }

        return value & ~(1 << index);
    }
    public override string Convert(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        int val = int.Parse(value);
        val = this.Value ? SetBitAt(val, BitPosition) : UnsetBitAt(val, BitPosition);
        return val.ToString();
    }
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
        PaddingChar = string.IsNullOrEmpty(PaddingChar) ? " " : PaddingChar;
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

    public override string Convert(string value)
    {
        return value.Length <= StartIndex ? "" : value.Length - StartIndex <= Length ? value.Substring(StartIndex) : value.Substring(StartIndex, Length);
    }
}
public class RegexSelect : Transform
{
    public override string Convert(string value)
    {
        throw new NotImplementedException();
    }
}
public enum DateType
{
    [XmlEnum(Name = "DateTime")]
    DateTime,
    [XmlEnum(Name = "FileTimeUTC")]
    FileTimeUTC
}
public class FormatDate : Transform
{
    [XmlAttribute("DateType")]
    [XmlTextAttribute()]
    public DateType DateType { get; set; }

    [XmlAttribute("FromFormat")]
    public string FromFormat { get; set; }
    [XmlAttribute("ToFormat")]
    public string ToFormat { get; set; }

    public override string Convert(string text)
    {
        string returnValue = text;
        if (DateType.Equals(DateType.FileTimeUTC))
        {
            returnValue = DateTime.FromFileTimeUtc(long.Parse(text)).ToString(ToFormat);
            return returnValue;
        }
        if (DateType.Equals(DateType.DateTime))
        {
            returnValue = DateTime.ParseExact(text, FromFormat, CultureInfo.InvariantCulture).ToString(ToFormat);
            return returnValue;
        }
        return returnValue;
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

    public string GetValueOrDefault(Direction direction, CSEntry csentry, MVEntry mventry, TraceSource traceSource)
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
    public void SetTargetValue(Direction direction, CSEntry csentry, MVEntry mventry, string Value, TraceSource traceSource)
    {
        AttributeType at = direction.Equals(Direction.Import) ? mventry[this.Name].DataType : csentry[this.Name].DataType;
        traceSource.TraceInformation("Target attribute type: {0}", at);
        if (string.IsNullOrEmpty(Value))
        {
            switch (this.ActionOnNullSource)
            {
                case AttributeAction.None:
                    throw new DeclineMappingException("No default action");
                case AttributeAction.Delete:
                    if (direction.Equals(Direction.Import))
                        mventry[this.Name].Delete();
                    else
                        csentry[this.Name].Delete();
                    break;
                case AttributeAction.SetDefault:
                    if (direction.Equals(Direction.Import))
                        mventry[this.Name].Value = this.DefaultValue;
                    else
                        csentry[this.Name].Value = this.DefaultValue;
                    break;
                default:
                    throw new DeclineMappingException("No default action");
            }
        }
        else
        {
            if (direction.Equals(Direction.Import))
                mventry[this.Name].Value = Value;
            else
                csentry[this.Name].Value = Value;
        }
    }

}
public class Constant : Value
{
    [XmlAttribute("Value")]
    public string Value { get; set; }
}
public class SourceExpression
{
    [XmlElement("Source")]
    public List<Value> Source { get; set; }
}
#endregion
