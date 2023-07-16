using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TextEditor {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        //private readonly Cursor mirroredArrow = new("C:\\COMT\\PersonalProjects\\TextEditor\\TextEditor\\MirroredArrow.cur");
        //private readonly Cursor mirroredArrow = new(@"D:\COMT\PersonalProjects\TextEditor\TextEditor\MirroredArrow.cur");
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //int noLines = string.IsNullOrEmpty(data) ? 1 : data.Split('\n').Length;
            StringBuilder sb = new StringBuilder();
            //for (int i = 1; i <= txtBoxEditor.LineCount; i++) { //noLines; i++) {
            //    sb.AppendLine(i.ToString());
            //}

            //txtBlockLineNums.Text = sb.ToString();
            //txtBoxEditor.Text = data ?? "";


            if (txtBoxEditor.Text.Length <= 0) {
                txtBlockLineNums.Clear();
                txtBlockLineNums.Text = "1";
                return;
            }

            var txtBoxText = txtBoxEditor.Text;
            var txtBoxTextLines = txtBoxEditor.Text.Split('\n');

            int currentLineCharIndex = 0;
            string textToCurrentLineCharIndex = string.Empty;
            //var firstVisibleLineIndex = txtBoxEditor.GetFirstVisibleLineIndex();
            //var lastVisibleLineIndex = txtBoxEditor.GetLastVisibleLineIndex();

            List<string> lineNums = new();
            for (int i = 0; i < txtBoxEditor.LineCount; i++) {// firstVisibleLineIndex; i <= lastVisibleLineIndex; i++) {
                currentLineCharIndex = txtBoxEditor.GetCharacterIndexFromLineIndex(i);
                textToCurrentLineCharIndex = txtBoxText.Substring(0, currentLineCharIndex);
                var lines = textToCurrentLineCharIndex.Split('\n');

                if (lineNums.Contains(lines.Length.ToString()) == false)
                    lineNums.Add(lines.Length.ToString());
                else
                    lineNums.Add(string.Empty);
            }

            for (int i=0; i<lineNums.Count; i++) {
                if (i == lineNums.Count - 1)
                    sb.Append(lineNums[i]);
                else
                    sb.Append(lineNums[i] + '\n');
            }
            txtBlockLineNums.Text = sb.ToString();

            sw.Start();
            Debug.WriteLine("Stopwatch in milliseconds/seconds = " + sw.ElapsedMilliseconds + "/" + Convert.ToDouble(sw.ElapsedMilliseconds / 1000));
        }

        private void TextBox_ScrollChanged(object sender, ScrollChangedEventArgs e) {
            Debug.WriteLine(((TextBox)sender).Name);
            txtBlockLineNums.ScrollToVerticalOffset(txtBoxEditor.VerticalOffset);
            //SetUpLines(txtBoxEditor.Text);
            //Debug.WriteLine("TextBox_ScrollChanged sender = " + sender.ToString());
            //Debug.WriteLine("TextBox_ScrollChanged sender type = " + sender.GetType());

            //TextBox lineNumsTextBox = (TextBox)((TextBox)sender).Template.FindName("txtBlockLineNums",((TextBox)sender));
            //lineNumsTextBox.ScrollToVerticalOffset(((TextBox)sender).VerticalOffset);
        }

        private void SetUpLines(object sender, TextChangedEventArgs e) {
            SetUpLines(txtBoxEditor.Text);
        }

        private void txtBlockLineNums_MouseEnter(object sender, MouseEventArgs e) {
            //txtBlockLineNums.Cursor = mirroredArrow;
        }

        private void txtBlockLineNums_MouseLeave(object sender, MouseEventArgs e) {
            //txtBlockLineNums.Cursor = Cursors.Arrow;
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
            //see if file exists

            //try to save to file

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
            txtBoxEditor.TextWrapping = TextWrapping.Wrap;
            SetUpLines(txtBoxEditor.Text);
        }

        private void menuItemWordWrap_Unchecked(object sender, RoutedEventArgs e) {
            txtBoxEditor.TextWrapping = TextWrapping.NoWrap;
            SetUpLines(txtBoxEditor.Text);
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
            var textEditorText = txtBoxEditor.Text;
            var textEditorLines = textEditorText.Split('\n');

            //int WrappedLines1 = txtBoxEditor.GetLineIndexFromCharacterIndex(txtBoxEditor.Text.Length) + 1;
            //int WrappedLines2 = txtBoxEditor.LineCount;
            //int LineFeedsCount = txtBoxEditor.Text.Split(new[] { "\r" }, StringSplitOptions.None).Count();

            //Debug.WriteLine("WrappedLines1 = " + WrappedLines1);
            //Debug.WriteLine("WrappedLines2 = " + WrappedLines1);
            //Debug.WriteLine("LineFeedsCount = " + LineFeedsCount);

            //txtBoxEditor.TextWrapping = TextWrapping.NoWrap;
            //LineFeedsCount = txtBoxEditor.LineCount;

            //Debug.WriteLine("LineFeedsCount = " + LineFeedsCount);

            //int truelineCount = 0;
            //Debug.WriteLine($"textEditorLines.Length = {textEditorLines.Length}");
            //Debug.WriteLine($"txtBoxEditor.LineCount = {txtBoxEditor.LineCount}");
            //foreach (var line in textEditorLines) {
            //    //var characters = Encoding.UTF8.GetBytes(line);

            //    //foreach (byte b in characters) {
            //    //    System.Diagnostics.Debug.Write($"{b} ");
            //    //}

            //    //System.Diagnostics.Debug.WriteLine("");

            //    //Debug.WriteLine(BitConverter.ToString(characters));

            //    //Debug.WriteLine(txtBoxEditor);

            //    //Debug.WriteLine($"txtBoxEditor.GetLineIndexFromCharacterIndex(truelineCount) = {txtBoxEditor.GetLineIndexFromCharacterIndex(truelineCount)}");
            //    //Debug.WriteLine($"txtBoxEditor.GetCharacterIndexFromLineIndex(truelineCount) = {txtBoxEditor.GetCharacterIndexFromLineIndex(truelineCount)}");
            //    //truelineCount++;

            //    Debug.WriteLine($"");
            //}
            //Debug.WriteLine($"trueLineCount = {truelineCount}");

            //for (int i = 0; i < txtBoxEditor.LineCount; i++) {
            //    Debug.WriteLine($"txtBoxEditor.GetCharacterIndexFromLineIndex({i}) = {txtBoxEditor.GetCharacterIndexFromLineIndex(i)}");
            //    //Debug.WriteLine($"txtBoxEditor.GetLineIndexFromCharacterIndex({i}) = {txtBoxEditor.GetLineIndexFromCharacterIndex(i)}");
            //    Debug.WriteLine($"txtBoxEditor.GetLineIndexFromCharacterIndex(txtBoxEditor.GetCharacterIndexFromLineIndex({i})) = {txtBoxEditor.GetLineIndexFromCharacterIndex(txtBoxEditor.GetCharacterIndexFromLineIndex(i))}");
            //}

            //Debug.WriteLine("GetLines() entered...");
            //List<string> lines = new List<string>();
            //var source = txtBlockEditor;
            //var text = source.Text;
            //int offset = 0;
            //TextPointer lineStart = source.ContentStart.GetPositionAtOffset(1, LogicalDirection.Forward);
            //Debug.WriteLine("lineStart = " + lineStart.);
            //do {
            //    TextPointer lineEnd = lineStart != null ? lineStart.GetLineStartPosition(1) : null;
            //    Debug.WriteLine("lineEnd = " + lineEnd.);
            //    int length = lineEnd != null ? lineStart.GetOffsetToPosition(lineEnd) : text.Length - offset;
            //    Debug.WriteLine("length = " + length);
            //    //yield return text.Substring(offset, length);
            //    lines.Add(text.Substring(offset, length));
            //    offset += length;
            //    lineStart = lineEnd;
            //    Debug.WriteLine("offset = " + offset);
            //    Debug.WriteLine("lineStart = " + lineStart);
            //} while (lineStart != null);

            //Debug.WriteLine($"lines.Count = {lines.Count}");
            //Debug.WriteLine("lines:");
            //foreach (var line in lines) {
            //    Debug.WriteLine("\t" + line);
            //}

            ////Debug.WriteLine("before GetLines() called...");
            ////var lines = GetLines(txtBlockEditor);
            ////Debug.WriteLine("after GetLines() called...");
            ///


            //var textLines = txtBoxEditor.Text.Split("\n");
            //var txtBoxText = txtBoxEditor.Text;
            /* Debug.WriteLine("txtBoxText.IndexOf(\'\\n\') = " + txtBoxText.IndexOf('\n', txtBoxEditor.GetCharacterIndexFromLineIndex(txtBoxEditor.GetFirstVisibleLineIndex())));
            Debug.WriteLine("first index for next line = " + (txtBoxText.IndexOf('\n', txtBoxEditor.GetCharacterIndexFromLineIndex(txtBoxEditor.GetFirstVisibleLineIndex())) + 1));
            var txtBoxtTextSubstring = txtBoxText.Substring(0, txtBoxText.IndexOf('\n', txtBoxEditor.GetCharacterIndexFromLineIndex(txtBoxEditor.GetFirstVisibleLineIndex())));
            var txtSubstringLines = txtBoxtTextSubstring.Split('\n');
            Debug.WriteLine("line number for first line = " + (txtSubstringLines.Length - 1));  //remove '- 1' to get display line number
            //Debug.WriteLine("char index for line 1 = " + txtBoxEditor.GetCharacterIndexFromLineIndex(2));
            //var tBlockLineHeight = TextBlock.GetLineHeight(txtBoxEditor);
            //Debug.WriteLine($"TextBlock.GetLineHeight(txtBoxEditor) = {tBlockLineHeight}"); */

            //Debug.WriteLine("First visible line index = " + txtBoxEditor.GetFirstVisibleLineIndex());
            //Debug.WriteLine("Last visible line index = " + txtBoxEditor.GetLastVisibleLineIndex());
            //Debug.WriteLine("character index from first visible line = " + txtBoxEditor.GetCharacterIndexFromLineIndex(txtBoxEditor.GetFirstVisibleLineIndex()));
            //Debug.WriteLine("character index from second visible line = " + txtBoxEditor.GetCharacterIndexFromLineIndex(txtBoxEditor.GetFirstVisibleLineIndex() + 1));
            //Debug.WriteLine(txtBoxEditor.GetFirstVisibleLineIndex());

            /*
            var txtBoxText = txtBoxEditor.Text;
            var firstVisibleLineIndex = txtBoxEditor.GetFirstVisibleLineIndex();
            var lastVisibleLineIndex = txtBoxEditor.GetLastVisibleLineIndex();
            //var firstVisibleLineCharacterIndex = txtBoxEditor.GetCharacterIndexFromLineIndex(firstVisibleLineIndex);
            //var lastVisibleLineCharacterIndex = txtBoxEditor.GetCharacterIndexFromLineIndex(lastVisibleLineIndex);

            List<string> lineNums = new();
            for (int i = firstVisibleLineIndex; i <= lastVisibleLineIndex; i++) {
                Debug.WriteLine("line at index " + i + ":");
                int currentLineCharIndex = txtBoxEditor.GetCharacterIndexFromLineIndex(i);
                Debug.WriteLine("\tcurrent line character index = " + currentLineCharIndex);
                string textToCurrentLineCharIndex = txtBoxText.Substring(0, currentLineCharIndex);
                var lines = textToCurrentLineCharIndex.Split('\n');
                Debug.WriteLine("\tarray length for substring from character index 0 to " + currentLineCharIndex + " = " + lines.Length);

                Debug.WriteLine("\tlineNums contains split substring array length = " + lineNums.Contains(lines.Length.ToString()));
                if (lineNums.Contains(lines.Length.ToString()) == false)
                    lineNums.Add(lines.Length.ToString());
                else
                    lineNums.Add(string.Empty);
            }

            Debug.WriteLine("calculated line numbers:");
            foreach (var s in lineNums) {
                Debug.WriteLine("\t" + s);
            }
            Debug.WriteLine("---------------------------------------------------------------------------------------------------------");
            */

            List<int> ints = new List<int>();
            for (int i=0; i<=20;  i++) {
                ints.Add(i);
            }
            Debug.WriteLine("List ints contains 3:  " + ints.Contains(3));
            Debug.WriteLine("List ints contains 12: " + ints.Contains(12));
            Debug.WriteLine("List ints contains 20: " + ints.Contains(20));
            Debug.WriteLine("List ints contains -5: " + ints.Contains(-5));
            Debug.WriteLine("List ints contains 21: " + ints.Contains(21));

            //Debug.WriteLine($"");
            //for(int i=0;i<txtBoxEditor.LineCount;i++) {
            //    Debug.WriteLine($"character index for line {i} = {txtBoxEditor.GetCharacterIndexFromLineIndex(i)} with length of {txtBoxEditor.GetLineLength(i)}");
            //}
            //txtBoxEditor.TextWrapping = TextWrapping.NoWrap;
            //Debug.WriteLine("");
            //for (int i = 0; i < txtBoxEditor.LineCount; i++) {
            //    Debug.WriteLine($"character index for line {i} = {txtBoxEditor.GetCharacterIndexFromLineIndex(i)} with length of {txtBoxEditor.GetLineLength(i)}");
            //}
            //txtBoxEditor.TextWrapping = TextWrapping.Wrap;
        }

        //public static IEnumerable<string> GetLines(this TextBlock source) {
        //    Debug.WriteLine("GetLines() entered...");
        //    //var source = txtBlockEditor;
        //    var text = source.Text;
        //    int offset = 0;
        //    TextPointer lineStart = source.ContentStart.GetPositionAtOffset(1, LogicalDirection.Forward);
        //    Debug.WriteLine("lineStart = " + lineStart);
        //    do {
        //        TextPointer lineEnd = lineStart != null ? lineStart.GetLineStartPosition(1) : null;
        //        Debug.WriteLine("lineEnd = " + lineEnd);
        //        int length = lineEnd != null ? lineStart.GetOffsetToPosition(lineEnd) : text.Length - offset;
        //        Debug.WriteLine("length = " + length);
        //        yield return text.Substring(offset, length);
        //        offset += length;
        //        lineStart = lineEnd;
        //        Debug.WriteLine("offset = " + offset);
        //        Debug.WriteLine("lineStart = " + lineStart);
        //    } while (lineStart != null);
        //}

        private void PART_HorizontalScrollBar_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (e.NewValue.Equals(true)) {
                lineNumsScrollBarNull.Visibility = Visibility.Visible;
            }

            if (e.NewValue.Equals(false)) {
                lineNumsScrollBarNull.Visibility = Visibility.Collapsed;
            }
        }

        private void txtBoxEditor_SelectionChanged(object sender, RoutedEventArgs e) {
            Stopwatch sw = new();
            sw.Start();
            //Debug.WriteLine("txtBoxEditor_SelectionChanged called...\ntxtBoxEditor.CaretIndex = " + txtBoxEditor.CaretIndex);

            int caretVerticalPosition = 0, caretHorizontalPosition = txtBoxEditor.CaretIndex;

            //get vertical position
            var textBoxSubstring = txtBoxEditor.Text.Substring(0, txtBoxEditor.CaretIndex);
            var textLines = textBoxSubstring.Split("\n");

            caretVerticalPosition = textLines.Length;

            //get horizontal position
            caretHorizontalPosition = textLines[textLines.Length - 1].Length;

            statusBarLabelCaretPos.Content = $"LN: {caretVerticalPosition}  Ch: {caretHorizontalPosition + 1}";
            sw.Stop();
            Debug.WriteLine("Stopwatch in milliseconds/seconds = " + sw.ElapsedMilliseconds + "/" + Convert.ToDouble(sw.ElapsedMilliseconds / 1000));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            SetUpLines(txtBoxEditor.Text);
        }
    }
}