using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace RightKeyboard.Win32 {
	/// <summary>
	/// Exposes the Win32 API functions
	/// </summary>
	public static class API {
		#region Constants
		public const uint RIDEV_REMOVE = 0x00000001;
		public const uint RIDEV_EXCLUDE = 0x00000010;
		public const uint RIDEV_PAGEONLY = 0x00000020;
		public const uint RIDEV_NOLEGACY = 0x00000030;
		public const uint RIDEV_INPUTSINK = 0x00000100;
		public const uint RIDEV_CAPTUREMOUSE = 0x00000200;  // effective when mouse nolegacy is specified, otherwise it would be an error
		public const uint RIDEV_NOHOTKEYS = 0x00000200;  // effective for keyboard.
		public const uint RIDEV_APPKEYS = 0x00000400;  // effective for keyboard.

		public const int WM_INPUT = 0x00FF;

		public const int RID_INPUT = 0x10000003;
		public const int RID_HEADER = 0x10000005;

		public const int WM_POWERBROADCAST = 0x0218;

		public const int PBT_APMQUERYSUSPEND = 0x0000;
		public const int PBT_APMQUERYSTANDBY = 0x0001;
		public const int PBT_APMQUERYSUSPENDFAILED = 0x0002;
		public const int PBT_APMQUERYSTANDBYFAILED = 0x0003;
		public const int PBT_APMSUSPEND = 0x0004;
		public const int PBT_APMSTANDBY = 0x0005;
		public const int PBT_APMRESUMECRITICAL = 0x0006;
		public const int PBT_APMRESUMESUSPEND = 0x0007;
		public const int PBT_APMRESUMESTANDBY = 0x0008;
		public const int PBTF_APMRESUMEFROMFAILURE = 0x00000001;
		public const int PBT_APMBATTERYLOW = 0x0009;
		public const int PBT_APMPOWERSTATUSCHANGE = 0x000A;
		public const int PBT_APMOEMEVENT = 0x000B;
		public const int PBT_APMRESUMEAUTOMATIC = 0x0012;
		#endregion

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool RegisterRawInputDevices(
			[MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] RAWINPUTDEVICE[] pRawInputDevices,
			int uiNumDevices,
			int cbSize
		);

		public static bool RegisterRawInputDevices(params RAWINPUTDEVICE[] rawInputDevices) {
			return RegisterRawInputDevices(
				rawInputDevices,
				rawInputDevices.Length,
				Marshal.SizeOf(typeof(RAWINPUTDEVICE))
			);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RAWINPUTDEVICELIST {
			public IntPtr hDevice;
			public int dwType;
		}

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceList([Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref uint puiNumDevices, int cbSize);

		public static RAWINPUTDEVICELIST[] GetRawInputDeviceList() {
			uint nDevices = 0;

			int res = GetRawInputDeviceList(null, ref nDevices, Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
			Debug.Assert(res == 0);

			RAWINPUTDEVICELIST[] deviceList = new RAWINPUTDEVICELIST[nDevices];

			uint size = nDevices * (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICELIST));
			res = GetRawInputDeviceList(deviceList, ref size, Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)));
			Debug.Assert(res == nDevices);
			return deviceList;
		}

		public const int RIM_TYPEKEYBOARD = 1;

		[DllImport("user32.dll")]
		private static extern int GetRawInputDeviceInfo(IntPtr deviceHandle, uint command, [Out] StringBuilder data, ref uint dataSize);

		public static string GetRawInputDeviceName(IntPtr deviceHandle) {
			uint dataSize = 0;
			int res = GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, null, ref dataSize);
			Debug.Assert(res == 0);
			Debug.Assert(dataSize > 0);

			StringBuilder buffer = new StringBuilder((int)dataSize);
			res = GetRawInputDeviceInfo(deviceHandle, RIDI_DEVICENAME, buffer, ref dataSize);
			Debug.Assert(res > 0);

			return buffer.ToString();
		}

		public const uint RIDI_PREPARSEDDATA = 0x20000005;
		public const uint RIDI_DEVICENAME = 0x20000007;
		public const uint RIDI_DEVICEINFO = 0x2000000b;

		[DllImport("user32.dll", SetLastError = false)]
		private static extern uint GetRawInputData(
			IntPtr hRawInput,
			uint uiCommand,
			IntPtr pData,
			ref int pcbSize,
			int cbSizeHeader
		);

		public static uint GetRawInputData(IntPtr hRawInput, uint uiCommand, out RAWINPUTHEADER data) {
			int size = Marshal.SizeOf(typeof(RAWINPUTHEADER));
			IntPtr buffer = Marshal.AllocHGlobal(size);
			try {
				uint result = GetRawInputData(
					hRawInput,
					uiCommand,
					buffer,
					ref size,
					size
				);

				data = new RAWINPUTHEADER();
				Marshal.PtrToStructure(buffer, data);
				return result;
			}
			finally {
				Marshal.FreeHGlobal(buffer);
			}
		}

		public const uint BSM_ALLCOMPONENTS = 0x00000000;
		public const uint BSM_VXDS = 0x00000001;
		public const uint BSM_NETDRIVER = 0x00000002;
		public const uint BSM_INSTALLABLEDRIVERS = 0x00000004;
		public const uint BSM_APPLICATIONS = 0x00000008;
		public const uint BSM_ALLDESKTOPS = 0x00000010;

		public const uint BSF_QUERY = 0x00000001;
		public const uint BSF_IGNORECURRENTTASK = 0x00000002;
		public const uint BSF_FLUSHDISK = 0x00000004;
		public const uint BSF_NOHANG = 0x00000008;
		public const uint BSF_POSTMESSAGE = 0x00000010;
		public const uint BSF_FORCEIFHUNG = 0x00000020;
		public const uint BSF_NOTIMEOUTIFNOTHUNG = 0x00000040;
		public const uint BSF_ALLOWSFW = 0x00000080;
		public const uint BSF_SENDNOTIFYMESSAGE = 0x00000100;
		public const uint BSF_RETURNHDESK = 0x00000200;
		public const uint BSF_LUID = 0x00000400;

		public const uint WM_INPUTLANGCHANGEREQUEST = 0x0050;

		[DllImport("user32", SetLastError = true)]
		public static extern int BroadcastSystemMessage(uint dwFlags, ref uint lpdwRecipients, uint uiMessage, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

		public static IntPtr[] GetKeyboardLayoutList() {
			int count = (int)GetKeyboardLayoutList(0, null);
			Debug.Assert(count > 0);

			IntPtr[] localeHandles = new IntPtr[count];
			int realCount = (int)GetKeyboardLayoutList(count, localeHandles);
			Debug.Assert(realCount == count);
			return localeHandles;
		}

		public const int SPI_SETDEFAULTINPUTLANG = 90;
		public const int SPIF_SENDCHANGE = 2;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr LoadKeyboardLayout([MarshalAs(UnmanagedType.LPTStr)] string pwszKLID, uint flags);

		public static IntPtr LoadKeyboardLayout(ushort layout, uint flags) {
			return LoadKeyboardLayout(string.Format("{0:X04}{0:X04}", layout), flags);
		}

		[DllImport("user32.dll")]
		public static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr[] pvParam, uint fWinIni);
	}
}