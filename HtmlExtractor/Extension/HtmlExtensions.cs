using System;
using System.Text.RegularExpressions;

namespace HtmlExtractor
{
    internal static class HtmlExtensions
    {
        internal static string FillRelativeUrl(this string html, string baseUrl)
        {
            html = Regex.Replace(html, "(?is)(href|src)=(\"|\')([^(\"|\')]+)(\"|\')", (match) =>
            {
                string src = match.Value;
                string link = match.Groups[3].Value;
                if (link.StartsWith("http"))
                {
                    return src;
                }
                try
                {
                    Uri uri = new Uri(new Uri(baseUrl), link);
                    string fullUrl = $"{match.Groups[1].Value}=\"{uri.AbsoluteUri}\"";
                    return fullUrl;
                }
                catch (Exception)
                {
                    return src;
                }
            });
            return html;
        }

    }
}

