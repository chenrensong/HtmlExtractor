using HtmlAgilityPack;
using HtmlExtractor.Extractor;
using PuppeteerSharp;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HtmlExtractor
{
    public class Html
    {

        private const int ChromiumRevision = BrowserFetcher.DefaultRevision;


        /// <summary>
        /// 默认配置文件
        /// </summary>
        private static HtmlConf DefaultConf = new HtmlConf();


        public static Browser Browser = null;


        public static async Task<HtmlMeta> ExtractAsync(string url, HtmlConf htmlConf = null)
        {
            Stopwatch sw = new Stopwatch();


            if (Browser == null)
            {

                sw.Start();
                //Download chromium browser revision package
                await new BrowserFetcher().DownloadAsync(ChromiumRevision);

                sw.Stop();

                Console.WriteLine("浏览器下载：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");

                sw.Restart();
                //Enabled headless option
                var launchOptions = new LaunchOptions { Headless = true };
                //Starting headless browser
                Browser = await Puppeteer.LaunchAsync(launchOptions);
                sw.Stop();

                Console.WriteLine("浏览器初始化：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");

            }


            sw.Restart();

            //New tab page
            var page = await Browser.NewPageAsync();

            //Request URL to get the page
            var response = await page.GoToAsync(url);

            //Get and return the HTML content of the page
            var htmlString = await page.GetContentAsync();

            sw.Stop();

            Console.WriteLine("浏览器渲染：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");


            sw.Restart();
            ///Smart Program Html Content 
            var newHtmlString = await page.GetSmartProgreamHtml(htmlString);

            if (!string.IsNullOrEmpty(newHtmlString))
            {
                htmlString = newHtmlString;
            }

            sw.Stop();

            Console.WriteLine("小程序渲染耗时：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");


            #region Dispose resources
            //Close tab page
            await page.CloseAsync();

            //Close headless browser, all pages will be closed here.
            //await Browser.CloseAsync();
            #endregion

            sw.Restart();

            var htmlData = Extract(htmlString, htmlConf);

            sw.Stop();
            Console.WriteLine("提取耗时：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");

            return htmlData;
        }




        /// <summary>
        /// 从给定的Html原始文本中获取正文信息
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static HtmlMeta Extract(string html, HtmlConf htmlConf = null)
        {
            if (htmlConf == null)
            {
                htmlConf = DefaultConf;
            }
            //<swan-view class=" postcontent" id="_7cf7">
            //<swan-view class=" postTitle" id="_7cf10">
            //<swan-view class=" publishTime" id="_7cf19">
            //<swan-view class=" authorNname" id="_7cf16">
            var _publishDateExtractor = new PublishDateExtractor();
            var _titleExtractor = new TitleExtractor();
            var _contentExtractor = new ContentExtractor();

            var title = _titleExtractor.Get(html, htmlConf);
            var publishDate = _publishDateExtractor.Get(html, htmlConf);
            var content = _contentExtractor.Get(html, htmlConf);

            return new HtmlMeta()
            {
                Title = title,
                Content = content,
                PublishTime = publishDate
            };
        }


    }
}
