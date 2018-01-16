using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

using System.Web;

namespace ImageService
{
    /// <summary>
    /// ImgHandler 的摘要说明
    /// </summary>
    public class ImgHandler : IHttpHandler
    {
        HttpResponse Response = null;
        public void ProcessRequest(HttpContext context)
        {
            Response = context.Response;
            try
            {
                int width = Convert.ToInt32(context.Request["width"]);//宽度
                string filePath = context.Request["path"];//相对路径 原图/upload/10004/Picture/2017_07_05//图1.jpg
                string orig_Path = context.Server.MapPath(filePath);//原图绝对地址 C://项目1/upload/10004/Picture/2017_07_05//图1.jpg
                #region 原图

                if (System.IO.File.Exists(orig_Path))
                {
                    if (width == 0)//原图
                    {
                        ResponseBinaryWrite(orig_Path);
                    }
                    else
                    {
                        //缩图换一个位置 方便清理 
                        Confighelper config = new Confighelper();

                        //context.Server.MapPath一定在注意 如果是虚拟目录 C://项目2/upload/10004/Picture/2017_07_05//图1.jpg 
                        string path = context.Server.MapPath(filePath.Replace(config.OrigDirectory, config.TempDirectory));
                        string tnImgPath = path + context.Request["ImageStyle"];
                        //创建文件夹
                        string dir = System.IO.Path.GetDirectoryName(tnImgPath);
                        if (!System.IO.Directory.Exists(dir))
                        {
                            System.IO.Directory.CreateDirectory(dir);
                        }
                        if (System.IO.File.Exists(tnImgPath))
                        {
                            ResponseBinaryWrite(tnImgPath);
                        }
                        else
                        {
                            #region 不存在则 进行动态缩图
                            Image origImage = Image.FromFile(orig_Path);
                            Bitmap Tn_imgsrc = ImageHelper.GetThumbnailImage(origImage, ThumbnailImageType.Zoom, width, 0);
                            if (config.IsRealGenerate == 1)
                            {
                                Tn_imgsrc.Save(tnImgPath);
                            }
                            origImage.Dispose();
                            ResponseBinaryWrite(tnImgPath);
                            #endregion
                        }
                    }
                }
                else
                {
                    Response.StatusCode = 404;
                    Response.Write(Get404Html());
                }
                #endregion
            }
            catch (Exception ex)
            {
                LogCommon.Logs.LogError(ex.ToString());
                Response.StatusCode = 404;
                Response.Write(Get404Html());
            }

        }
        private string Get404Html(string msg = "404 Not Found")
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.Append("<html>");
            html.Append("\n<head><title>" + msg + "</title></head>");
            html.Append("\n<body bgcolor=\"white\">");
            html.Append("\n<center><h1>" + msg + "</h1></center>");

            html.Append("\n</body>");
            html.Append("\n</html>");

            return html.ToString();
        }
        private void ResponseBinaryWrite(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            byte[] data = new byte[fs.Length];
            fs.Read(data, 0, data.Length);
            fs.Close();
            fs.Dispose();
            MemoryStream ms = new MemoryStream(data);
            Response.ClearContent();
            Response.ContentType = "*";
            Response.BinaryWrite(ms.ToArray());
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}