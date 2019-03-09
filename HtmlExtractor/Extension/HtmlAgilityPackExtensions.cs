using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace HtmlExtractor
{
    public static class HtmlAgilityPackExtensions
    {

        public static void FindHtmlNode(this HtmlNode htmlNode, HtmlNode targetNode, ref HtmlNode resultNode)
        {
            if (htmlNode == null || targetNode == null)
            {
                resultNode = null;
                return;
            }
            foreach (var item in htmlNode.ChildNodes)
            {
                if (resultNode != null)
                {
                    break;
                }
                var itemHtml = item.InnerHtml.Replace("\n", "").Replace("\r", "");
                var targetHtml = targetNode.InnerHtml.Replace("\n", "").Replace("\r", "");
                //|| item.InnerText.Trim().Contains(targetNode.InnerText.Trim()
                if (targetHtml == itemHtml)
                {
                    resultNode = item;
                    break;
                }
                FindHtmlNode(item, targetNode, ref resultNode);
            }
        }

        public static string GetXPath(this HtmlDocument doc, HtmlNode node)
        {
            HtmlNode htmlNode = null;
            return GetXPathInternal(doc, node, ref htmlNode);
        }

        private static string GetXPathInternal(this HtmlDocument doc, HtmlNode node, ref HtmlNode newNode)
        {
            string xpath = null;
            if (node.ParentNode != null)
            {
                newNode = node.ParentNode;
                var index = node.XPath.LastIndexOf("/");
                xpath = node.XPath.Substring(index, node.XPath.Length - index) + xpath;
                var classValue = newNode.GetAttributeValue("class");
                if (!string.IsNullOrEmpty(classValue))
                {
                    var newXPath = "//*[@class=\"" + classValue + "\"]";
                    var xpathNodes = doc.DocumentNode.SelectNodes(newXPath);
                    if (xpathNodes != null && xpathNodes.Count == 1) ///按照class是否是唯一
                    {
                        xpath = newXPath + xpath;
                        return xpath;
                    }
                }
                return GetXPathInternal(doc, newNode, ref newNode);
            }
            return xpath;
        }


        public static string GetAttributeValue(this HtmlNode node, string attributeName)
        {
            if (node == null || String.IsNullOrWhiteSpace(attributeName) || (!node.Attributes.Contains(attributeName)))
            {
                return String.Empty;
            }
            return node.Attributes[attributeName].Value;
        }

        public static string GetAttributeValue(this HtmlDocument htmlDocument, string nodeName, string nodeType, string attributeName)
        {
            var node = htmlDocument.FindNodeByName(nodeName, nodeType);
            if (node == null)
            {
                return string.Empty;
            }
            return GetAttributeValue(node, attributeName);
        }




        public static HtmlNode FindNodeByType(this HtmlDocument htmlDocument, string type)
        {
            return FindNodeByType(htmlDocument.DocumentNode, type);
        }

        public static HtmlNode FindNodeByType(this HtmlNode htmlNode, string type)
        {
            var elements = htmlNode.Descendants().Where(n => string.Equals(n.Name, type, StringComparison.OrdinalIgnoreCase));
            return elements.FirstOrDefault();
        }


        public static HtmlNode FindNodeById(this HtmlDocument htmlDocument, string id, string type = null)
        {
            return FindNodeById(htmlDocument.DocumentNode, id, type);
        }

        public static HtmlNode FindNodeById(this HtmlNode htmlNode, string id, string type = null)
        {
            var elements = htmlNode.Descendants().Where(n => !String.IsNullOrWhiteSpace(GetAttributeValue(n, "id")));
            if (!String.IsNullOrWhiteSpace(type))
            {
                elements = elements.Where(n => string.Equals(n.Name, type, StringComparison.OrdinalIgnoreCase));
            }
            return elements.Where(n => string.Equals(GetAttributeValue(n, "id"), id, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }


        public static HtmlNode FindNodeByName(this HtmlDocument htmlDocument, string name, string type = null)
        {
            return FindNodeByName(htmlDocument.DocumentNode, name, type);
        }

        public static HtmlNode FindNodeByName(this HtmlNode htmlNode, string name, string type = null)
        {
            var elements = htmlNode.Descendants().Where(n => !String.IsNullOrWhiteSpace(GetAttributeValue(n, "name")));
            if (!String.IsNullOrWhiteSpace(type))
            {
                elements = elements.Where(n => string.Equals(n.Name, type, StringComparison.OrdinalIgnoreCase));
            }
            return elements.Where(n => string.Equals(GetAttributeValue(n, "name"), name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        }

        public static HtmlNode FindNodeByClass(this HtmlDocument htmlDocument, string @class)
        {
            return FindNodeByClass(htmlDocument.DocumentNode, @class);
        }

        public static HtmlNode FindNodeByClass(this HtmlNode htmlNode, string @class)
        {
            var elements = htmlNode.Descendants().Where(n => !String.IsNullOrWhiteSpace(GetAttributeValue(n, "class")));
            return elements.Where(n => GetAttributeValue(n, "class").Contains(@class)).FirstOrDefault();
        }
    }
}
