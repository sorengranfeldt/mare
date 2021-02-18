// feb 12, 2015 | soren granfeldt
//  - added transform LookupMVValue
// october 15, 2015 | soren granfeldt
//	- added transform RegexIsMatch
// october 15, 2015 | soren granfeldt
//	- added MultiValueConcatenate and MultiValueRemoveIfNotMatch
//	- change type of data flowing through transforms from string to object to support multivalues
// december 7, 2015 | soren granfeldt
//	- added ReplaceBefore and ReplaceAfter
// january 28, 2016 | soren granfeldt
//	- fixed FromValueCollection to handle single value flowed to multivalued
// january 14, 2020 | soren granfeldt
//	- added XmlInclude ToFileTimeUTC and Base64ToGUIDFormat
// february 17, 2021 | soren granfeldt
//  - added XmlInclude MultiValueReplace

using Microsoft.MetadirectoryServices;
using System.Collections.Generic;
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
        XmlInclude(typeof(MultiValueReplace)),
        XmlInclude(typeof(ReplaceBefore)),
        XmlInclude(typeof(ReplaceAfter)),
        XmlInclude(typeof(IsBeforeOrAfter)),
        XmlInclude(typeof(RightString)),
        XmlInclude(typeof(LeftString)),
        XmlInclude(typeof(StringFormat)),
        XmlInclude(typeof(Not)),
        XmlInclude(typeof(ToFileTimeUTC)),
        XmlInclude(typeof(Base64ToGUIDFormat)),
        XmlInclude(typeof(ConvertFromTrueFalse)),
        XmlInclude(typeof(Word))
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

    public class Transforms
    {
        [XmlElement("Transform")]
        public List<Transform> Transform { get; set; }
    }

}
