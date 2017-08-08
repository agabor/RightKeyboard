using System;
using System.Collections.Generic;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RightKeyboard
{
    public partial class MainForm : Form
    {
        private IntPtr hCurrentDevice = IntPtr.Zero;

        private bool selectingLayout = false;
        private IntPtr currentLayoutHkl;
        private readonly LayoutSelectionDialog layoutSelectionDialog = new LayoutSelectionDialog();

        private readonly Dictionary<IntPtr, Layout> languageMappings = new Dictionary<IntPtr, Layout>();

        private readonly Dictionary<string, IntPtr> devicesByName = new Dictionary<string, IntPtr>();

        public MainForm()
        {
            InitializeComponent();

            RAWINPUTDEVICE rawInputDevice = new RAWINPUTDEVICE(1, 6, API.RIDEV_INPUTSINK, this);
            bool ok = API.RegisterRawInputDevices(rawInputDevice);
            if (!ok)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            Debug.Assert(ok);

            WindowState = FormWindowState.Minimized;

            LoadDeviceList();
            LoadConfiguration();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            SaveConfiguration();
        }

        private void SaveConfiguration()
        {
            try
            {
                var mappings = new List<string>();
                foreach (KeyValuePair<string, IntPtr> entry in devicesByName)
                {
                    if (languageMappings.TryGetValue(entry.Value, out Layout layout))
                        mappings.Add(string.Format("{0}={1}", entry.Key, layout.Hkl));
                }
                Properties.Settings.Default.Mappings = string.Join(";", mappings);
                Properties.Settings.Default.Save();
            }
            catch (Exception err)
            {
                MessageBox.Show("Could not save the configuration. Reason: " + err.Message);
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.Mappings))
                    return;

                foreach (var line in Properties.Settings.Default.Mappings.Split(';'))
                {
                    string[] parts = line.Split('=');
                    Debug.Assert(parts.Length == 2);

                    string deviceName = parts[0];
                    var hkl = (IntPtr)Int32.Parse(parts[1]);

                    if (devicesByName.TryGetValue(deviceName, out IntPtr deviceHandle))
                    {
                        var l = new Layout((ushort)hkl, deviceName);
                        l.Hkl = hkl;
                        languageMappings.Add(deviceHandle, l); 

                    }
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Could not load the configuration. Reason: " + err.Message);
            }
        }

        private void LoadDeviceList()
        {
            foreach (API.RAWINPUTDEVICELIST rawInputDevice in API.GetRawInputDeviceList())
            {
                if (rawInputDevice.dwType == API.RIM_TYPEKEYBOARD)
                {
                    IntPtr deviceHandle = rawInputDevice.hDevice;
                    string deviceName = API.GetRawInputDeviceName(deviceHandle);
                    devicesByName.Add(deviceName, deviceHandle);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Hide();
        }

        protected override void WndProc(ref Message message)
        {
            switch (message.Msg)
            {
                case API.WM_INPUT:
                    if (!selectingLayout)
                    {
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

        private void ProcessPowerMessage(Message message)
        {
            switch (message.WParam.ToInt32())
            {
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

        private void ProcessInputMessage(Message message)
        {
            RAWINPUTHEADER header;
            uint result = API.GetRawInputData(message.LParam, API.RID_HEADER, out header);
            Debug.Assert(result != uint.MaxValue);
            if (result == uint.MaxValue)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            if (header.hDevice != hCurrentDevice)
            {
                hCurrentDevice = header.hDevice;
                CurrentDeviceChanged(hCurrentDevice);
            }
        }

        private void CurrentDeviceChanged(IntPtr currentDevice)
        {
            Layout layout;
            if (!languageMappings.TryGetValue(currentDevice, out layout))
            {
                selectingLayout = true;
                layoutSelectionDialog.ShowDialog();
                selectingLayout = false;
                layout = layoutSelectionDialog.Layout;
                languageMappings.Add(currentDevice, layout);
            }
            SetCurrentLayout(layout.Hkl);
            SetDefaultLayout(layout.Hkl);
        }

        private void SetCurrentLayout(IntPtr layout)
        {
            if (layout != currentLayoutHkl && layout != IntPtr.Zero)
            {
                currentLayoutHkl = layout;
                uint recipients = API.BSM_APPLICATIONS;
                API.BroadcastSystemMessage(API.BSF_POSTMESSAGE, ref recipients, API.WM_INPUTLANGCHANGEREQUEST, IntPtr.Zero, layout);
            }
        }

        private void SetDefaultLayout(IntPtr hkl)
        {
            //IntPtr hkl = new IntPtr(unchecked((int)((uint)layout << 16 | (uint)layout)));

            bool ok = API.SystemParametersInfo(API.SPI_SETDEFAULTINPUTLANG, 0, new[] { hkl }, API.SPIF_SENDCHANGE);
            uint er = API.GetLastError();
            Debug.Assert(ok);
        }
        //private void SetDefaultLayout(ushort layout)
        //{
        //    IntPtr hkl = new IntPtr(unchecked((int)((uint)layout << 16 | (uint)layout)));

        //    bool ok = API.SystemParametersInfo(API.SPI_SETDEFAULTINPUTLANG, 0, new[] { hkl }, API.SPIF_SENDCHANGE);
        //    uint er = API.GetLastError();
        //    Debug.Assert(ok);
        //}


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}