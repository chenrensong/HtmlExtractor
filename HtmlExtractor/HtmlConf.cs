using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlExtractor
{
    /// <summary>
    /// 配置
    /// </summary>
    public sealed class HtmlConf
    {
        /// <summary>
        /// 是否使用追加模式，默认为false
        /// 使用追加模式后，会将符合过滤条件的所有文本提取出来
        /// </summary>
        public bool AppendMode
        {
            get; set;
        } = true;

        /// <summary>
        /// 按行分析的深度，默认为6
        /// </summary>
        public int Depth
        {
            get; set;
        } = 6;

        /// <summary>
        /// 字符限定数，当分析的文本数量达到限定数则认为进入正文内容
        /// 默认180个字符数
        /// </summary>
        public int LimitCount
        {
            get; set;
        } = 80;//180;

        /// <summary>
        /// 确定文章正文头部时，向上查找，连续的空行到达_headEmptyLines，则停止查找
        /// </summary>
        public int HeadEmptyLines
        {
            get; set;
        } = 2;

        /// <summary>
        /// 用于确定文章结束的字符数
        /// </summary>
        public int EndLimitCharCount
        {
            get; set;
        } = 20;

        public string BaseUrl { get; set; }
    }
}
