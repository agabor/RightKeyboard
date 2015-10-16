using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace RightKeyboard {
	/// <summary>
	/// Represents a keyboard layout
	/// </summary>
	public class Layout {
		private readonly ushort identifier;

		/// <summary>
		/// Gets the layout's identifier
		/// </summary>
		public ushort Identifier {
			get {
				return identifier;
			}
		}

		private readonly string name;

		/// <summary>
		/// Gets the layout's name
		/// </summary>
		public string Name {
			get {
				return name;
			}
		}

		/// <summary>
		/// Initializes a new instance of Layout
		/// </summary>
		/// <param name="identifier"></param>
		/// <param name="name"></param>
		public Layout(ushort identifier, string name) {
			this.identifier = identifier;
			this.name = name;
		}

		public override string ToString() {
			return name;
		}

		private static Layout[] cachedLayouts = null;

		/// <summary>
		/// Gets the keyboard layouts from a ressource file
		/// </summary>
		/// <returns></returns>
		public static Layout[] GetLayouts() {
			if(cachedLayouts == null) {
				List<Layout> layouts = new List<Layout>();
				using(Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("RightKeyboard.Layouts.txt")) {
					using(TextReader reader = new StreamReader(input)) {
						string line;
						while((line = reader.ReadLine()) != null) {
							layouts.Add(GetLayout(line));
						}
					}
				}
				cachedLayouts = layouts.ToArray();
			}
			return cachedLayouts;
		}

		private static Layout GetLayout(string line) {
			string[] parts = line.Trim().Split('=');

			ushort identifier = ushort.Parse(parts[0], NumberStyles.HexNumber);
			string name = parts[1];
			return new Layout(identifier, name);
		}
	}
}