using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
namespace HtmlExtractor.Extractor
{
    class ContentExtractor : IExtractor<string>
    {

        /// <summary>
        /// 从body标签文本中分析正文内容(只过滤了script和style标签的body文本内容)
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public DomNode<string> Get(string html, HtmlConf conf)
        {

            var htmlBody = HtmlBody.Parse(html);
            var bodyText = htmlBody.Value;

            string[] orgLines = null;   // 保存原始内容，按行存储
            string[] lines = null;      // 保存干净的文本内容，不包含标签

            orgLines = bodyText.Split('\n');
            lines = new string[orgLines.Length];
            // 去除每行的空白字符,剔除标签
            for (int i = 0; i < orgLines.Length; i++)
            {
                string lineInfo = orgLines[i];
                // 处理回车，使用[crlf]做为回车标记符，最后统一处理
                lineInfo = Regex.Replace(lineInfo, "(?is)</p>|<br.*?/>", "[crlf]");
                lines[i] = Regex.Replace(lineInfo, "(?is)<.*?>", "").Trim();
            }

            StringBuilder sb = new StringBuilder();
            StringBuilder orgSb = new StringBuilder();

            int preTextLen = 0;         // 记录上一次统计的字符数量
            int startPos = -1;          // 记录文章正文的起始位置
            for (int i = 0; i < lines.Length - conf.Depth; i++)
            {
                int len = 0;
                for (int j = 0; j < conf.Depth; j++)
                {
                    len += lines[i + j].Length;
                }

                if (startPos == -1)     // 还没有找到文章起始位置，需要判断起始位置
                {
                    if (preTextLen > conf.LimitCount && len > 0)    // 如果上次查找的文本数量超过了限定字数，且当前行数字符数不为0，则认为是开始位置
                    {
                        // 查找文章起始位置, 如果向上查找，发现2行连续的空行则认为是头部
                        int emptyCount = 0;
                        for (int j = i - 1; j > 0; j--)
                        {
                            if (String.IsNullOrEmpty(lines[j]))
                            {
                                emptyCount++;
                            }
                            else
                            {
                                emptyCount = 0;
                            }
                            if (emptyCount == conf.HeadEmptyLines)
                            {
                                startPos = j + conf.HeadEmptyLines;
                                break;
                            }
                        }
                        // 如果没有定位到文章头，则以当前查找位置作为文章头
                        if (startPos == -1)
                        {
                            startPos = i;
                        }
                        // 填充发现的文章起始部分
                        for (int j = startPos; j <= i; j++)
                        {
                            sb.Append(lines[j]);
                            orgSb.Append(orgLines[j]);
                        }
                    }
                }
                else
                {
                    //if (len == 0 && preTextLen == 0)    // 当前长度为0，且上一个长度也为0，则认为已经结束
                    if (len <= conf.EndLimitCharCount && preTextLen < conf.EndLimitCharCount)    // 当前长度为0，且上一个长度也为0，则认为已经结束
                    {
                        if (!conf.AppendMode)
                        {
                            break;
                        }
                        startPos = -1;
                    }
                    sb.Append(lines[i]);
                    orgSb.Append(orgLines[i]);
                }
                preTextLen = len;
            }

            string content = sb.ToString();
            // 处理回车符，更好的将文本格式化输出
            content = content.Replace("[crlf]", Environment.NewLine);
            content = System.Web.HttpUtility.HtmlDecode(content);

            var newHtml = orgSb.ToString();
            if (!string.IsNullOrEmpty(conf.BaseUrl))
            {
                newHtml = newHtml.FillRelativeUrl(conf.BaseUrl);
            }


            var domNode = new DomNode<string>()
            {
                Html = newHtml,//输出带标签文本
                Value = content
            };

            HtmlAgilityPack.HtmlDocument doc_src = new HtmlAgilityPack.HtmlDocument();
            doc_src.LoadHtml(html);


            //原始文档
            HtmlAgilityPack.HtmlDocument doc_body = new HtmlAgilityPack.HtmlDocument();
            doc_body.LoadHtml(bodyText);

            //当前抽取的内容
            HtmlAgilityPack.HtmlDocument doc_node = new HtmlAgilityPack.HtmlDocument();
            doc_node.LoadHtml(newHtml);
            var firstNode = doc_node.DocumentNode.FirstChild;

            try
            {
                //todo 
                ///第一个可能某些原因没有提取全，可以用第二个
                while (firstNode.NextSibling != null)
                {
                    firstNode = firstNode.NextSibling;

                    if (firstNode.InnerHtml.Trim() != "")
                    {
                        break;
                    }
                }
            }
            catch
            {
                return domNode;
            }

            string xpath = null;
            HtmlNode resultNode = null;
            //查找第一个有效的节点在原始里面能否找到
            doc_body.DocumentNode.FindHtmlNode(firstNode, ref resultNode);

            var originParentNode = resultNode?.ParentNode;

            var firstParentNode = firstNode.ParentNode;
            var maxInnerHtmlNode = doc_node.DocumentNode.ChildNodes.OrderByDescending(x => x.InnerHtml.Length).FirstOrDefault();

            //如果最大的是直接子集，就用原始的
            resultNode = null;

            originParentNode.FindHtmlNode(maxInnerHtmlNode, ref resultNode);

            HtmlNode aaaNode = null;

            if (resultNode != null && originParentNode.ChildNodes.FirstOrDefault(x => x == resultNode) != null)
            {
                domNode.Html = originParentNode.OuterHtml;
                content = originParentNode.InnerText.Replace("\r", "\r\n");
                doc_body.DocumentNode.FindHtmlNode(originParentNode, ref aaaNode);

            }
            else
            {
                if (!firstParentNode.ChildNodes.Contains(maxInnerHtmlNode))
                {
                    domNode.Html = maxInnerHtmlNode.OuterHtml;
                    content = maxInnerHtmlNode.InnerText.Replace("\r", "\r\n");
                    doc_body.DocumentNode.FindHtmlNode(maxInnerHtmlNode, ref aaaNode);


                }
                else
                {
                    domNode.Html = firstParentNode.OuterHtml;
                    content = firstParentNode.InnerText.Replace("\r", "\r\n");
                    doc_body.DocumentNode.FindHtmlNode(firstParentNode, ref aaaNode);

                }

            }
            if (aaaNode != null)
            {
                xpath = doc_body.GetXPath(aaaNode);
                domNode.XPath = xpath;
            }
            if (!string.IsNullOrEmpty(xpath))
            {
                var test = doc_src.DocumentNode.SelectSingleNode(xpath);
            }
            return domNode;
        }
    }
}
