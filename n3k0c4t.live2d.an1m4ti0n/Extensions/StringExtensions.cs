namespace n3k0c4t.live2d.an1m4ti0n.Extensions;

public static class StringExtensions
{
    public static string ToUnderscoreCase(this string str)
    {
        return string.Concat(
            str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToUpper();
    }

    public static string ToCamelCase(this string str)
    {
        return string.Join("", 
            str.Split('_')
                .Select(i => i.Length > 0 ? char.ToUpper(i[0]) + i.Substring(1).ToLower() : "" )
        );
    }
}
