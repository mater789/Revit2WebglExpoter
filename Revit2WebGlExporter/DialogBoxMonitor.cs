using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

namespace Revit2WebGlExporter
{
    class DialogBoxMonitor
    {
        static private UIApplication _uiApp;
        EventHandler<DialogBoxShowingEventArgs> _dialogEventHandle;
        EventHandler<FailuresProcessingEventArgs> _failuresProcessingHandle;
        static private System.Timers.Timer _timer = null;
        static private readonly int _takeIntervalTime = 5000; //ms
        
        public DialogBoxMonitor(UIApplication app)
        {
            _uiApp = app;
            _dialogEventHandle = new EventHandler<DialogBoxShowingEventArgs>(OnDialogBoxShowingEvent);
            _failuresProcessingHandle = new EventHandler<FailuresProcessingEventArgs>(OnFailuresProcessingEvent);

            _timer = new System.Timers.Timer(_takeIntervalTime);
            _timer.Elapsed += OnTimerTask;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void Start()
        {
            _uiApp.DialogBoxShowing += _dialogEventHandle;
            _uiApp.Application.FailuresProcessing += _failuresProcessingHandle;
            _timer.Start();
        }

        public void Pause()
        {
            _uiApp.DialogBoxShowing -= _dialogEventHandle;
            _uiApp.Application.FailuresProcessing -= _failuresProcessingHandle;
            _timer.Stop();
        }

        static private void OnFailuresProcessingEvent(object sender, FailuresProcessingEventArgs e)
        {
            FailuresAccessor failuresAccessor = e.GetFailuresAccessor();
            IList<FailureMessageAccessor> failureMessages = failuresAccessor.GetFailureMessages();

            if (failureMessages.Count == 0)
                return;

            foreach (FailureMessageAccessor failure in failureMessages)
            {
                if (failure.GetSeverity() == FailureSeverity.Error)
                {
                    FailureResolutionType type = FailureResolutionType.Invalid;
                    if (failure.HasResolutions() && GetFailureResolutionType(failuresAccessor, failure, ref type))
                    {
                        failure.SetCurrentResolutionType(type);
                        failuresAccessor.ResolveFailure(failure);
                        e.SetProcessingResult(FailureProcessingResult.ProceedWithCommit);
                    }
                }
                if (failure.GetSeverity() == FailureSeverity.Warning)
                    failuresAccessor.DeleteWarning(failure);
            }
        }

        static private bool GetFailureResolutionType(FailuresAccessor failuresAccessor, FailureMessageAccessor failure, ref FailureResolutionType type)
        {
            IList<FailureResolutionType> resolutionTypeList = failuresAccessor.GetAttemptedResolutionTypes(failure);
            if (!resolutionTypeList.Contains(FailureResolutionType.Default) && failure.HasResolutionOfType(FailureResolutionType.Default))
            {
                type = FailureResolutionType.Default;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.CreateElements) && failure.HasResolutionOfType(FailureResolutionType.CreateElements))
            {
                type = FailureResolutionType.CreateElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.DeleteElements) && failure.HasResolutionOfType(FailureResolutionType.DeleteElements))
            {
                type = FailureResolutionType.DeleteElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.SkipElements) && failure.HasResolutionOfType(FailureResolutionType.SkipElements))
            {
                type = FailureResolutionType.SkipElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.MoveElements) && failure.HasResolutionOfType(FailureResolutionType.MoveElements))
            {
                type = FailureResolutionType.MoveElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.FixElements) && failure.HasResolutionOfType(FailureResolutionType.FixElements))
            {
                type = FailureResolutionType.FixElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.DetachElements) && failure.HasResolutionOfType(FailureResolutionType.DetachElements))
            {
                type = FailureResolutionType.DetachElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.QuitEditMode) && failure.HasResolutionOfType(FailureResolutionType.QuitEditMode))
            {
                type = FailureResolutionType.QuitEditMode;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.UnlockConstraints) && failure.HasResolutionOfType(FailureResolutionType.UnlockConstraints))
            {
                type = FailureResolutionType.UnlockConstraints;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.SetValue) && failure.HasResolutionOfType(FailureResolutionType.SetValue))
            {
                type = FailureResolutionType.SetValue;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.SaveDocument) && failure.HasResolutionOfType(FailureResolutionType.SaveDocument))
            {
                type = FailureResolutionType.SaveDocument;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.ShowElements) && failure.HasResolutionOfType(FailureResolutionType.ShowElements))
            {
                type = FailureResolutionType.ShowElements;
                return true;
            }
            else if (!resolutionTypeList.Contains(FailureResolutionType.Others) && failure.HasResolutionOfType(FailureResolutionType.Others))
            {
                type = FailureResolutionType.Others;
                return true;
            }

            return false;
        }

        static private void OnDialogBoxShowingEvent(object sender, DialogBoxShowingEventArgs e)
        {
            if (e.OverrideResult((int)System.Windows.Forms.DialogResult.OK))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.Cancel))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.Abort))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.Ignore))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.Yes))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.No))
                return;
            else if (e.OverrideResult((int)System.Windows.Forms.DialogResult.Retry))
                return;
        }

        private void OnTimerTask(object sender, ElapsedEventArgs args)
        {
            KillWindowsByText("复制的中心模型");
            KillWindowsByText("正在升级本地文件");
            KillWindowsByText("找不到中心模型");
        }

        private const int GW_CHILD = 5;
        private const int GW_HWNDNEXT = 2;
        private const int GWL_STYLE = (-16);
        private const int WS_VISIBLE = 268435456;
        private const int WS_BORDER = 8388608;
        private const int WM_CLOSE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("User32")]
        private extern static int GetWindow(int hWnd, int wCmd);

        [DllImport("User32")]
        private extern static int GetWindowLongA(int hWnd, int wIndx);

        [DllImport("user32.dll")]
        private static extern bool GetWindowText(int hWnd, StringBuilder title, int maxBufSize);

        [DllImport("user32", CharSet = CharSet.Auto)]
        private extern static int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "PostMessage")]
        public static extern int PostMessage(int hwnd, int wMsg, int wParam, int lParam);
        
        /// <summary>
        /// 关闭应用程序标题所代表的窗口
        /// </summary>
        /// <param name="text">要关闭窗口的标题</param>
        /// <returns>应用程序标题范型</returns>
        public void KillWindowsByText(string text)
        {
            bool isKilled = false;
            int handle = GetDesktopWindow().ToInt32();
            int hwCurr = GetWindow(handle, GW_CHILD);

            while (hwCurr > 0)
            {
                int IsTask = (WS_VISIBLE | WS_BORDER);
                int lngStyle = GetWindowLongA(hwCurr, GWL_STYLE);
                bool TaskWindow = ((lngStyle & IsTask) == IsTask);
                if (TaskWindow)
                {
                    int length = GetWindowTextLength(new IntPtr(hwCurr));
                    StringBuilder sb = new StringBuilder(2 * length + 1);
                    GetWindowText(hwCurr, sb, sb.Capacity);
                    string strTitle = sb.ToString();
                    if (!string.IsNullOrEmpty(strTitle) && strTitle == text)
                    {
                        isKilled = true;
                        PostMessage(hwCurr, WM_CLOSE, 0, 0);
                        break;
                    }
                }
                hwCurr = GetWindow(hwCurr, GW_HWNDNEXT);
            }

            if (!isKilled)
            {
                foreach (Process p in Process.GetProcesses(Environment.MachineName))
                {
                    if (p.MainWindowHandle != IntPtr.Zero && p.MainWindowTitle == text)
                    {
                        p.Kill();
                        break;
                    }
                }
            }
        }
    }
}
