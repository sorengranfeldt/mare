// feb 12, 2015 | soren granfeldt
//  - added transform LookupMVValue
// october 15, 2015 | soren granfeldt
//	- added transform RegexIsMatch
// october 15, 2015 | soren granfeldt
//	- added MultiValueConcatenate and MultiValueRemoveIfNotMatch
//	- change type of data flowing through transforms from string to object to support multivalues
// december 7, 2015 | soren granfeldt
//	- added ReplaceBefore and ReplaceAfter
// januar 28, 2016 | soren granfeldt
//	-fixed FromValueCollection to handle single value flowed to multivalued

using Microsoft.MetadirectoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        XmlInclude(typeof(StringCase)),
        XmlInclude(typeof(Trim)),
        XmlInclude(typeof(TrimEnd)),
        XmlInclude(typeof(TrimStart)),
        XmlInclude(typeof(Replace)),
        XmlInclude(typeof(PadLeft)),
        XmlInclude(typeof(PadRight)),
        XmlInclude(typeof(RegexReplace)),
        XmlInclude(typeof(Substring)),
        XmlInclude(typeof(RegexSelect)),
        XmlInclude(typeof(RegexIsMatch)),
        XmlInclude(typeof(FormatDate)),
		XmlInclude(typeof(DateTimeAdd)),
		XmlInclude(typeof(Base64ToGUID)),
        XmlInclude(typeof(IsBitSet)),
        XmlInclude(typeof(IsBitNotSet)),
        XmlInclude(typeof(SIDToString)),
        XmlInclude(typeof(SetBit)),
        XmlInclude(typeof(LookupMVValue)),
        XmlInclude(typeof(MultiValueConcatenate)),
        XmlInclude(typeof(MultiValueRemoveIfNotMatch)),
        XmlInclude(typeof(MultiValueRemoveIfMatch)),
        XmlInclude(typeof(ReplaceBefore)),
        XmlInclude(typeof(ReplaceAfter)),
        XmlInclude(typeof(IsBeforeOrAfter)),
        XmlInclude(typeof(RightString)),
        XmlInclude(typeof(LeftString)),
        XmlInclude(typeof(StringFormat)),
        XmlInclude(typeof(Not))
    ]
    public abstract class Transform
    {
        public abstract object Convert(object value);

        // TODO: merge this function with same function from Source.cs
        protected List<object> FromValueCollection(object value)
        {
            List<object> values = new List<object>();
            if (value.GetType() == typeof(List<object>))
            {
                Tracer.TraceInformation("already-type-list");
                values = (List<object>)value;
            }
            else
            {
                Tracer.TraceInformation("transform-converting-to-list");
                if (value.GetType() == typeof(System.String))
                {
                    values.Add(value);
                    return values;
                }
                ValueCollection vc = (ValueCollection)value;
                foreach (Microsoft.MetadirectoryServices.Value val in vc)
                {
                    values.Add(val.ToString());
                }
            }
            return values;
        }
    }

    #region multivalues
    public class MultiValueConcatenate : Transform
    {
        [XmlAttribute("Separator")]
        public string Separator { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            string returnValue = null;
            List<object> values = FromValueCollection(value);
            foreach (object val in values)
            {
                Tracer.TraceInformation("source-value {0}", val.ToString());
                returnValue = returnValue + val.ToString() + this.Separator;
            }
            returnValue = returnValue.Substring(0, returnValue.LastIndexOf(this.Separator));
            return returnValue;
        }
    }
    public class MultiValueRemoveIfNotMatch : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            List<object> values = FromValueCollection(value);
            List<object> returnValues = new List<object>();
            foreach (object val in values)
            {
                if (Regex.IsMatch(val.ToString(), this.Pattern, RegexOptions.IgnoreCase))
                {
                    Tracer.TraceInformation("removing-value {0}", val);
                }
                else
                {
                    Tracer.TraceInformation("keeping-value {0}", val.ToString());
                    returnValues.Add(val);
                }
            }
            return returnValues;
        }
    }
    public class MultiValueRemoveIfMatch : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;

            List<object> values = FromValueCollection(value);
            List<object> returnValues = new List<object>();
            foreach (object val in values)
            {
                if (!Regex.IsMatch(val.ToString(), this.Pattern, RegexOptions.IgnoreCase))
                {
                    Tracer.TraceInformation("removing-value {0}", val);
                }
                else
                {
                    Tracer.TraceInformation("keeping-value {0}", val.ToString());
                    returnValues.Add(val);
                }
            }
            return returnValues;
        }
    }

    #endregion

    public class Base64ToGUID : Transform
    {
        public override object Convert(object value)
        {
            if (value == null) return value;
            Guid guid = new Guid(System.Convert.FromBase64String(value as string));
            return guid;
        }
    }

    public enum SecurityIdentifierType
    {
        [XmlEnum(Name = "AccountSid")]
        AccountSid,
        [XmlEnum(Name = "AccountDomainSid")]
        AccountDomainSid
    }
    public enum DateTimeRelativity
    {
        [XmlEnum(Name = "After")]
        After,
        [XmlEnum(Name = "Before")]
        Before
    }
    public class SIDToString : Transform
    {
        [XmlAttribute("SIDType")]
        [XmlTextAttribute()]
        public SecurityIdentifierType SIDType { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            var sidInBytes = System.Convert.FromBase64String(value as string);
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

        public override object Convert(object value)
        {
            if (value == null) return value;
            MVEntry mventry = Utils.FindMVEntries(LookupAttributeName, value as string, 1).FirstOrDefault();
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

        public override object Convert(object value)
        {
            if (value == null) return value;
            long longValue = long.Parse(value as string);
            value = ((longValue & (1 << this.BitPosition)) != 0).ToString();
            return value;
        }
    }
    public class IsBitNotSet : Transform
    {
        [XmlAttribute("BitPosition")]
        public int BitPosition { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            long longValue = long.Parse(value as string);
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
        public override object Convert(object value)
        {
            if (value == null) return value;
            int val = int.Parse(value as string);
            val = this.Value ? SetBitAt(val, BitPosition) : UnsetBitAt(val, BitPosition);
            return val.ToString();
        }
    }
    public class RightString : Transform
    {
        [XmlAttribute("CharactersToGet")]
        public int CharactersToGet { get; set; }
        internal string Right(string str, int length)
        {
            str = (str ?? string.Empty);
            return (str.Length >= length)
                ? str.Substring(str.Length - length, length)
                : str;
        }
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : this.Right(value as string, CharactersToGet);
        }
    }
    public class LeftString : Transform
    {
        [XmlAttribute("CharactersToGet")]
        public int CharactersToGet { get; set; }
        internal string Left(string str, int length)
        {
            str = (str ?? string.Empty);
            return str.Substring(0, Math.Min(length, str.Length));
        }
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : this.Left(value as string, CharactersToGet);
        }
    }
    public class ToUpper : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().ToUpper();
        }
    }
    public class ToLower : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().ToLower();
        }
    }
    public class Trim : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().Trim();
        }
    }
    public class TrimEnd : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().TrimEnd();
        }
    }
    public class TrimStart : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().TrimStart();
        }
    }
    public class Replace : Transform
    {
        [XmlAttribute("OldValue")]
        public string OldValue { get; set; }
        [XmlAttribute("NewValue")]
        public string NewValue { get; set; }
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().Replace(OldValue, NewValue);
        }
    }
    public class ReplaceBefore : Transform
    {
        [XmlAttribute("IndexOf")]
        public string IndexOf { get; set; }
        [XmlAttribute("ReplaceValue")]
        public string ReplaceValue { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;
            string s = value.ToString();
            Tracer.TraceInformation("s {0}", s);
            int idx = s.IndexOf(IndexOf);
            Tracer.TraceInformation("idx {0}", idx);
            s = string.Concat(ReplaceValue, s.Substring(idx));
            return s;
        }
    }
    public class ReplaceAfter : Transform
    {
        [XmlAttribute("IndexOf")]
        public string IndexOf { get; set; }
        [XmlAttribute("ReplaceValue")]
        public string ReplaceValue { get; set; }
        public override object Convert(object value)
        {
            if (value == null) return value;
            string s = value.ToString();
            Tracer.TraceInformation("s {0}", s);
            int idx = s.IndexOf(IndexOf);
            Tracer.TraceInformation("idx {0}", idx);
            s = string.Concat(s.Remove(idx + IndexOf.Length), ReplaceValue);
            return s;
        }
    }
    public class PadLeft : Transform
    {
        [XmlAttribute("TotalWidth")]
        public int TotalWidth { get; set; }
        [XmlAttribute("PaddingChar")]
        public string PaddingChar { get; set; }

        public override object Convert(object value)
        {
            PaddingChar = string.IsNullOrEmpty(PaddingChar) ? " " : PaddingChar;
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().PadLeft(TotalWidth, PaddingChar[0]);
        }
    }
    public class PadRight : Transform
    {
        [XmlAttribute("TotalWidth")]
        public int TotalWidth { get; set; }
        [XmlAttribute("PaddingChar")]
        public string PaddingChar { get; set; }

        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().PadRight(TotalWidth, PaddingChar[0]);
        }
    }
    public class RegexReplace : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }
        [XmlAttribute("Replacement")]
        public string Replacement { get; set; }

        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : Regex.Replace(value as string, this.Pattern, this.Replacement);
        }
    }
    public class Substring : Transform
    {
        [XmlAttribute("StartIndex")]
        public int StartIndex { get; set; }
        [XmlAttribute("Length")]
        public int Length { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string val = value as string;
            return val.Length <= StartIndex ? "" : val.Length - StartIndex <= Length ? val.Substring(StartIndex) : val.Substring(StartIndex, Length);
        }
    }
    public class RegexIsMatch : Transform
    {
        [XmlAttribute("Pattern")]
        public string Pattern { get; set; }

        [XmlAttribute("TrueValue")]
        public string TrueValue { get; set; }

        [XmlAttribute("FalseValue")]
        public string FalseValue { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return FalseValue;
            return Regex.IsMatch(value as string, Pattern, RegexOptions.IgnoreCase) ? TrueValue : FalseValue;
        }
    }
    public class RegexSelect : Transform
    {
        public override object Convert(object value)
        {
            throw new NotImplementedException();
        }
    }
    public enum DateType
    {
        [XmlEnum(Name = "BestGuess")]
        BestGuess,
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

        public override object Convert(object value)
        {
            if (value == null) return value;
            string returnValue = value.ToString();
			Tracer.TraceInformation("formatdate-from {0} / {1}", DateType, returnValue);
			if (DateType.Equals(DateType.FileTimeUTC))
            {
                returnValue = DateTime.FromFileTimeUtc(long.Parse(value.ToString())).ToString(ToFormat);
                return returnValue;
            }
            if (DateType.Equals(DateType.BestGuess))
            {
				returnValue = DateTime.Parse(value.ToString(), CultureInfo.InvariantCulture).ToString(ToFormat);
                return returnValue;
            }
            if (DateType.Equals(DateType.DateTime))
            {
                returnValue = DateTime.ParseExact(value.ToString(), FromFormat, CultureInfo.InvariantCulture).ToString(ToFormat);
                return returnValue;
            }
            return returnValue;
        }
    }

    public class IsBeforeOrAfter : Transform
    {
        [XmlAttribute("AddDays")]
        public int AddDays { get; set; }

        [XmlAttribute("AddHours")]
        public int AddHours { get; set; }
        [XmlAttribute("AddMonths")]
        public int AddMonths { get; set; }

        [XmlAttribute("Relativity")]
        [XmlTextAttribute()]
        public DateTimeRelativity Relativity { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string input = value as string;
            DateTime dateValue;
            DateTime now = DateTime.Now;
            if (DateTime.TryParse(input, out dateValue))
            {
                bool returnValue = false;
                dateValue = dateValue.AddHours(this.AddHours);
                Tracer.TraceInformation("date-after-addhours {0}", dateValue);

                dateValue = dateValue.AddDays(this.AddDays);
                Tracer.TraceInformation("date-after-adddays {0}", dateValue);

                dateValue = dateValue.AddMonths(this.AddMonths);
                Tracer.TraceInformation("date-after-addmonths {0}", dateValue);

                if (this.Relativity == DateTimeRelativity.After)
                {
                    returnValue = now > dateValue;
                    Tracer.TraceInformation("compare-dates now: {0}, value: {1}, is-after: {2}", now, dateValue, returnValue);
                    return returnValue;
                }
                else if (this.Relativity == DateTimeRelativity.Before)
                {
                    returnValue = now < dateValue;
                    Tracer.TraceInformation("compare-dates now: {0}, value: {1}, is-before: {2}", now, dateValue, returnValue);
                    return returnValue;
                }
                Tracer.TraceInformation("is-{0}-{1}-{2}: {3}", now, this.Relativity, dateValue, returnValue);
                return returnValue;
            }
            else
            {
                Tracer.TraceWarning("could-not-parse-to-date {0}", 1, input);
            }
            return input;
        }
    }

    public enum CaseType
    {
        [XmlEnum(Name = "Lowercase")]
        Lowercase,
        [XmlEnum(Name = "Uppercase")]
        Uppercase,
        [XmlEnum(Name = "TitleCase")]
        TitleCase
    }
    public class StringCase : Transform
    {
        [XmlAttribute("Culture")]
        public string Culture { get; set; }
        internal CultureInfo cultureInfo
        {
            get
            {
                if (string.IsNullOrEmpty(this.Culture))
                {
                    return CultureInfo.CurrentCulture;
                }
                return new CultureInfo(this.Culture, false);
            }
        }
        [XmlAttribute("CaseType")]
        [XmlTextAttribute()]
        public CaseType CaseType { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            TextInfo textInfo = this.cultureInfo.TextInfo;
            switch (CaseType)
            {
                case CaseType.Lowercase:
                    return textInfo.ToLower(value as string);
                case CaseType.Uppercase:
                    return textInfo.ToUpper(value as string);
                case CaseType.TitleCase:
                    return textInfo.ToTitleCase(value as string);
                default:
                    return value;
            }
        }
    }

    public class StringFormat : Transform
    {
        [XmlAttribute("ConvertToNumber")]
        public bool ConvertToNumber { get; set; }

        [XmlAttribute("Format")]
        public string Format { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            try
            {
                if (this.ConvertToNumber)
                {
                    return System.Convert.ToInt64(value).ToString(this.Format);
                }
                return string.Format(this.Format, value);
            }
            catch (System.FormatException fe)
            {
                Tracer.TraceError("unable-to-format-string", fe);
                return value;
            }

        }
    }

    public class Transforms
    {
        [XmlElement("Transform")]
        public List<Transform> Transform { get; set; }
    }


}
