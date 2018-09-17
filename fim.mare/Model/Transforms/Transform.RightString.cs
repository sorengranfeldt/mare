using System.Xml.Serialization;

namespace FIM.MARE
{

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

}
