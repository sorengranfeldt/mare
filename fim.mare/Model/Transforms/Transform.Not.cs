// april 23, 2018 | soren granfeldt
//  - added Not transform

namespace FIM.MARE
{

    public class Not : Transform
    {
        public override object Convert(object value)
        {
            if (value == null) return value;
            bool boolValue = bool.Parse(value as string);
            value = (!boolValue).ToString();
            return value;
        }
    }
 
}
