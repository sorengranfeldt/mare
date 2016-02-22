using Microsoft.MetadirectoryServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
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
		public virtual bool IsMet(CSEntry csentry, MVEntry mventry)
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

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			if (Operator.Equals(ConditionOperator.And))
			{
				bool met = true;
				foreach (ConditionBase condition in Conditions)
				{
					met = condition.IsMet(csentry, mventry);
					Tracer.TraceInformation("'And' condition '{0}' returned: {1}", condition.GetType(), met);
					if (met == false) break;
				}
				Tracer.TraceInformation("All 'And' conditions {0} met", met ? "were" : "were not");
				return met;
			}
			else
			{
				bool met = false;
				foreach (ConditionBase condition in Conditions)
				{
					met = condition.IsMet(csentry, mventry);
					Tracer.TraceInformation("'Or' condition '{0}' returned: {1}", condition.GetType(), met);
					if (met == true) break;
				}
				Tracer.TraceInformation("One or more 'Or' conditions {0} met", met ? "were" : "were not");
				return met;
			}
		}
	}
	public class ObjectClassMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			return Source.Equals(EvaluateAttribute.CSEntry) ? Regex.IsMatch(csentry.ObjectType, this.Pattern) : Regex.IsMatch(mventry.ObjectType, this.Pattern);
		}
	}
	public class ObjectClassNotMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			return Source.Equals(EvaluateAttribute.CSEntry) ? !Regex.IsMatch(csentry.ObjectType, this.Pattern) : !Regex.IsMatch(mventry.ObjectType, this.Pattern);
		}
	}
	public class SourceValueMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			string value = SourceValue(csentry, mventry);
			Tracer.TraceInformation("value-is: {0}", value);
			return string.IsNullOrEmpty(value) ? false : Regex.IsMatch(value, Pattern, RegexOptions.IgnoreCase);
		}
	}
	public class SourceValueNotMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
		{
			string value = SourceValue(csentry, mventry);
			Tracer.TraceInformation("value-is: {0}", value);
			return string.IsNullOrEmpty(value) ? false : !Regex.IsMatch(value, Pattern, RegexOptions.IgnoreCase);
		}
	}
	public class TargetValueMatch : ConditionBase
	{
		[XmlAttribute("Pattern")]
		public string Pattern { get; set; }

		public override bool IsMet(CSEntry csentry, MVEntry mventry)
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

		public bool AreMet(CSEntry csentry, MVEntry mventry)
		{
			if (ConditionBase == null || ConditionBase.Count == 0)
			{
				return true; // assume true if no conditions
			}
			if (Operator.Equals(ConditionOperator.And))
			{
				bool met = true;

				foreach (ConditionBase condition in ConditionBase)
				{
					met = condition.IsMet(csentry, mventry);
					Tracer.TraceInformation("'And' condition '{0}' returned: {1}", condition.GetType().Name, met);
					if (met == false) break;
				}
				Tracer.TraceInformation("All 'And' conditions {0} met", met ? "were" : "were not");
				return met;
			}
			else
			{
				bool met = false;
				foreach (ConditionBase condition in ConditionBase)
				{
					met = condition.IsMet(csentry, mventry);
					Tracer.TraceInformation("'Or' condition '{0}' returned: {1}", condition.GetType().Name, met);
					if (met == true) break;
				}
				Tracer.TraceInformation("One or more 'Or' conditions {0} met", met ? "were" : "were not");
				return met;
			}
		}
	}
}
