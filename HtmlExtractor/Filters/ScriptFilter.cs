using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

namespace HtmlExtractor.Filters
{
    class ScriptFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
            ///删除script
            foreach (var script in document.DocumentNode.Descendants("script").ToArray())
            {
                script.Remove();
            }

        }

        public string OnExecuting(string html)
        {
            return html;
        }
    }
}
