using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Revit2WebGlExporter.Exporter;
using System;
using System.Threading;
using System.Text;
using System.IO;

namespace Revit2WebGlExporter
{
    class ExportEventHandler : IExternalEventHandler
    {
        static public CommonSettings Settings { get; set; }
        DialogBoxMonitor _dialogMonitor;

        public void Execute(Autodesk.Revit.UI.UIApplication app)
        {
            Log.WriteLog("*************************开始转换*************************");

            int resultCode = 0;
            string errorDesc = string.Empty;
            Document activeDoc = null;

            try
            {
                if (_dialogMonitor == null)
                    _dialogMonitor = new DialogBoxMonitor(app);
                _dialogMonitor.Start();

                if (Settings == null)
                {
                    resultCode = -1;
                    errorDesc = "Settings为空";
                    return;
                }
                
                UIDocument uiDoc = app.OpenAndActivateDocument(Settings.InputFilePath);
                if (uiDoc == null)
                {
                    resultCode = -2;
                    errorDesc = "打开文件失败，UIDocument为空";
                    return;
                }

                activeDoc = uiDoc.Document;
                if (activeDoc == null)
                {
                    resultCode = -3;
                    errorDesc = "打开文件失败，Document为空";
                    return;
                }

                if (!ViewHelper.SetFirst3DView(uiDoc))
                {
                    resultCode = -4;
                    errorDesc = "打开3D视图失败";
                    return;
                }
                Log.WriteLog("当前视图 : " + uiDoc.ActiveView.Name);

                DocumentExporter modelExporter = new Exporter.DocumentExporter(app.Application);
                DocumentExporter.ErrorType error = modelExporter.ExportDocumentWithAcvtiveView(activeDoc);
                if (error != DocumentExporter.ErrorType.Success)
                {
                    resultCode = -5;
                    if (error == DocumentExporter.ErrorType.NoElement)
                        errorDesc = "当前视图无几何信息";
                    else if (error == DocumentExporter.ErrorType.UnSupportedGeometry)
                        errorDesc = "写WebGl文件失败";
                    else
                        errorDesc = "其他";

                    return;
                }
            }
            catch (Exception ex)
            {
                resultCode = -6;
                errorDesc = ex.Message + "\r\n" + ex.StackTrace;
            }
            finally
            {
                Log.WriteLog("转换结果 : " + resultCode.ToString());
                if (!string.IsNullOrEmpty(errorDesc))
                    Log.WriteLog(errorDesc);
                
                int count = 0;
                while (count < 10)
                {
                    count++;
                    Log.WriteLog("尝试写入result文件 ： 第" + count.ToString() + "次");

                    if (!WriteResultFile(resultCode.ToString() + "\r\n" + errorDesc))
                        Thread.Sleep(1000);
                    else
                        break;
                }

                if (activeDoc != null)
                    activeDoc.Close();

                _dialogMonitor.Pause();
                Settings = null;

                Log.WriteLog("*************************转换结束*************************\r\n");
            }
        }

        public string GetName()
        {
            return "NDS Revit Exporter";
        }

        private bool WriteResultFile(string context)
        {
            string resultFilePath = Path.Combine(Settings.OutputFolder, "result.res");
            FileStream fs = null;
            try
            {
                fs = new FileStream(resultFilePath, FileMode.Create, FileAccess.ReadWrite);
                byte[] buffer = Encoding.Default.GetBytes(context);
                fs.Write(buffer, 0, buffer.Length);
                Log.WriteLog("写入result文件成功");
                return true;
            }
            catch(Exception ex)
            {
                Log.WriteLog("写入result文件时发生异常 : " + ex.Message + "\r\n" + ex.StackTrace);
                return false;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Flush();
                    fs.Close();
                }
            }
        }
    }
}
