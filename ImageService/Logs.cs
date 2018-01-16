using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace LogCommon
{
    /// <summary>
    /// 日志记录
    /// </summary>
    public class Logs
    {
        #region 写日志 指定日志名称
        /// <summary>
        /// 写日志 指定日志名称 
        /// </summary>
        /// <param name="logName">日志名 (不含.txt)</param>
        /// <param name="content">内容</param>
        /// <param name="fileNameByDay">是否每小时产生一个文件</param>
        public static void write(string logName, string content, bool fileNameByDay = true)
        {
            string ext = System.IO.Path.GetExtension(logName);
            if (ext.Contains(".txt"))
            {
                logName = logName.Replace(".txt", "");
            }
            string fileName = fileNameByDay ? DateTime.Now.ToString("dd-HH") + " " + logName + ".txt" : logName + ".txt";

            string logPath = AppDomain.CurrentDomain.BaseDirectory + "log";
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            logPath += "\\" + DateTime.Now.ToString("yyyyMM");
            if (!Directory.Exists(logPath))
                Directory.CreateDirectory(logPath);

            writeLog(logPath, fileName, content);
        }

        #endregion

        #region 调试日志保存位置LogDeBug.txt
        /// <summary>
        /// 调试日志保存位置LogDeBug.txt
        /// </summary>
        /// <param name="obj"></param>
        public static void LogDeBug(object obj)
        {
            write("LogDeBug.txt", obj.ToString(), true);
        }
        #endregion

        #region 错误日志Error.txt
        /// <summary>
        /// 错误日志Error.txt  每小时产生一个文件
        /// </summary>
        /// <param name="obj"></param>
        public static void LogError(object obj)
        {
            write("Error.txt", obj.ToString(), true);
        }
        #endregion

        #region 新建目录写日志
        
        /// <summary>
        /// 新建目录写日志
        /// </summary>
        /// <param name="logName">日志名 无需后缀如 log</param>
        /// <param name="content">内容</param>
        /// <param name="LogsPath">指定特定日志目录{0}/yyyyMM/log.txt</param>
        public static void write2(string logName, string content, string LogsPath)
        {
            string ext = System.IO.Path.GetExtension(logName);
            if (ext.Contains(".txt"))
            {
                logName = logName.Replace(".txt", "");
            }
            string fileName = DateTime.Now.ToString("dd-HH") + " " + logName + ".txt";

            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);

            LogsPath = LogsPath.TrimEnd('\\');

            LogsPath += "\\" + DateTime.Now.ToString("yyyyMM");
            if (!Directory.Exists(LogsPath))
                Directory.CreateDirectory(LogsPath);

            writeLog(LogsPath, fileName, content);
        }

        #endregion

        #region 写日志到文件

        private static object writeLogObj = new object();

        private static void writeLog(string path, string fileName, string content)
        {
            string fullPath = path + "\\" + fileName;
            string info = "[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "] " + content + "\r\n";

            try
            {
                lock (writeLogObj)
                {
                    if (fullPath != nowStreamName)
                    {
                        if (nowStreamName != null)
                            closeLogFile();

                        mySw = new StreamWriter(fullPath, true, ASCIIEncoding.UTF8);
                        nowStreamName = fullPath;
                    }

                    mySw.Write(info);
                    mySw.Flush();
                }
            }
            catch
            {
                writeLostLog(path, content);
            }
            closeLogFile();
        }

        #endregion

        #region 第二次写失败的日志
        
        private static StreamWriter loseStreamWriter;
        private static string loseLogName = null;
        private static void writeLostLog(string path, string content)
        {
            try
            {
                string fileName = path + "\\" + DateTime.Now.ToString("dd-HH") + "_lostLog.txt";
                if (fileName != loseLogName)
                {
                    closeLoseLogFile();
                    loseStreamWriter = new StreamWriter(fileName, true, ASCIIEncoding.Default);
                }
                loseLogName = fileName;

                string info = "[" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "] " + content + "\r\n";
                loseStreamWriter.Write(info);
                loseStreamWriter.Flush();
            }
            catch { }
        }

        #endregion

        /// <summary>
        /// 关闭丢失日志记录文件流
        /// </summary>
        public static void closeLoseLogFile()
        {
            if (loseStreamWriter != null)
            {
                loseLogName = null;
                loseStreamWriter.Close();
                loseStreamWriter.Dispose();
                loseStreamWriter = null;
            }
        }

        private static StreamWriter mySw;

        private static string nowStreamName { get; set; }

        /// <summary>
        /// 关闭最后打开的日志文件流
        /// </summary>
        public static void closeLogFile()
        {
            if (mySw != null)
            {
                nowStreamName = null;
                mySw.Close();
                mySw.Dispose();
                mySw = null;
            }
        }
        　
    }
}
