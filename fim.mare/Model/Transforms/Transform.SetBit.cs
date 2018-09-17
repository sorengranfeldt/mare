using System;
using System.Xml.Serialization;

namespace FIM.MARE
{
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

}
