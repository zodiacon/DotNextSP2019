using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint {
	static class Extensions {
		public static string ToEnum(this string s) {
			var words = s.Split(' ');
			if (words.Length == 1)
				return s;
			return string.Join(string.Empty, words);
		}
	}
}
