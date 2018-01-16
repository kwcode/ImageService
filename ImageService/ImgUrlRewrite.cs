using System;
using System.Collections.Generic;

using System.Text.RegularExpressions;
using System.Web;

namespace ImageService
{
    /// <summary>
    /// 图片服务处理入口
    /// </summary>
    public class ImgUrlRewrite : IHttpModule
    {

        public void Init(HttpApplication context)
        {
            context.BeginRequest += OnBeginRequest;

        }
        private void OnBeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            try
            {
                //CurrentExecutionFilePath 不会带有 ?后面的， RawUrl 带有?t=1
                string requestUrl = app.Request.CurrentExecutionFilePath;
                string path = requestUrl;//upload/10004/Picture/2017_07_05/20170305100315_66580.jpg_w400.jpg
                if (!string.IsNullOrEmpty(path) && path.Length > 1)
                {
                    Confighelper config = new Confighelper();
                    if (path.Contains(config.OrigDirectory))
                    {
                        //正则匹配格式
                        //http://img.cms.com/upload/10004/Picture/2017_07_05/20170305100315_66580.jpg_w400.jpg
                        if (!ImgRewriteUrl(app, path))
                        {
                            app.Response.StatusCode = 404;
                            app.Response.Write(Get404Html());
                        }
                    }
                    else
                    {
                        app.Response.StatusCode = 404;
                        app.Response.Write(Get404Html());
                    }
                }
                else
                {
                    app.Response.StatusCode = 404;
                    app.Response.Write(Get404Html());
                }

            }
            catch (Exception ex)
            {
                app.Response.StatusCode = 404;
                app.Response.Write(Get404Html());
            }

        }

        #region 根据路径地址,获取原图地址
        private string GetFilePath(string rawUrl)
        {
            string filePath = rawUrl;
            Confighelper config = new Confighelper();
            foreach (string item in config.ImageStyleList)
            {
                if (rawUrl.EndsWith("_" + item))
                {
                    filePath = rawUrl.Replace("_" + item, "");
                }
            }
            return filePath;
        }
        #endregion

        /// <summary>
        /// 图片规则 缩图为_w200.jpg
        /// 判断是否有2个点 否则为原图
        /// </summary>
        /// <param name="path"></param>
        public bool ImgRewriteUrl(HttpApplication app, string rawUrl)
        {
            bool b = false;
            int dianCount = Regex.Matches(rawUrl, @"[.]").Count;
            if (dianCount == 1)//一个点的为原图
            {
                app.Response.StatusCode = 200;
                string destinationUrl = "/ImgHandler.ashx?path=" + rawUrl + "&width=0";
                app.Context.RewritePath(destinationUrl, false);
                b = true;
            }
            else
            {
                //缩图处理
                //截取最后一个_
                int indexOf = rawUrl.IndexOf("_w");
                if (indexOf > -1)
                {
                    string filePath = rawUrl.Substring(0, indexOf);
                    int width = 0;
                    string ImageStyle = rawUrl.Replace(filePath + "_", "");//w200.jpg
                    if (IsImageStyle(ImageStyle))//是否有样式名称
                    {
                        width = Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(ImageStyle.Replace("w", "")));
                        app.Response.StatusCode = 200;
                        string destinationUrl = "/ImgHandler.ashx?path=" + filePath + "&width=" + width + "&ImageStyle=_" + ImageStyle;
                        app.Context.RewritePath(destinationUrl, false);
                        b = true;
                    }
                }
            }
            return b;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ImageStyle"></param>
        /// <returns></returns>
        private bool IsImageStyle(string ImageStyle)
        {
            bool b = false;
            Confighelper config = new Confighelper();
            b = config.ImageStyleList.Contains(ImageStyle);
            //if (ImageStyle == "w100.jpg"
            //    || ImageStyle == "w200.jpg"
            //    || ImageStyle == "w300.jpg"
            //    || ImageStyle == "w400.jpg"
            //    || ImageStyle == "w500.jpg"
            //    || ImageStyle == "w600.jpg"
            //    || ImageStyle == "w700.jpg"
            //    || ImageStyle == "w800.jpg")
            //{
            //    b = true;
            //}
            return b;
        }

        private string Get404Html()
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.Append("<html>");
            html.Append("\n<head><title>404 Not Found</title></head>");
            html.Append("\n<body bgcolor=\"white\">");
            html.Append("\n<center><h1>404 Not Found</h1></center>");

            html.Append("\n</body>");
            html.Append("\n</html>");

            return html.ToString();
        }
        public void Dispose()
        {

        }
    }
}