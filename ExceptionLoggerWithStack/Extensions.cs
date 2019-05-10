using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLogger {
	static class Extensions {
		public static string ToTimeString(this DateTime dt) => dt.ToString("G") + "." + dt.Millisecond.ToString("D3");
	}
}
