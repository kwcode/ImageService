using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;


/// <summary>
/// ImageHelper 的摘要说明
/// </summary>
public class ImageHelper
{
    #region 获取图片Exif属性
    public static ImageExif ExifInfo(Image img)
    {
        System.Drawing.Imaging.PropertyItem[] pt = img.PropertyItems;
        ImageExif exif = new ImageExif();
        for (int i = 0; i < pt.Length; i++)
        {
            PropertyItem p = pt[i];
            switch (pt[i].Id)
            {
                // 设备制造商 20. 
                case 0x010F:
                    exif.Make = System.Text.ASCIIEncoding.ASCII.GetString(pt[i].Value);
                    break;
                case 0x0110: // 设备型号 25. 
                    exif.Model = GetValueOfType2(p.Value);
                    break;
                case 0x0132: // 拍照时间 30.
                    exif.DateTime = GetValueOfType2(p.Value);
                    break;
                case 0x829A: // .曝光时间 
                    exif.ExposureTime = GetValueOfType5(p.Value) + " sec";
                    break;
                case 0x8827: // ISO 40.  
                    exif.ISO = GetValueOfType3(p.Value);
                    break;
                case 0x010E: // 图像说明info.description
                    exif.ImageTitle = GetValueOfType2(p.Value);
                    break;
                case 0x920a: //相片的焦距
                    exif.FocalLength = GetValueOfType5A(p.Value) + " mm";
                    break;
                case 0x829D: //相片的光圈值
                    exif.Aperture = GetValueOfType5A(p.Value);
                    break;
                case 0x0112:  //方向
                    exif.Orientation = ShortToString(p.Value, 0);
                    break;
                case 0x011A:
                    exif.XResolution = RationalToSingle(p.Value, 0);
                    break;
                case 0x011B:
                    exif.YResolution = RationalToSingle(p.Value, 0);
                    break;
                case 0x0128:
                    exif.ResolutionUnit = RationalToSingle(p.Value, 0);
                    break;
                case 0x0131:
                    exif.Software = ASCIIToString(p.Value);
                    break;
                case 0x0002:
                    exif.GPSLatitude = string.Format("{0}°{1}′{2}″", RationalToSingle(p.Value, 0), RationalToSingle(p.Value, 8), RationalToSingle(p.Value, 16));
                    break;
                case 0x0004:
                    exif.GPSLongitude = string.Format("{0}°{1}′{2}″", RationalToSingle(p.Value, 0), RationalToSingle(p.Value, 8), RationalToSingle(p.Value, 16));
                    break;
                case 0x0006:
                    exif.GPSAltitude = RationalToSingle(p.Value, 0);
                    break;

            }
        }
        return exif;

    }
    static string ByteToString(byte[] b, int startindex)
    {
        if (startindex + 1 <= b.Length)
        {
            return ((char)b[startindex]).ToString();
        }
        else
        {
            return string.Empty;
        }
    }

    static string ShortToString(byte[] b, int startindex)
    {
        if (startindex + 2 <= b.Length)
        {
            return BitConverter.ToInt16(b, startindex).ToString();
        }
        else
        {
            return string.Empty;
        }
    }
    static string RationalToSingle(byte[] b, int startindex)
    {
        if (startindex + 8 <= b.Length)
        {
            return (BitConverter.ToSingle(b, startindex) / BitConverter.ToSingle(b, startindex + 4)).ToString();
        }
        else
        {
            return string.Empty;
        }
    }
    static string ASCIIToString(byte[] b)
    {
        return Encoding.ASCII.GetString(b);
    }

    public static string GetValueOfType2(byte[] b)// 对type=2 的value值进行读取
    {
        return System.Text.Encoding.ASCII.GetString(b);
    }
    private static string GetValueOfType3(byte[] b) //对type=3 的value值进行读取
    {
        if (b.Length != 2) return "unknow";
        return Convert.ToUInt16(b[1] << 8 | b[0]).ToString();
    }
    private static string GetValueOfType5(byte[] b) //对type=5 的value值进行读取
    {
        if (b.Length != 8) return "unknow";
        UInt32 fm, fz;
        fm = 0;
        fz = 0;
        fz = Convert.ToUInt32(b[7] << 24 | b[6] << 16 | b[5] << 8 | b[4]);
        fm = Convert.ToUInt32(b[3] << 24 | b[2] << 16 | b[1] << 8 | b[0]);
        return fm.ToString() + "/" + fz.ToString() + " sec";
    }
    private static string GetValueOfType5A(byte[] b)//获取光圈的值
    {
        if (b.Length != 8) return "unknow";
        UInt32 fm, fz;
        fm = 0;
        fz = 0;
        fz = Convert.ToUInt32(b[7] << 24 | b[6] << 16 | b[5] << 8 | b[4]);
        fm = Convert.ToUInt32(b[3] << 24 | b[2] << 16 | b[1] << 8 | b[0]);
        double temp = (double)fm / fz;
        return (temp).ToString();
    }
    #endregion

    #region 附加图片水印

    /// <summary>
    /// 图片水印
    /// </summary>
    /// <param name="imgPath">服务器图片相对路径</param>
    /// <param name="filename">保存文件名</param>
    /// <param name="watermarkFilename">水印文件相对路径</param>
    /// <param name="watermarkStatus">图片水印位置 0=不使用 1=左上 2=中上 3=右上 4=左中  9=右下</param>
    /// <param name="quality">附加水印图片质量,0-100</param>
    /// <param name="watermarkTransparency">水印的透明度 1--10 10为不透明</param>
    public static void AddImageSignPic(HttpContext RequestContext, string imgPath, string filename, string watermarkFilename, int watermarkStatus, int quality, int watermarkTransparency)
    {
        byte[] _ImageBytes = File.ReadAllBytes(RequestContext.Server.MapPath(imgPath));
        Image img = Image.FromStream(new System.IO.MemoryStream(_ImageBytes));
        filename = RequestContext.Server.MapPath(filename);

        if (watermarkFilename.StartsWith("/") == false)
            watermarkFilename = "/" + watermarkFilename;
        watermarkFilename = RequestContext.Server.MapPath(watermarkFilename);
        if (!File.Exists(watermarkFilename))
            return;
        Graphics g = Graphics.FromImage(img);
        //设置高质量插值法
        //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
        //设置高质量,低速度呈现平滑程度
        //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
        Image watermark = new Bitmap(watermarkFilename);

        if (watermark.Height >= img.Height || watermark.Width >= img.Width)
            return;

        ImageAttributes imageAttributes = new ImageAttributes();
        ColorMap colorMap = new ColorMap();

        colorMap.OldColor = Color.FromArgb(255, 0, 255, 0);
        colorMap.NewColor = Color.FromArgb(0, 0, 0, 0);
        ColorMap[] remapTable = { colorMap };

        imageAttributes.SetRemapTable(remapTable, ColorAdjustType.Bitmap);

        float transparency = 0.5F;
        if (watermarkTransparency >= 1 && watermarkTransparency <= 10)
            transparency = (watermarkTransparency / 10.0F);

        float[][] colorMatrixElements = {
			new float[] {1.0f,  0.0f,  0.0f,  0.0f, 0.0f},
			new float[] {0.0f,  1.0f,  0.0f,  0.0f, 0.0f},
			new float[] {0.0f,  0.0f,  1.0f,  0.0f, 0.0f},
			new float[] {0.0f,  0.0f,  0.0f,  transparency, 0.0f},
			new float[] {0.0f,  0.0f,  0.0f,  0.0f, 1.0f}
		};

        ColorMatrix colorMatrix = new ColorMatrix(colorMatrixElements);

        imageAttributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        int xpos = 0;
        int ypos = 0;

        switch (watermarkStatus)
        {
            case 1:
                xpos = (int)(img.Width * (float).01);
                ypos = (int)(img.Height * (float).01);
                break;

            case 2:
                xpos = (int)((img.Width * (float).50) - (watermark.Width / 2));
                ypos = (int)(img.Height * (float).01);
                break;

            case 3:
                xpos = (int)((img.Width * (float).99) - (watermark.Width));
                ypos = (int)(img.Height * (float).01);
                break;

            case 4:
                xpos = (int)(img.Width * (float).01);
                ypos = (int)((img.Height * (float).50) - (watermark.Height / 2));
                break;

            case 5:
                xpos = (int)((img.Width * (float).50) - (watermark.Width / 2));
                ypos = (int)((img.Height * (float).50) - (watermark.Height / 2));
                break;

            case 6:
                xpos = (int)((img.Width * (float).99) - (watermark.Width));
                ypos = (int)((img.Height * (float).50) - (watermark.Height / 2));
                break;

            case 7:
                xpos = (int)(img.Width * (float).01);
                ypos = (int)((img.Height * (float).99) - watermark.Height);
                break;

            case 8:
                xpos = (int)((img.Width * (float).50) - (watermark.Width / 2));
                ypos = (int)((img.Height * (float).99) - watermark.Height);
                break;

            case 9:
                xpos = (int)((img.Width * (float).99) - (watermark.Width));
                ypos = (int)((img.Height * (float).99) - watermark.Height);
                break;
        }

        g.DrawImage(watermark, new Rectangle(xpos, ypos, watermark.Width, watermark.Height), 0, 0, watermark.Width, watermark.Height, GraphicsUnit.Pixel, imageAttributes);

        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        ImageCodecInfo ici = null;
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.MimeType.IndexOf("jpeg") > -1)
                ici = codec;
        }
        EncoderParameters encoderParams = new EncoderParameters();
        long[] qualityParam = new long[1];
        if (quality < 0 || quality > 100)
            quality = 80;

        qualityParam[0] = quality;

        EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityParam);
        encoderParams.Param[0] = encoderParam;

        if (ici != null)
            img.Save(filename, ici, encoderParams);
        else
            img.Save(filename);

        g.Dispose();
        img.Dispose();
        watermark.Dispose();
        imageAttributes.Dispose();
    }

    /// <summary>
    /// 文字水印
    /// </summary>
    /// <param name="imgPath">服务器图片相对路径</param>
    /// <param name="filename">保存文件名</param>
    /// <param name="watermarkText">水印文字</param>
    /// <param name="watermarkStatus">图片水印位置 0=不使用 1=左上 2=中上 3=右上 4=左中  9=右下</param>
    /// <param name="quality">附加水印图片质量,0-100</param>
    /// <param name="fontname">字体</param>
    /// <param name="fontsize">字体大小</param>
    public static void AddImageSignText(HttpContext RequestContext, string imgPath, string filename, string watermarkText, int watermarkStatus, int quality, string fontname, int fontsize)
    {
        byte[] _ImageBytes = File.ReadAllBytes(RequestContext.Server.MapPath(imgPath));
        Image img = Image.FromStream(new System.IO.MemoryStream(_ImageBytes));
        filename = RequestContext.Server.MapPath(filename);

        Graphics g = Graphics.FromImage(img);
        Font drawFont = new Font(fontname, fontsize, FontStyle.Regular, GraphicsUnit.Pixel);
        SizeF crSize;
        crSize = g.MeasureString(watermarkText, drawFont);

        float xpos = 0;
        float ypos = 0;

        switch (watermarkStatus)
        {
            case 1:
                xpos = (float)img.Width * (float).01;
                ypos = (float)img.Height * (float).01;
                break;

            case 2:
                xpos = ((float)img.Width * (float).50) - (crSize.Width / 2);
                ypos = (float)img.Height * (float).01;
                break;

            case 3:
                xpos = ((float)img.Width * (float).99) - crSize.Width;
                ypos = (float)img.Height * (float).01;
                break;

            case 4:
                xpos = (float)img.Width * (float).01;
                ypos = ((float)img.Height * (float).50) - (crSize.Height / 2);
                break;

            case 5:
                xpos = ((float)img.Width * (float).50) - (crSize.Width / 2);
                ypos = ((float)img.Height * (float).50) - (crSize.Height / 2);
                break;

            case 6:
                xpos = ((float)img.Width * (float).99) - crSize.Width;
                ypos = ((float)img.Height * (float).50) - (crSize.Height / 2);
                break;

            case 7:
                xpos = (float)img.Width * (float).01;
                ypos = ((float)img.Height * (float).99) - crSize.Height;
                break;

            case 8:
                xpos = ((float)img.Width * (float).50) - (crSize.Width / 2);
                ypos = ((float)img.Height * (float).99) - crSize.Height;
                break;

            case 9:
                xpos = ((float)img.Width * (float).99) - crSize.Width;
                ypos = ((float)img.Height * (float).99) - crSize.Height;
                break;
        }

        g.DrawString(watermarkText, drawFont, new SolidBrush(Color.White), xpos + 1, ypos + 1);
        g.DrawString(watermarkText, drawFont, new SolidBrush(Color.Black), xpos, ypos);

        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        ImageCodecInfo ici = null;
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.MimeType.IndexOf("jpeg") > -1)
                ici = codec;
        }
        EncoderParameters encoderParams = new EncoderParameters();
        long[] qualityParam = new long[1];
        if (quality < 0 || quality > 100)
            quality = 80;

        qualityParam[0] = quality;

        EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, qualityParam);
        encoderParams.Param[0] = encoderParam;

        if (ici != null)
        {
            img.Save(filename, ici, encoderParams);
        }
        else
        {
            img.Save(filename);
        }

        g.Dispose();
        img.Dispose();
    }

    #endregion


    #region 等比缩放图片

    /// <summary>
    /// 指定大小简单等比缩放图片
    /// </summary>
    /// <param name="original_image"></param>
    /// <param name="new_width"></param>
    /// <returns></returns>
    //public static Bitmap GetSimpleThumbnailImage(Image original_image, int new_width, int new_height)
    //{
    //    NewImageSize imageSize = new NewImageSize(original_image.Width, original_image.Height, new_width, new_height);
    //    Bitmap returnImage = null;
    //    returnImage = new Bitmap(original_image, imageSize.NewWidth, imageSize.NewHeight);
    //    return returnImage;
    //}

    /// <summary>
    /// 根据指定大小绘制高质量缩略图
    /// </summary>
    /// <param name="original_image"></param>
    /// <param name="new_width"></param>
    /// <returns></returns>
    public static Bitmap GetThumbnailImage(Image original_image, ThumbnailImageType titCase, int target_width, int target_height)
    {
        System.Drawing.Graphics graphic = null;
        Bitmap returnImage = null;

        int origin_width = original_image.Width;
        int origin_height = original_image.Height;
        int paste_x = 0, paste_y = 0;

        NewImageSize imageSize = new NewImageSize(original_image.Width, original_image.Height, target_width, target_height);
        int new_width = imageSize.NewWidth, new_height = imageSize.NewHeight;
        target_width = imageSize.NewWidth;
        target_height = imageSize.NewHeight;
        float origin_ratio = imageSize.OrignRatio;
        float target_ratio = imageSize.NewRatio;

        //缩略图生成方式
        switch (titCase)
        {
            //缩放全图到正确比例
            case ThumbnailImageType.Zoom:
                if (target_ratio > origin_ratio)
                {
                    //较宽图片
                    new_height = (int)Math.Floor((float)target_width / origin_ratio);
                    new_width = target_width;
                }
                else
                {
                    //较高图片
                    new_height = target_height;
                    new_width = (int)Math.Floor(origin_ratio * (float)target_height);
                }

                new_width = new_width > target_width ? target_width : new_width;
                new_height = new_height > target_height ? target_height : new_height;
                break;

            //截取高宽比中间部分
            case ThumbnailImageType.Cut:
                if (target_ratio > origin_ratio)
                {
                    //较高图片
                    new_height = (int)Math.Floor((float)target_width / origin_ratio);
                    new_width = target_width;
                }
                else
                {
                    //较宽图片
                    new_height = target_height;
                    new_width = (int)Math.Floor(origin_ratio * (float)target_height);
                }
                break;
        }

        paste_x = (target_width - new_width) / 2;
        paste_y = (target_height - new_height) / 2;

        // 创建缩略图
        returnImage = new System.Drawing.Bitmap(target_width, target_height);

        graphic = System.Drawing.Graphics.FromImage(returnImage);
        //指定画布大小并以透明色填充空余部分
        graphic.FillRectangle(new System.Drawing.SolidBrush(System.Drawing.Color.White), new System.Drawing.Rectangle(0, 0, target_width, target_height));

        //设置高质量绘画
        graphic.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphic.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        //输出缩略图
        graphic.DrawImage(original_image, paste_x, paste_y, new_width, new_height);

        return returnImage;
    }

    /// <summary>
    /// 用于计算新的图片大小
    /// </summary>
    private class NewImageSize
    {
        public int OrignWidth = 0;

        public int OrignHeight = 0;

        public float OrignRatio = 0;

        public int NewWidth = 0;

        public int NewHeight = 0;

        public float NewRatio = 0;

        public NewImageSize(int OrignWidth, int OrignHeight, int NewWidth, int NewHeight)
        {
            this.OrignWidth = OrignWidth;
            this.OrignHeight = OrignHeight;
            this.OrignRatio = (float)OrignWidth / (float)OrignHeight;

            this.NewWidth = NewWidth;
            this.NewHeight = NewHeight;
            if (this.NewWidth > 0 && this.NewHeight <= 0)
            {
                this.NewHeight = (int)Math.Floor((float)this.NewWidth / this.OrignRatio);
            }
            else if (this.NewHeight > 0 && this.NewWidth <= 0)
            {
                this.NewWidth = (int)Math.Floor((float)this.NewHeight * this.OrignRatio);
            }
            else if (this.NewHeight <= 0 && this.NewWidth <= 0)
            {
                throw new Exception("新的图片大小不能都小于零");
            }

            if (this.NewWidth >= this.OrignWidth)
            {
                this.NewWidth = this.OrignWidth;
            }

            if (this.NewHeight >= this.OrignHeight)
            {
                this.NewHeight = this.OrignHeight;
            }

            this.NewRatio = (float)this.NewWidth / (float)this.NewHeight;
        }
    }

    #endregion

    public static void MakeThumbnail(string originalImagePath, string thumbnailPath, int width, int height, string mode, string imageType)
    {
        Image originalImage = Image.FromFile(originalImagePath);
        int towidth = width;
        int toheight = height;
        int x = 0;
        int y = 0;
        int ow = originalImage.Width;
        int oh = originalImage.Height;

        switch (mode)
        {
            case "HW"://指定高宽缩放（可能变形）　　　　　　　　
                break;
            case "W"://指定宽，高按比例　　　　　　　　　　
                toheight = originalImage.Height * width / originalImage.Width;
                break;
            case "H"://指定高，宽按比例
                towidth = originalImage.Width * height / originalImage.Height;
                break;
            case "Cut"://指定高宽裁减（不变形）　　　　　　　　
                if ((double)originalImage.Width / (double)originalImage.Height > (double)towidth / (double)toheight)
                {
                    oh = originalImage.Height;
                    ow = originalImage.Height * towidth / toheight;
                    y = 0;
                    x = (originalImage.Width - ow) / 2;
                }
                else
                {
                    ow = originalImage.Width;
                    oh = originalImage.Width * height / towidth;
                    x = 0;
                    y = (originalImage.Height - oh) / 2;
                }
                break;
            default:
                break;
        }
        //新建一个bmp图片
        Image bitmap = new System.Drawing.Bitmap(towidth, toheight);

        //新建一个画板
        Graphics g = System.Drawing.Graphics.FromImage(bitmap);

        //设置高质量插值法
        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;

        //设置高质量,低速度呈现平滑程度
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        //清空画布并以透明背景色填充
        g.Clear(Color.Transparent);

        //在指定位置并且按指定大小绘制原图片的指定部分
        g.DrawImage(originalImage, new Rectangle(0, 0, towidth, toheight),
          new Rectangle(x, y, ow, oh),
          GraphicsUnit.Pixel);

        try
        {
            //以jpg格式保存缩略图
            switch (imageType.ToLower())
            {
                case ".gif":
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Gif);
                    break;
                case ".jpg":
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
                case ".bmp":
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Bmp);
                    break;
                case ".png":
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Png);
                    break;
                default:
                    bitmap.Save(thumbnailPath, System.Drawing.Imaging.ImageFormat.Jpeg);
                    break;
            }
        }
        catch (System.Exception e)
        {
            throw e;
        }
        finally
        {
            originalImage.Dispose();
            bitmap.Dispose();
            g.Dispose();
        }
    }
  

}

public class ImageExif
{
    /// <summary>
    /// 设备制造商
    /// </summary>
    public string Make { set; get; }
    /// <summary>
    /// 设备型号
    /// </summary>
    public string Model { set; get; }
    /// <summary>
    /// 方向
    /// </summary>
    public string Orientation { get; set; }
    /// <summary>
    /// 水平分辨率
    /// </summary>
    public string XResolution { get; set; }
    /// <summary>
    /// 垂直分辨率
    /// </summary>
    public string YResolution { get; set; }
    /// <summary>
    /// 分辨率单位
    /// </summary>
    public string ResolutionUnit { get; set; }
    /// <summary>
    /// 软件
    /// </summary>
    public string Software { get; set; }
    /// <summary>
    /// 拍照时间
    /// </summary>
    public string DateTime { get; set; }
    /// <summary>
    /// GPS纬度
    /// </summary>
    public string GPSLatitude { get; set; }
    /// <summary>
    /// GPS经度
    /// </summary>
    public string GPSLongitude { get; set; }
    /// <summary>
    /// GPS高度
    /// </summary>
    public string GPSAltitude { get; set; }
    /// <summary>
    /// 图片描述
    /// </summary>
    public string ImageTitle { get; set; }
    /// <summary>
    /// 曝光时间
    /// </summary>
    public string ExposureTime { get; set; }
    /// <summary>
    /// 感光度
    /// </summary>
    public string ISO { get; set; }
    /// <summary>
    /// 相机焦距
    /// </summary>
    public string FocalLength { get; set; }
    /// <summary>
    /// 光圈值
    /// </summary>
    public string Aperture { get; set; }
}

#region [ThumbnailImageType]缩率图生成类型
/// <summary>
/// 缩率图生成类型
/// </summary>
public enum ThumbnailImageType
{
    /// <summary>
    /// 缩放生成
    /// </summary>
    Zoom = 1,
    /// <summary>
    /// 截取生成
    /// </summary>
    Cut = 2
}
#endregion

