using HtmlAgilityPack;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HtmlExtractor
{
    /// <summary>
    /// 小程序Web化扩展
    /// </summary>
    public static class SmartProgramExtensions
    {

        public static async Task<string> GetSmartProgreamHtml(this Page page, string htmlString)
        {
            bool isRender = false;
            var index = 0;
            var latestHtmlString = string.Empty;
            var timeout = 3000;//超时时间3s
            var delayTime = 100;

            do
            {
                //判断是否为小程序，是否已经渲染完成
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(htmlString);
                var swanPageNode = doc.FindNodeById("web-swan-page");
                if (swanPageNode != null)
                {
                    //try
                    //{
                    //    await page.WaitForNavigationAsync(new NavigationOptions()
                    //    {
                    //        WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
                    //    });
                    //}
                    //catch (Exception ex)
                    //{

                    //}

                    if (swanPageNode.FirstChild == null)//还没渲染
                    {
                        isRender = false;
                    }
                    else
                    {
                        if (swanPageNode.FirstChild.FirstChild != null)
                        {
                            if (latestHtmlString != htmlString)
                            {
                                latestHtmlString = htmlString;
                                index++;
                            }
                            timeout -= delayTime;
                            await Task.Delay(delayTime);
                            if (index >= 3 || timeout <= 0)
                            {
                                isRender = true;
                            }
                        }
                        else
                        {
                            isRender = false;
                        }
                    }
                }
                else
                {
                    isRender = true; //不是小程序
                }

                if (!isRender)
                {
                    htmlString = await page.GetContentAsync();
                }

                await Task.Delay(delayTime);

            } while (!isRender);

            return htmlString;
        }

    }
}
