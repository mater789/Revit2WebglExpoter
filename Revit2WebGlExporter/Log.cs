using System;
using System.IO;
using System.Reflection;

namespace Revit2WebGlExporter
{
    class Log
    {
        static string _logFolder;
        static string _logFile;

        public static void Initial()
        {
            try
            {
                Assembly dll = Assembly.GetExecutingAssembly();
                string dllFolder = Path.GetDirectoryName(dll.Location);
                _logFolder = Path.Combine(dllFolder, "RevitExporterLog");
                if (!Directory.Exists(_logFolder))
                    Directory.CreateDirectory(_logFolder);

                DateTime now = DateTime.Now;
                string[] files = Directory.GetFiles(_logFolder);
                foreach (string file in files)
                {
                    DateTime fileDate = DateTime.Parse(Path.GetFileNameWithoutExtension(file));
                    TimeSpan span = now - fileDate;
                    if (span.Days > 10)
                        File.Delete(file);  // 只保留十天以内的
                }

                _logFile = Path.Combine(_logFolder, now.ToString("yyyyMMdd") + ".log");
            }
            catch (Exception ex)
            {

            }
        }

        public static void WriteLog(string strLog)
        {
            try
            {
                FileStream fs;
                StreamWriter sw;
                if (File.Exists(_logFile))
                    fs = new FileStream(_logFile, FileMode.Append, FileAccess.Write);
                else
                    fs = new FileStream(_logFile, FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(fs);
                sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "   ---   " + strLog);
                sw.Close();
                fs.Close();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
