using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Globalization;

namespace RightKeyboard {
	public partial class MainForm : Form {
		private IntPtr hCurrentDevice = IntPtr.Zero;

		private bool selectingLayout = false;
		private ushort currentLayout;
		private readonly LayoutSelectionDialog layoutSelectionDialog = new LayoutSelectionDialog();

		private readonly Dictionary<IntPtr, ushort> languageMappings = new Dictionary<IntPtr, ushort>();

		private readonly Dictionary<string, IntPtr> devicesByName = new Dictionary<string, IntPtr>();

		public MainForm() {
			InitializeComponent();

			RAWINPUTDEVICE rawInputDevice = new RAWINPUTDEVICE(1, 6, API.RIDEV_INPUTSINK, this);
			bool ok = API.RegisterRawInputDevices(rawInputDevice);
			if(!ok) {
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}
			Debug.Assert(ok);

			WindowState = FormWindowState.Minimized;

			LoadDeviceList();
			LoadConfiguration();
		}

		protected override void OnClosed(EventArgs e) {
			base.OnClosed(e);
			SaveConfiguration();
		}

		private void SaveConfiguration() {
			try {
				string configFilePath = GetConfigFilePath();
				using(TextWriter output = File.CreateText(configFilePath)) {
					foreach(KeyValuePair<string, IntPtr> entry in devicesByName) {
						ushort layout;
						if(languageMappings.TryGetValue(entry.Value, out layout)) {
							output.WriteLine("{0}={1:X04}", entry.Key, layout);
						}
					}
				}
			}
			catch(Exception err) {
				MessageBox.Show("Could not save the configuration. Reason: " + err.Message);
			}
		}

		private void LoadConfiguration() {
			try {
				string configFilePath = GetConfigFilePath();
				if(File.Exists(configFilePath)) {
					using(TextReader input = File.OpenText(configFilePath)) {
						string line;
						while((line = input.ReadLine()) != null) {
							string[] parts = line.Split('=');
							Debug.Assert(parts.Length == 2);

							string deviceName = parts[0];
							ushort layout = ushort.Parse(parts[1], NumberStyles.HexNumber);

							IntPtr deviceHandle;
							if(devicesByName.TryGetValue(deviceName, out deviceHandle)) {
								languageMappings.Add(deviceHandle, layout);
							}
						}
					}
				}
			}
			catch(Exception err) {
				MessageBox.Show("Could not load the configuration. Reason: " + err.Message);
			}
		}

		private static string GetConfigFilePath() {
			string configFileDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RightKeyboard");
			if(!Directory.Exists(configFileDir)) {
				Directory.CreateDirectory(configFileDir);
			}

			return Path.Combine(configFileDir, "config.txt");
		}

		private void LoadDeviceList() {
			foreach(API.RAWINPUTDEVICELIST rawInputDevice in API.GetRawInputDeviceList()) {
				if(rawInputDevice.dwType == API.RIM_TYPEKEYBOARD) {
					IntPtr deviceHandle = rawInputDevice.hDevice;
					string deviceName = API.GetRawInputDeviceName(deviceHandle);
					devicesByName.Add(deviceName, deviceHandle);
				}
			}
		}

		protected override void OnLoad(EventArgs e) {
			base.OnLoad(e);
			Hide();
		}

		protected override void WndProc(ref Message message) {
			switch(message.Msg) {
				case API.WM_INPUT:
					if(!selectingLayout) {
						ProcessInputMessage(message);
					}
					break;

				case API.WM_POWERBROADCAST:
					ProcessPowerMessage(message);
					break;

				default:
					base.WndProc(ref message);
					break;
			}
		}

		private void ProcessPowerMessage(Message message) {
			switch(message.WParam.ToInt32()) {
				case API.PBT_APMQUERYSUSPEND:
					Debug.WriteLine("PBT_APMQUERYSUSPEND");
					break;

				case API.PBT_APMQUERYSTANDBY:
					Debug.WriteLine("PBT_APMQUERYSTANDBY");
					break;

				case API.PBT_APMQUERYSUSPENDFAILED:
					Debug.WriteLine("PBT_APMQUERYSUSPENDFAILED");
					break;

				case API.PBT_APMQUERYSTANDBYFAILED:
					Debug.WriteLine("PBT_APMQUERYSTANDBYFAILED");
					break;

				case API.PBT_APMSUSPEND:
					Debug.WriteLine("PBT_APMSUSPEND");
					break;

				case API.PBT_APMSTANDBY:
					Debug.WriteLine("PBT_APMSTANDBY");
					break;

				case API.PBT_APMRESUMECRITICAL:
					Debug.WriteLine("PBT_APMRESUMECRITICAL");
					break;

				case API.PBT_APMRESUMESUSPEND:
					Debug.WriteLine("PBT_APMRESUMESUSPEND");
					break;

				case API.PBT_APMRESUMESTANDBY:
					Debug.WriteLine("PBT_APMRESUMESTANDBY");
					break;

				case API.PBT_APMBATTERYLOW:
					Debug.WriteLine("PBT_APMBATTERYLOW");
					break;

				case API.PBT_APMPOWERSTATUSCHANGE:
					Debug.WriteLine("PBT_APMPOWERSTATUSCHANGE");
					break;

				case API.PBT_APMOEMEVENT:
					Debug.WriteLine("PBT_APMOEMEVENT");
					break;

				case API.PBT_APMRESUMEAUTOMATIC:
					Debug.WriteLine("PBT_APMRESUMEAUTOMATIC");
					break;
			}
		}

		private void ProcessInputMessage(Message message) {
			RAWINPUTHEADER header;
			uint result = API.GetRawInputData(message.LParam, API.RID_HEADER, out header);
			Debug.Assert(result != uint.MaxValue);
			if(result == uint.MaxValue) {
				throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
			}

			if(header.hDevice != hCurrentDevice) {
				hCurrentDevice = header.hDevice;
				CurrentDeviceChanged(hCurrentDevice);
			}
		}

		private void CurrentDeviceChanged(IntPtr currentDevice) {
			ushort layout;
			if(!languageMappings.TryGetValue(currentDevice, out layout)) {
				selectingLayout = true;
				layoutSelectionDialog.ShowDialog();
				selectingLayout = false;
				layout = layoutSelectionDialog.Layout.Identifier;
				languageMappings.Add(currentDevice, layout);
			}
			SetCurrentLayout(layout);
			SetDefaultLayout(layout);
		}

		private void SetCurrentLayout(ushort layout) {
			if(layout != currentLayout && layout != 0) {
				currentLayout = layout;
				uint recipients = API.BSM_APPLICATIONS;
				API.BroadcastSystemMessage(API.BSF_POSTMESSAGE, ref recipients, API.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, new IntPtr(layout));
			}
		}

		private void SetDefaultLayout(ushort layout) {
			IntPtr hkl = new IntPtr(unchecked((int)((uint)layout << 16 | (uint)layout)));

			bool ok = API.SystemParametersInfo(API.SPI_SETDEFAULTINPUTLANG, 0, new[] { hkl }, API.SPIF_SENDCHANGE);
			Debug.Assert(ok);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			Close();
		}
	}
}