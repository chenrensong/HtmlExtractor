namespace HtmlExtractor
{
    public class DomNode<T>
    {
        public string Html { get; internal set; }

        public T Value { get; internal set; }

        /// <summary>
        /// xpath
        /// </summary>
        public string XPath { get; internal set; }
    }

}
