using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace HtmlExtractor.Filters
{
    class EmptySwanFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
            foreach (var swan_template in document.DocumentNode.Descendants("swan-template").ToArray())
            {
                var innerText = swan_template.InnerText.Replace("\n", "").Replace("\r", "").Trim();
                //除去空行
                if (string.IsNullOrEmpty(innerText))
                {
                    swan_template.Remove();
                }
            }
        }

        public string OnExecuting(string html)
        {
            return html;
        }
    }
}
