using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlExtractor.Filters
{
    public interface IHtmlFilter
    {
        string OnExecuting(string html);

        void OnExecuted(HtmlDocument document);
    }
}
