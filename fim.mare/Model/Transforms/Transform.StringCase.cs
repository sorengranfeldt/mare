using System.Globalization;
using System.Xml.Serialization;

namespace FIM.MARE
{
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
}
