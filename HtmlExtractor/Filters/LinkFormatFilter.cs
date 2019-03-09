using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace HtmlExtractor.Filters
{
    class LinkFormatFilter : IHtmlFilter
    {

        public void OnExecuted(HtmlDocument document)
        {
        }

        public string OnExecuting(string html)
        {
            // 标签规整化处理，将标签属性格式化处理到同一行
            // 处理形如以下的标签：
            //  <a 
            //   href='http://www.baidu.com'
            //   class='test'
            // 处理后为
            //  <a href='http://www.baidu.com' class='test'>
            html = Regex.Replace(html, @"(<[^<>]+)\s*\n\s*", FormatTag);
            // 针对链接密集型的网站的处理，主要是门户类的网站，降低链接干扰
            html = Regex.Replace(html, @"(?is)</a>", "</a>\n");
            return html;
        }

        /// <summary>
        /// 格式化标签，剔除匹配标签中的回车符
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private static string FormatTag(Match match)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ch in match.Value)
            {
                if (ch == '\r' || ch == '\n')
                {
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
