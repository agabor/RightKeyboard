using System;
using System.Runtime.InteropServices;

namespace RightKeyboard.Win32 {
	[StructLayout(LayoutKind.Sequential)]
	public class RAWINPUTHEADER {
		public uint dwType;
		public uint dwSize;
		public IntPtr hDevice;
		public IntPtr wParam;
	}
}