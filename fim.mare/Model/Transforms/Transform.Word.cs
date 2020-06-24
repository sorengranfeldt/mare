using System.Xml.Serialization;

namespace FIM.MARE
{
    public class Word : Transform
    {
        [XmlAttribute("Number")]
        public int Number { get; set; }
        [XmlAttribute("Delimiters")]
        public string Delimiters { get; set; }

        public override object Convert(object value)
        {
            if (value == null) return value;
            string val = value as string;
            string[] words = val.Split(Delimiters.ToCharArray());
            Tracer.TraceInformation($"word-count: {words.Length}, number: {Number}");
            if (words == null || Number >= words.Length) return null;
            return words[Number];
        }
    }
}
