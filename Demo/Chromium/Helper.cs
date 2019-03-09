using HtmlExtractor;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Chromium
{
    class Helper
    {

        public static Browser Browser = null;
        private const int ChromiumRevision = BrowserFetcher.DefaultRevision;


        public static async Task<HtmlMeta> DownloadAndExtractAsync(string url, HtmlConf htmlConf = null)
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

            var htmlData = Html.Extract(htmlString, htmlConf);

            sw.Stop();
            Console.WriteLine("提取耗时：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");

            return htmlData;
        }


    }
}
