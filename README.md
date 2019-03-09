# HtmlExtractor
 支持通用网页以及百度智能小程序HTML信息提取，能够提取 Content、Title、PublishTime 等字段，项目核心代码基于stanzhai的Html2Article进行改造

 [![NuGet](https://img.shields.io/nuget/v/HtmlExtractor.svg)](https://www.nuget.org/packages/HtmlExtractor/)

 ## 无需XPath即可提取网页meta信息
* Content基于陈鑫的《基于行块分布函数的通用网页正文抽取算法》匹配，部分正文可提取出XPath
* Title采用正则匹配
* PublishTime采用正则匹配

```C#
string html = "<html>....</html>";
var meta = Html.Extract(html);
```

 ##	项目依赖
- HtmlAgilityPack >= 1.9.1


## 许可和引用
* 依照Apache 2.0许可发布
* [Html2Article](https://github.com/stanzhai/Html2Article)Apache 2.0许可