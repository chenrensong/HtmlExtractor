using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace HtmlExtractor.Filters
{
    class StyleFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
            ///删除style
            foreach (var style in document.DocumentNode.Descendants("style").ToArray())
            {
                style.Remove();
            }
        }

        public string OnExecuting(string html)
        {
            return html;
        }
    }
}
