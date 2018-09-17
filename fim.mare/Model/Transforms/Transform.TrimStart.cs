namespace FIM.MARE
{
    public class TrimStart : Transform
    {
        public override object Convert(object value)
        {
            return string.IsNullOrEmpty(value as string) ? value : value.ToString().TrimStart();
        }
    }
}
