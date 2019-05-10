using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLogger.ViewModels {
	sealed class CallStackEntryViewModel {
		public int Index { get; set; }
		public int Depth { get; set; }
		public string MethodName { get; set; }
		public ulong Address { get; set; }
		public string ModuleName { get; set; }
	}
}
