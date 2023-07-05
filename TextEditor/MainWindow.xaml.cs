using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        //private readonly Cursor mirroredArrow = new("C:\\COMT\\PersonalProjects\\TextEditor\\TextEditor\\MirroredArrow.cur");
        private readonly Cursor mirroredArrow = new(@"D:\COMT\PersonalProjects\TextEditor\TextEditor\MirroredArrow.cur");
        private Visibility _lineNumbersVisibility = Visibility.Collapsed;
        private Stack<Action> undoStack = new Stack<Action>();
        private Stack<Action> redoStack = new Stack<Action>();

        public Visibility LineNumbersVisibility {
            get { return _lineNumbersVisibility; }
            set { _lineNumbersVisibility = value; }
        }

        public MainWindow() {
            InitializeComponent();
            RefreshMaximizeRestoreButton();
            txtBoxEditor.Text = "";
            SetUpLines(txtBoxEditor.Text);
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e) {
            this.WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreButtonClick(object sender, RoutedEventArgs e) {
            if (this.WindowState == WindowState.Maximized) {
                this.WindowState = WindowState.Normal;
            } else {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void RefreshMaximizeRestoreButton() {
            if (this.WindowState == WindowState.Maximized) {
                titleBarBtnMaximize.Visibility = Visibility.Collapsed;
                titleBarBtnRestoreDown.Visibility = Visibility.Visible;
            } else {
                titleBarBtnMaximize.Visibility = Visibility.Visible;
                titleBarBtnRestoreDown.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e) {
            this.RefreshMaximizeRestoreButton();
        }

        protected override void OnSourceInitialized(EventArgs e) {
            base.OnSourceInitialized(e);
            ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
        }

        public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == WM_GETMINMAXINFO) {
                // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
                // including the task bar.
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

                // Adjust the maximized size and position to fit the work area of the correct monitor
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

                if (monitor != IntPtr.Zero) {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    GetMonitorInfo(monitor, ref monitorInfo);
                    RECT rcWorkArea = monitorInfo.rcWork;
                    RECT rcMonitorArea = monitorInfo.rcMonitor;
                    mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                    mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top);
                    mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                    mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top);
                }

                Marshal.StructureToPtr(mmi, lParam, true);
            }

            return IntPtr.Zero;
        }

        private const int WM_GETMINMAXINFO = 0x0024;

        private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom) {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT {
            public int X;
            public int Y;

            public POINT(int x, int y) {
                this.X = x;
                this.Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        //private void LeftStackPanel_OnMouseEnter(object sender, MouseEventArgs e) {
        //    LeftStackPanel.Width = 75;
        //}

        //private void LeftStackPanel_OnMouseLeave(object sender, MouseEventArgs e) {
        //    LeftStackPanel.Width = 10;
        //}

        private void SetUpLines(string data) {  // needs revised - not working properly
            int noLines = string.IsNullOrEmpty(data) ? 1 : data.Split('\n').Length;
            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= noLines; i++) {
                sb.AppendLine(i.ToString());
            }

            txtBlockLineNums.Text = sb.ToString();
            txtBoxEditor.Text = data ?? "";
        }

        private void TextBox_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            Debug.WriteLine(((TextBox)sender).Name);
            txtBlockLineNums.ScrollToVerticalOffset(txtBoxEditor.VerticalOffset);
            //Debug.WriteLine("TextBox_ScrollChanged sender = " + sender.ToString());
            //Debug.WriteLine("TextBox_ScrollChanged sender type = " + sender.GetType());

            //TextBox lineNumsTextBox = (TextBox)((TextBox)sender).Template.FindName("txtBlockLineNums",((TextBox)sender));
            //lineNumsTextBox.ScrollToVerticalOffset(((TextBox)sender).VerticalOffset);
        }

        private void SetUpLines(object sender, TextChangedEventArgs e) {
            SetUpLines(txtBoxEditor.Text);
        }

        private void txtBlockLineNums_MouseEnter(object sender, MouseEventArgs e) {
            txtBlockLineNums.Cursor = mirroredArrow;
        }

        private void txtBlockLineNums_MouseLeave(object sender, MouseEventArgs e) {
            txtBlockLineNums.Cursor = Cursors.Arrow;
        }

        private void MenuItemLineNums_Checked(object sender, RoutedEventArgs e) {
            //lineNumsGrid.Visibility = Visibility.Visible;
            //LineNumbersVisibility = Visibility.Visible;
            //txtBoxEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;   // will need to be changed later -- do not belong in this method
        }

        private void menuItemLineNums_Unchecked(object sender, RoutedEventArgs e) {
            //lineNumsGrid.Visibility = Visibility.Collapsed;
            //LineNumbersVisibility = Visibility.Collapsed;
            //txtBoxEditor.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;   // will need to be changed later -- do not belong in this method
        }

        private void menuItemNew_Click(object sender, RoutedEventArgs e) {
            txtBoxEditor.Text = "";
        }

        private void menuItemNewWindow_Click(object sender, RoutedEventArgs e) {
            // Nothing yet as I don't know how to accomplish this...
        }

        private void menuItemOpen_Click(object sender, RoutedEventArgs e) {
            
        }

        private void OpenFile() {
            var openDialog = new OpenFileDialog {
                FileName = "FileName",
                DefaultExt = ".extension",
                Filter = "Filters (.extension)|*.extension"
            };

            bool? result = openDialog.ShowDialog();

            if (result == true) {
                string fileName = openDialog.FileName;
            }
        }

        private void SaveFile() {

        }

        private void menuItemSave_Click(object sender, RoutedEventArgs e) {
            
        }

        private void menuItemSaveAs_Click(object sender, RoutedEventArgs e) {
            var saveDialog = new SaveFileDialog {
                FileName = "*.txt",
                DefaultExt = "*.txt",
                Filter = "Text Documents (*.txt)|*.txt|All files (*.*)|*.*"
            };

            bool? result = saveDialog.ShowDialog();

            if (result == true) {
                string fileName = saveDialog.FileName;
                Debug.WriteLine(fileName);
            }
        }

        private void menuItemPageSetup_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemPrint_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemExit_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void MenuItemUndo_Click(object sender, RoutedEventArgs e) {
            ApplicationCommands.Undo.Execute(new(), Owner); //prob. gonna use stacks though...
        }

        private void menuItemRedo_Click(object sender, RoutedEventArgs e) {
            ApplicationCommands.Redo.Execute(new(), Owner); //prob. gonna use stacks though...
        }

        private void menuItemCut_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemCopy_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemPaste_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemDelete_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemBing_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemFind_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemNext_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemPrevious_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemReplace_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemGoTo_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemSelectAll_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemTimeDate_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemWordWrap_Checked(object sender, RoutedEventArgs e) {

        }

        private void menuItemWordWrap_Unchecked(object sender, RoutedEventArgs e) {

        }

        private void menuItemFont_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemZoomIn_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemZoomOut_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemDefaultZoom_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemStatusBar_Checked(object sender, RoutedEventArgs e) {
            //statusBarPanel.Visibility = Visibility.Visible;
            //Debug.WriteLine(statusBar);

            if (statusBar != null) {
                statusBar.Visibility = Visibility.Visible;
            }
        }

        private void menuItemStatusBar_Unchecked(object sender, RoutedEventArgs e) {
            //statusBarPanel.Visibility = Visibility.Collapsed;
            //Debug.WriteLine(statusBar);
            if (statusBar != null) {
                statusBar.Visibility = Visibility.Collapsed;
            }
        }

        private void menuItemHelp_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemFeedback_Click(object sender, RoutedEventArgs e) {

        }

        private void menuItemAbout_Click(object sender, RoutedEventArgs e) {

        }

        private void PART_HorizontalScrollBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue.Equals(true)) {
                lineNumsScrollBarNull.Visibility = Visibility.Visible;
            }

            if (e.NewValue.Equals(false)) {
                lineNumsScrollBarNull.Visibility = Visibility.Collapsed;
            }
        }

        private void txtBoxEditor_SelectionChanged(object sender, RoutedEventArgs e) {
            //Debug.WriteLine("txtBoxEditor_SelectionChanged called...\ntxtBoxEditor.CaretIndex = " + txtBoxEditor.CaretIndex);

            int caretVerticalPosition = 0, caretHorizontalPosition = txtBoxEditor.CaretIndex;

            //get vertical position
            var textBoxSubstring = txtBoxEditor.Text.Substring(0, txtBoxEditor.CaretIndex);
            var textLines = textBoxSubstring.Split("\n");

            caretVerticalPosition = textLines.Length;

            //get horizontal position
            caretHorizontalPosition = textLines[textLines.Length - 1].Length;

            statusBarLabelCaretPos.Content = $"LN: {caretVerticalPosition}  Ch: {caretHorizontalPosition + 1}";
        }
    }
}