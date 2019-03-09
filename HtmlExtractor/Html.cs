using HtmlExtractor.Extractor;

namespace HtmlExtractor
{
    public class Html
    {
        /// <summary>
        /// 从给定的Html原始文本中获取正文信息
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static HtmlMeta Extract(string html, HtmlConf htmlConf = null)
        {
            if (htmlConf == null)
            {
                htmlConf = HtmlConf.Defalut;
            }
            //<swan-view class=" postcontent" id="_7cf7">
            //<swan-view class=" postTitle" id="_7cf10">
            //<swan-view class=" publishTime" id="_7cf19">
            //<swan-view class=" authorNname" id="_7cf16">
            WebExtractor webExtractor = new WebExtractor(html, htmlConf);
            return webExtractor.Get();
        }


    }
}
