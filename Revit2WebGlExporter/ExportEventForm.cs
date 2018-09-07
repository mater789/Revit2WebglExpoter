using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Revit2WebGlExporter
{
    public partial class ExportEventForm : Form
    {
        ExternalEvent ExportEvent;
        // 任务定时器
        static private System.Timers.Timer _taskTimer = null;
        static private readonly int _takeTaskIntervalTime = 2000; //ms
        static private readonly string _taskInfoFileName = "task.info";

        public ExportEventForm(ExternalEvent _event)
        {
            InitializeComponent();
            ExportEvent = _event;
            if (_taskTimer == null)
                _taskTimer = new System.Timers.Timer();

            _taskTimer.Interval = _takeTaskIntervalTime;
            _taskTimer.Elapsed += OnTimerTask;
            _taskTimer.AutoReset = true;
            _taskTimer.Enabled = true;
        }

        private void OnTimerTask(object sender, ElapsedEventArgs args)
        {
            _taskTimer.Stop();
            Assembly dll = Assembly.GetExecutingAssembly();
            string dllFolder = Path.GetDirectoryName(dll.Location);
            string taskFile = Path.Combine(dllFolder, _taskInfoFileName);

            try
            {
                if (File.Exists(taskFile))
                {
                    string taskInfo = File.ReadAllText(taskFile);
                    Log.WriteLog("取到任务 : " + taskInfo);

                    File.Delete(taskFile);  //  防止重复转同一条任务
                    CommonSettings settings = CommonSettings.DeserializeWithJson(taskInfo);
                    if (settings == null)
                    {
                        Log.WriteLog("解析任务信息失败");
                        return;
                    }
                    ExportEventHandler.Settings = settings;
                    while (ExportEvent.IsPending)
                        Thread.Sleep(100);
                    ExportEvent.Raise();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLog("异常 : " + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
                _taskTimer.Start();
            }
        }


        ///////////////////////////////////////////////////////////////////////////////////////
        //发送消息的机制由于转换服务是在admin用户下运行，因此Revit也必须在admin用户下启动    //
        //但这会导致每次Revit启动都提示是否加载插件，暂时无法解决，因此使用文件的形式传递消息//
        ///////////////////////////////////////////////////////////////////////////////////////

        //private const int WM_COPYDATA = 0x004A;

        //[StructLayout(LayoutKind.Sequential)]
        //public struct COPYDATASTRUCT
        //{
        //    public IntPtr dwData;
        //    public int cbData;
        //    public IntPtr lpData;

        //    public void Dispose()
        //    {
        //        if (lpData != IntPtr.Zero)
        //        {
        //            Marshal.FreeCoTaskMem(lpData);
        //            lpData = IntPtr.Zero;
        //            cbData = 0;
        //        }
        //    }

        //    public string AsAnsiString { get { return Marshal.PtrToStringAnsi(lpData, cbData); } }
        //}

        //protected override void DefWndProc(ref Message m)
        //{
        //    switch (m.Msg)
        //    {
        //        case WM_COPYDATA:
        //            {
        //                DoExport(m);
        //                break;
        //            }
        //        default:
        //            base.DefWndProc(ref m);
        //            break;
        //    }
        //}

        //private bool DoExport(Message m)
        //{
        //    try
        //    {
        //        COPYDATASTRUCT cds = new COPYDATASTRUCT();
        //        Type t = cds.GetType();
        //        cds = (COPYDATASTRUCT)m.GetLParam(t);
        //        string serializedSettings = cds.AsAnsiString;

        //        CommonSettings settings = CommonSettings.DeserializeWithJson(serializedSettings);
        //        if (settings == null)
        //            return false;
        //        ExportEventHandler.Settings = settings;
        //        while (ExportEvent.IsPending)
        //            Thread.Sleep(100);
        //        ExportEvent.Raise();

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}
    }
}
