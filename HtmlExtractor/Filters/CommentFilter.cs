using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace HtmlExtractor.Filters
{
    class CommentFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
            ///comment() 在XPath中表示“所有注释节点”
            foreach (var comment in document.DocumentNode.SelectNodes("//comment()").ToArray())
            {
                comment.Remove();//新增的代码
            }
        }

        public string OnExecuting(string html)
        {
            return html;
        }
    }
}
