using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;

namespace HtmlExtractor.Filters
{
    class SwanBodyFilter : IBodyFilter
    {
        public HtmlNode OnExecuted(HtmlNode htmlNode)
        {
            //百度小程序Web化
            var sfrNode = htmlNode.FindNodeById("sfr-app");
            if (sfrNode != null)
            {
                var rtNode = sfrNode.FindNodeByClass("web-swan-body");//rt-body
                if (rtNode != null)
                {
                    var node = rtNode;
                    return node;
                }
            }
            return htmlNode;
        }
    }
}
