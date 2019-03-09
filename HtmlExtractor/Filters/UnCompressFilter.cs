using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace HtmlExtractor.Filters
{
    class UnCompressFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
        }

        public string OnExecuting(string html)
        {
            // 如果换行符的数量小于10，则认为html为压缩后的html
            // 由于处理算法是按照行进行处理，需要为html标签添加换行符，便于处理
            if (html.Count(c => c == '\n') < 10)
            {
                html = html.Replace(">", ">\n");
            }
            return html;
        }
    }
}
