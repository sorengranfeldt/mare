using System;

namespace FIM.MARE
{
    public class Base64ToGUID : Transform
    {
        public override object Convert(object value)
        {
            if (value == null) return value;
            Guid guid = new Guid(System.Convert.FromBase64String(value as string));
            return guid;
        }
    }

}
