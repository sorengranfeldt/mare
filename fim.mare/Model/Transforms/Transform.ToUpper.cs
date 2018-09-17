namespace FIM.MARE
{
    public class ToUpper : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().ToUpper();
        }
    }
}
