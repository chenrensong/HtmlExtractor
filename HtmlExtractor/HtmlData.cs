using System;
using System.Collections.Generic;
using System.Text;

namespace HtmlExtractor
{
    /// <summary>
    /// 格式化数据
    /// </summary>
    public class HtmlMeta
    {
        /// <summary>
        /// 标题
        /// </summary>
        public DomNode<string> Title { get; internal set; }

        /// <summary>
        /// 仅内容
        /// </summary>
        public DomNode<string> Content { get; internal set; }

        /// <summary>
        /// 发布日期
        /// </summary>
        public DomNode<DateTime> PublishTime { get; internal set; }

    }



}
