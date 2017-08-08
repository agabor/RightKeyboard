using System;
using System.ComponentModel;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Linq;

namespace RightKeyboard {
	public partial class LayoutSelectionDialog : Form {
		public LayoutSelectionDialog() {
			InitializeComponent();

			LoadLanguageList();
		}

		private void LoadLanguageList() {
			lbLayouts.Items.Clear();
			recentLayoutsCount = 0;

			IntPtr[] installedLayouts = API.GetKeyboardLayoutList();

            var layoutCodes = installedLayouts.Select(l => ((l.ToInt32() >> 16)& 0xffff)).Distinct();

            foreach (var item in layoutCodes)
            {
                // Get layout name
                var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\0000" +
                    item.ToString("X4"));

                string name;
                if (key == null)
                    name = "Unknown " + item.ToString("X4");
                else
                    name = key.GetValue("Layout Text").ToString();

                var layout = new Layout((ushort)item, name);
                // Need full hkl, if there are several for a layout here we just take one,
                // we can improve this by also having the language selection somehow...
                layout.Hkl = installedLayouts.First(h => ((h.ToInt32() >> 16) & 0xffff) == item);
                lbLayouts.Items.Add(layout);
            }
      // var layouts = Layout.GetLayouts();

            //         foreach (IntPtr installedLayout in installedLayouts)
            //         {
            //             ushort languageId = unchecked((ushort)installedLayout.ToInt32());

            //             var layout = layouts.FirstOrDefault(l => l.Identifier == languageId);

            //             if (layout != null)
            //             {
            //                 layout.Hkl = installedLayout;
            //                 lbLayouts.Items.Add(layout);
            //             }
            //         }

            //lbLayouts.SelectedIndex = 0;
        }

		private int recentLayoutsCount = 0;
		private Layout selectedLayout;
		private bool okPressed = false;

		public new Layout Layout {
			get {
				return selectedLayout;
			}
		}

		private void btOk_Click(object sender, EventArgs e) {
			selectedLayout = (Layout)lbLayouts.SelectedItem;
			okPressed = true;
			Close();
		}

		private void lbLayouts_SelectedIndexChanged(object sender, EventArgs e) {
			btOk.Enabled = lbLayouts.SelectedIndex != recentLayoutsCount || recentLayoutsCount == 0;
		}

		private void lbLayouts_DoubleClick(object sender, EventArgs e) {
			if(btOk.Enabled) {
				btOk_Click(this, EventArgs.Empty);
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			e.Cancel = !okPressed;
			okPressed = false;
			base.OnClosing(e);
		}
	}
}