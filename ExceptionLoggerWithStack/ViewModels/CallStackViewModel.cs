using Microsoft.Diagnostics.Tracing.Etlx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace ExceptionLogger.ViewModels {
	sealed class CallStackViewModel {
		readonly TraceCallStack _callStack;
		readonly ExceptionViewModel _ex;

		public CallStackViewModel(TraceCallStack callStack, ExceptionViewModel ex) {
			_callStack = callStack;
			_ex = ex;
		}

		public IEnumerable<CallStackEntryViewModel> CallStack {
			get {
				var stack = _callStack;
				while (stack != null) {
					yield return new CallStackEntryViewModel {
						Index = (int)stack.CallStackIndex,
						Address = stack.CodeAddress.Address,
						MethodName = stack.CodeAddress.FullMethodName,
						ModuleName = stack.CodeAddress.ModuleName,
						Depth = stack.Depth,

					};
					stack = stack.Caller;
				}
			}
		}

		public string Title => $"Call Stack Time:{_ex.TimeAsString} PID:{_ex.ProcessId} TID:{_ex.ThreadId} {_ex.ExceptionType}";
	}
}
