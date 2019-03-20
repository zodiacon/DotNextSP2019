using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint.ViewModels {
	sealed class DebugItem {
		public bool IsKernel { get; set; }
		public string KernelOrUser => IsKernel ? "Kernel" : "User";
		public string ProcessName { get; set; }
		public int ProcessId { get; set; }
		public int? ThreadId { get; set; }
		public string Text { get; set; }
		public DateTime Time { get; set; }
		public uint? Component { get; set; }
		public string ComponentAsString => Component == null ? string.Empty : ("0x" + Component.ToString());
		public string TimeAsString => Time.ToString("G") + "." + Time.Millisecond.ToString("D3");

		public string ToString(string sep) => $"{TimeAsString}{sep}{KernelOrUser}{sep}{ProcessId}{sep}{ProcessName}{sep}{ThreadId}{sep}{ComponentAsString}{sep}{Text}";

		public override string ToString() => ToString(" ");
	}
}

