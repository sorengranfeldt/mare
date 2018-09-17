namespace FIM.MARE
{
    public class Trim : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().Trim();
        }
    }
}
