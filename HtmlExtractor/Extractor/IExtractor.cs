using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlExtractor.Extractor
{
    interface IExtractor<T>
    {
        DomNode<T> Get(string html, HtmlConf conf);
    }
}
