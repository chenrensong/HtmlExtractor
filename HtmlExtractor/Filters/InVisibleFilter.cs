using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Linq;
namespace HtmlExtractor.Filters
{
    public class InVisibleFilter : IHtmlFilter
    {
        public void OnExecuted(HtmlDocument document)
        {
            //删除diplay:none visible:hidden
            foreach (var item in document.DocumentNode.Descendants()
                .Where(m => m.GetAttributeValue("style").Contains("display:none")
                || m.GetAttributeValue("style").Contains("visible:hidden")).ToArray())
            {
                item.Remove();
            }
        }

        public string OnExecuting(string html)
        {
            return html;
        }
    }
}
