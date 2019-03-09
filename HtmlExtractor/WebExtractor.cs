using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HtmlExtractor.Extractor
{
    public class WebExtractor
    {
        protected readonly string _html;

        protected readonly HtmlConf _conf;

        private HtmlParser _htmlParser = null;


        public WebExtractor(string html, HtmlConf conf)
        {
            _html = html;
            _conf = conf;
        }

        public HtmlMeta Get()
        {
            _htmlParser = HtmlParser.Parse(_html);
            var title = GetTitle();
            var publishDate = GetPublishDate();
            var content = GetContent();
            return new HtmlMeta()
            {
                Title = title,
                PublishTime = publishDate,
                Content = content
            };
        }

        protected DomNode<string> GetTitle()
        {
            string titleFilter = @"<title>[\s\S]*?</title>";
            string h1Filter = @"<h1.*?>.*?</h1>";
            string clearFilter = @"<.*?>";

            string title = "";
            Match match = Regex.Match(_html, titleFilter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                title = Regex.Replace(match.Groups[0].Value, clearFilter, "");
            }

            // 正文的标题一般在h1中，比title中的标题更干净
            match = Regex.Match(_html, h1Filter, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                string h1 = Regex.Replace(match.Groups[0].Value, clearFilter, "");
                if (!String.IsNullOrEmpty(h1) && title.StartsWith(h1))
                {
                    title = h1;
                }
            }
            return new DomNode<string>()
            {
                Value = title
            };
        }

        /// <summary>
        /// 获取文章发布日期
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected DomNode<DateTime> GetPublishDate()
        {
            // 过滤html标签，防止标签对日期提取产生影响
            string text = Regex.Replace(_html, "(?is)<.*?>", "");
            Match match = Regex.Match(
                text,
                @"((\d{4}|\d{2})(\-|\/)\d{1,2}\3\d{1,2})(\s?\d{2}:\d{2})?|(\d{4}年\d{1,2}月\d{1,2}日)(\s?\d{2}:\d{2})?",
                RegexOptions.IgnoreCase);

            DateTime date = new DateTime(1900, 1, 1);
            if (match.Success)
            {
                try
                {
                    string dateStr = "";
                    for (int i = 0; i < match.Groups.Count; i++)
                    {
                        dateStr = match.Groups[i].Value;
                        if (!String.IsNullOrEmpty(dateStr))
                        {
                            break;
                        }
                    }
                    // 对中文日期的处理
                    if (dateStr.Contains("年"))
                    {
                        StringBuilder sb = new StringBuilder();
                        foreach (var ch in dateStr)
                        {
                            if (ch == '年' || ch == '月')
                            {
                                sb.Append("/");
                                continue;
                            }
                            if (ch == '日')
                            {
                                sb.Append(' ');
                                continue;
                            }
                            sb.Append(ch);
                        }
                        dateStr = sb.ToString();
                    }
                    date = Convert.ToDateTime(dateStr);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                if (date.Year < 1900)
                {
                    date = new DateTime(1900, 1, 1);
                }
            }
            return new DomNode<DateTime>()
            {
                Html = text,
                Value = date,
                XPath = null
            };
        }


        /// <summary>
        /// 从body标签文本中分析正文内容(只过滤了script和style标签的body文本内容)
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        protected DomNode<string> GetContent()
        {
            var bodyText = _htmlParser.Body;
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
            for (int i = 0; i < lines.Length - _conf.Depth; i++)
            {
                int len = 0;
                for (int j = 0; j < _conf.Depth; j++)
                {
                    len += lines[i + j].Length;
                }

                if (startPos == -1)     // 还没有找到文章起始位置，需要判断起始位置
                {
                    if (preTextLen > _conf.LimitCount && len > 0)    // 如果上次查找的文本数量超过了限定字数，且当前行数字符数不为0，则认为是开始位置
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
                            if (emptyCount == _conf.HeadEmptyLines)
                            {
                                startPos = j + _conf.HeadEmptyLines;
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
                    if (len <= _conf.EndLimitCharCount && preTextLen < _conf.EndLimitCharCount)    // 当前长度为0，且上一个长度也为0，则认为已经结束
                    {
                        if (!_conf.AppendMode)
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
            if (!string.IsNullOrEmpty(_conf.BaseUrl))
            {
                newHtml = newHtml.FillRelativeUrl(_conf.BaseUrl);
            }


            var domNode = new DomNode<string>()
            {
                Html = newHtml,//输出带标签文本
                Value = content
            };

            HtmlAgilityPack.HtmlDocument doc_src = new HtmlAgilityPack.HtmlDocument();
            doc_src.LoadHtml(_html);

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
            //if (!string.IsNullOrEmpty(xpath))
            //{
            //    var test = doc_src.DocumentNode.SelectSingleNode(xpath);
            //}
            return domNode;
        }



    }
}
