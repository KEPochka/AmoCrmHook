namespace WebApp.Extentions
{
    public static class StringHelper
    {
        public static string Underscore(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }
}
