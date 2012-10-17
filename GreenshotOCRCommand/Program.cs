using System;
using System.IO;
using Greenshot.Interop;
using GreenshotOCR;

namespace GreenshotOCRCommand {
	public class Program {
		private const string USAGE = "<-c> | <path to image.bmp> [language] [orientimage] [straightenImage]";
		public static int Main(string[] args) {
			// to test
			//args = new string[] { @"C:\localdata\test.bmp"};
			if (args.Length == 0) {
				Console.WriteLine(USAGE);
				return -1;
			}
			string filename = args[0];
			ModiLanguage language = ModiLanguage.ENGLISH;
			if (args.Length >= 2) {
				language = (ModiLanguage)Enum.Parse(typeof(ModiLanguage), args[1]);
			}
			bool orientimage = true;
			if (args.Length >= 3) {
				orientimage = bool.Parse(args[2]);
			}
			bool straightenImage = true;
			if (args.Length >= 4) {
				straightenImage = bool.Parse(args[3]);
			}
			try {
				if (File.Exists(filename) || "-c".Equals(filename)) {
					using (ModiDocu modiDocument = COMWrapper.GetOrCreateInstance<ModiDocu>()) {
						if (modiDocument == null) {
							Console.WriteLine("MODI not installed");
							return -2;
						}
						if ("-c".Equals(filename)) {
							return 0;
						}
						modiDocument.Create(filename);
						modiDocument.OCR(language, orientimage, straightenImage);
						IImage modiImage = modiDocument.Images[0];
						ILayout layout = modiImage.Layout;
						Console.WriteLine(layout.Text);
						modiDocument.Close(false);
						return 0;
					}
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
			}
			return -1;
		}
	}
}
