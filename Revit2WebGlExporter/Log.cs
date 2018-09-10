using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;

namespace Revit2WebGlExporter
{
    class Log
    {
        public static void WriteLog(string strLog)
        {
            try
            {
                string logFolder = "C:\\RevitConverterLog";
                string logFile = Path.Combine(logFolder, DateTime.Now.ToString("yyyyMMdd") + ".log");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                FileStream fs;
                StreamWriter sw;
                if (File.Exists(logFile))
                    fs = new FileStream(logFile, FileMode.Append, FileAccess.Write);
                else
                    fs = new FileStream(logFile, FileMode.Create, FileAccess.Write);

                sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-ffff : ") + strLog);
                sw.Close();
                fs.Close();
            }
            catch
            { }
        }
    }
}
