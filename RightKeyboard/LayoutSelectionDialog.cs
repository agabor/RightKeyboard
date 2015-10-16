using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RightKeyboard.Win32;

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

			foreach(Layout layout in RightKeyboard.Layout.GetLayouts()) {
				foreach(IntPtr installedLayout in installedLayouts) {
					ushort languageId = unchecked((ushort)installedLayout.ToInt32());
					if(layout.Identifier == languageId) {
						lbLayouts.Items.Add(layout);
					}
				}
			}

			lbLayouts.SelectedIndex = 0;
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