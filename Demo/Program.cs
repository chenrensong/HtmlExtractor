using HtmlExtractor;
using System;
using System.Diagnostics;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "https://7q-fp-uw-yybx-b-jwgp-5qybpw-v-la-p-a-b4s-69g-.smartapps.cn/pages/detail/post/post?qid=0afedae01e9c62a5ec0f8b4413f4faa8";

            url = "https://km6pli.smartapps.cn/pages/note/note?id=5c7e6f6c000000000f02f4a5";

            url = "http://csy-n-l-zjs-en0n-uh-z-ic-1y-li-giq-z-f-a-t-a-tn-k-.smartapps.cn/pages/index/newsDetail/ycNews/ycNews?newsId=9225075";

            url = "https://l6unbo.smartapps.cn/pages/article/article?article_id=1852";

            url = "http://tech.hqew.com/fangan_137420";

            url = "https://www.leiphone.com/news/201902/TTBfNE3ncmSuusel.html";

            url = "https://baijiahao.baidu.com/s?id=1626396596168069280&wfr=spider&for=pc";

            url = "https://l6unbo.smartapps.cn/pages/article/article?article_id=1852";


            //url = "http://jt9wgp.smartapps.cn/pages/detail/detail?id=1592433437&categoryId=23851&comefrom=home";


            Get(url);

            url = "https://l6unbo.smartapps.cn/pages/article/article?article_id=1853";

            Get(url);

            url = "https://tynthl.smartapps.cn/pages/content/content?id=1618648";

            Get(url);

            Console.ReadKey();
        }

        private static void Get(string url)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();

            var result = Html.ExtractAsync(url).Result;

            sw.Stop();

            Console.WriteLine("完整耗时：" + Environment.NewLine + sw.ElapsedMilliseconds + "毫秒");

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                title = result.Title.Value,
                publishDate = result.PublishTime.Value,
                content = result.Content.Value,
                //html = result.Content.Html
            }));
        }
    }
}
