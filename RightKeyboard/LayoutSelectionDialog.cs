using System;
using System.ComponentModel;
using System.Windows.Forms;
using RightKeyboard.Win32;
using System.Linq;
using Microsoft.Win32;

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
				string layout_id = item.ToString("X4");

				RegistryKey key = null;

				// If we're using "advanced" keyboard layouts (us-intl, dvorak, etc)
				if (layout_id.First() == 'F')
				{
					string real_layout_id = "0"+layout_id.Substring(1);

					RegistryKey all_layouts = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts");

					// Get layout with layout id search
					foreach (string key_name in all_layouts.GetSubKeyNames())
					{
						key = all_layouts.OpenSubKey(key_name);

						if (key == null) {
							continue;
						}

						if(key.GetValue("Layout Id", "").ToString() == real_layout_id)
                        {
							break;
                        }
					}
				}
				else
				{
					// Get layout name directly
					key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Keyboard Layouts\0000" + layout_id);
				}

				string name;
                if (key == null)
                    name = "Unknown " + layout_id;
                else
                    name = key.GetValue("Layout Text").ToString();

                var layout = new Layout((ushort)item, name);

                // Need full hkl, if there are several for a layout here we just take one,
                // we can improve this by also having the language selection somehow...
                layout.Hkl = installedLayouts.First(h => ((h.ToInt32() >> 16) & 0xffff) == item);
                lbLayouts.Items.Add(layout);
            }
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

        private void LayoutSelectionDialog_VisibleChanged(object sender, EventArgs e)
        {
			lbLayouts.SelectedIndex = -1;
			lbLayouts.Focus();
        }
    }
}