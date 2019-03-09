using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

namespace HtmlExtractor.Filters
{
    class ComonBodyFilter : IBodyFilter
    {
        public HtmlNode OnExecuted(HtmlNode htmlNode)
        {
            var node = htmlNode.FindNodeByType("body");
            return node != null ? node : htmlNode;
        }
    }
}
