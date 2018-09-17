using System;
using System.Xml.Serialization;

namespace FIM.MARE
{
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

}
