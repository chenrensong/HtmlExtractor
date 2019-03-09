using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlExtractor.Filters
{
    interface IBodyFilter
    {
        HtmlNode OnExecuted(HtmlNode htmlNode);
    }
}
