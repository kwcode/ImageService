using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ImageService
{
    public class Confighelper
    {
        public Confighelper()
        {
            try
            {
                IsRealGenerate = Convert.ToInt32(ConfigurationManager.AppSettings["IsRealGenerate"]);
                TempDirectory = ConfigurationManager.AppSettings["TempDirectory"];
                OrigDirectory = ConfigurationManager.AppSettings["OrigDirectory"];
                ImageStyleList = new List<string>();
                string imageStyle = ConfigurationManager.AppSettings["ImageStyle"];
                ImageStyleList = imageStyle.Split(',').ToList();
            }
            catch (Exception ex)
            {
                LogCommon.Logs.LogError(ex.ToString()); 
            }
        }
        public List<string> ImageStyleList { get; set; }
        public int IsRealGenerate { get; set; }
        public string TempDirectory { get; set; }
        public string OrigDirectory { get; set; }

    }
}