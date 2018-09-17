using System.Xml.Serialization;

namespace FIM.MARE
{

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

}
