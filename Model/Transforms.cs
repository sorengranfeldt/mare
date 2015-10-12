using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace FIM.MARE
{
	[
		XmlInclude(typeof(ToUpper)),
		XmlInclude(typeof(ToLower)),
		XmlInclude(typeof(Trim)),
		XmlInclude(typeof(TrimEnd)), 
		XmlInclude(typeof(TrimStart)), 
		XmlInclude(typeof(Replace)), 
		XmlInclude(typeof(PadLeft)), 
		XmlInclude(typeof(PadRight)), 
		XmlInclude(typeof(RegexReplace)), 
		XmlInclude(typeof(Substring)), 
		XmlInclude(typeof(RegexSelect)), 
		XmlInclude(typeof(FormatDate)), 
		XmlInclude(typeof(Base64ToGUID)), 
		XmlInclude(typeof(IsBitSet)), 
		XmlInclude(typeof(IsBitNotSet)), 
		XmlInclude(typeof(SIDToString)), 
		XmlInclude(typeof(SetBit)), 
		XmlInclude(typeof(LookupMVValue))
	]
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

	public class LookupMVValue : Transform
	{
		[XmlAttribute("LookupAttributeName")]
		public string LookupAttributeName { get; set; }

		[XmlAttribute("ExtractValueFromAttribute")]
		public string ExtractValueFromAttribute { get; set; }

		[XmlAttribute("MAName")]
		public string MAName { get; set; }

		public override string Convert(string value)
		{
			if (string.IsNullOrEmpty(value)) return value;
			MVEntry mventry = Utils.FindMVEntries(LookupAttributeName, value, 1).FirstOrDefault();
			if (mventry != null)
			{
				if (this.ExtractValueFromAttribute.Equals("[DN]"))
				{
					ConnectorCollection col = mventry.ConnectedMAs[MAName].Connectors;
					if (col != null && col.Count.Equals(1))
					{
						value = mventry.ConnectedMAs[MAName].Connectors.ByIndex[0].DN.ToString();
					}
				}
				else
				{
					value = mventry[ExtractValueFromAttribute].IsPresent ? mventry[ExtractValueFromAttribute].Value : null;
				}
			}
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

}
