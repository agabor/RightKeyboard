using System;
using System.Globalization;

namespace RightKeyboard
{
    /// <summary>
    /// Represents a keyboard layout
    /// </summary>
    public class Layout {

        /// <summary>
        /// Gets the layout's identifier
        /// </summary>
        public ushort Identifier { get; }

        /// <summary>
        /// Gets the layout's name
        /// </summary>
        public string Name { get; }

        public IntPtr Hkl { get; set; }

        /// <summary>
        /// Initializes a new instance of Layout
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        public Layout(ushort identifier, string name) {
			this.Identifier = identifier;
			Name = name;
		}

		public override string ToString() {
			return Name + " (" + Hkl.ToString("X8") + ")";
		}
	}
}