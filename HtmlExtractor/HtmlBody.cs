using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace HtmlExtractor
{
    internal class HtmlBody
    {
        // 正则表达式过滤：正则表达式，要替换成的文本
        private static readonly string[][] Filters =
        {
            //new[] { @"(?is)<script.*?>.*?</script>", "" },
            //new[] { @"(?is)<style.*?>.*?</style>", "" },
            //// 过滤Html代码中的注释
            //new[] { @"(?is)<!--.*?-->", "" },    // 
            // 针对链接密集型的网站的处理，主要是门户类的网站，降低链接干扰
            new[] { @"(?is)</a>", "</a>\n"}
        };

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


        public string Value { get; set; }


        public static HtmlBody Parse(string html)
        {
            // 如果换行符的数量小于10，则认为html为压缩后的html
            // 由于处理算法是按照行进行处理，需要为html标签添加换行符，便于处理
            if (html.Count(c => c == '\n') < 10)
            {
                html = html.Replace(">", ">\n");
            }

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(html);


            ////删除diplay:none
            //foreach (var display in document.DocumentNode.Descendants()
            //    .Where(m => m.GetAttributeValue("style") == "display:none" || m.GetAttributeValue("style") == "display: none").ToArray())
            //{
            //    display.Remove();
            //}

            ///删除script
            foreach (var script in document.DocumentNode.Descendants("script").ToArray())
            {
                script.Remove();
            }
            ///删除style
            foreach (var style in document.DocumentNode.Descendants("style").ToArray())
            {
                style.Remove();
            }
            ///comment() 在XPath中表示“所有注释节点”
            foreach (var comment in document.DocumentNode.SelectNodes("//comment()").ToArray())
            {
                comment.Remove();//新增的代码
            }
            foreach (var swan_template in document.DocumentNode.Descendants("swan-template").ToArray())
            {
                var innerText = swan_template.InnerText.Replace("\n", "").Replace("\r", "").Trim();

                if (string.IsNullOrEmpty(innerText))
                {
                    //swan_template.Remove();
                    //var otherList = swan_template.Descendants().Where(m => m.Name != "swan-view" && m.Name != "swan-template" && m.Name != "#text");
                    //var count = otherList.Count();
                    //if (count == 0)
                    //{
                    //    swan_template.Remove();
                    //}
                    //else
                    //{
                    //    foreach (var item in swan_template.Descendants().ToArray())
                    //    {
                    //        if (otherList.Contains(item))
                    //        {
                    //            continue;
                    //        }
                    //        item.Remove();
                    //    }
                    //}
                }
            }


            HtmlNode htmlNode = null;

            htmlNode = document.FindNodeByType("body");

            //百度小程序Web化
            var sfrNode = htmlNode.FindNodeById("sfr-app");

            if (htmlNode == null || sfrNode != null)
            {
                if (sfrNode != null)
                {
                    var rtNode = sfrNode.FindNodeByClass("rt-body");//web-swan-body
                    if (rtNode != null)
                    {
                        htmlNode = rtNode;
                    }
                }
            }


            var body = htmlNode.OuterHtml;

            // 过滤样式，脚本等不相干标签
            foreach (var filter in Filters)
            {
                body = Regex.Replace(body, filter[0], filter[1]);
            }
            // 标签规整化处理，将标签属性格式化处理到同一行
            // 处理形如以下的标签：
            //  <a 
            //   href='http://www.baidu.com'
            //   class='test'
            // 处理后为
            //  <a href='http://www.baidu.com' class='test'>
            body = Regex.Replace(body, @"(<[^<>]+)\s*\n\s*", FormatTag);

            return new HtmlBody()
            {
                Value = body
            };

        }
    }
}
