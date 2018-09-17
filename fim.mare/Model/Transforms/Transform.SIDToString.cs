using System.Security.Principal;
using System.Xml.Serialization;

namespace FIM.MARE
{
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

        public override object Convert(object value)
        {
            if (value == null) return value;
            var sidInBytes = System.Convert.FromBase64String(value as string);
            var sid = new SecurityIdentifier(sidInBytes, 0);
            value = SIDType.Equals(SecurityIdentifierType.AccountSid) ? sid.Value : sid.AccountDomainSid.Value;
            return value;
        }
    }
}
