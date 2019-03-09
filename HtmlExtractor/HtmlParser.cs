using HtmlAgilityPack;
using HtmlExtractor.Filters;
using System.Collections.Generic;

namespace HtmlExtractor
{
    internal class HtmlParser
    {

        private readonly static IList<IHtmlFilter> _htmlFilters = new List<IHtmlFilter>()
        {
            new UnCompressFilter(),//压缩的html进行解压缩
            new LinkFormatFilter(),//多行链接转为一行
            new ScriptFilter(),//去除script标签
            new StyleFilter(),//去除style标签
            new CommentFilter(),//去除注释标签
            new EmptySwanFilter()//除去空的swan-template标签
        };

        private readonly static IList<IBodyFilter> _bodyFilters = new List<IBodyFilter>()
        {
            new ComonBodyFilter(),
            new SwanBodyFilter(),
        };

        /// <summary>
        /// 原始的Html
        /// </summary>
        public string OrgHtml { get; private set; }

        public string Body { get; set; }

        public static HtmlParser Parse(string html)
        {
            var copyHtml = html;
            foreach (var filter in _htmlFilters)
            {
                copyHtml = filter.OnExecuting(copyHtml);
            }
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(copyHtml);
            foreach (var filter in _htmlFilters)
            {
                filter.OnExecuted(document);
            }
            var htmlNode = document.DocumentNode;

            foreach (var filter in _bodyFilters)
            {
                htmlNode = filter.OnExecuted(htmlNode);
            }
            var body = htmlNode.OuterHtml;

            return new HtmlParser()
            {
                OrgHtml = html,
                Body = body
            };

        }
    }
}
