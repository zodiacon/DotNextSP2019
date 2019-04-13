using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLogger.ViewModels {
	class ExceptionViewModel : BindableBase {
		readonly ExceptionTraceData _data;
		public ExceptionViewModel(ExceptionTraceData data) {
			_data = data;
		}

		public int ProcessId => _data.ProcessID;
		public string ProcessName => GetProcessName();

		public string ExceptionType => _data.ExceptionType;
		public ExceptionThrownFlags ExceptionFlags => _data.ExceptionFlags;
		public string ExceptionMessage => _data.ExceptionMessage;
		public uint ExceptionHResult => (uint)_data.ExceptionHRESULT;
		public string TimeAsString => _data.TimeStamp.ToTimeString();
		public int ThreadId => _data.ThreadID;

		private string GetProcessName() {
			if (!string.IsNullOrEmpty(_data.ProcessName))
				return _data.ProcessName;

			try {
				return Process.GetProcessById(ProcessId).ProcessName;
			}
			catch {
				return string.Empty;
			}
		}

		DateTime? _endExceptionTime;
		public DateTime? EndExceptionTime {
			get => _endExceptionTime;
			set {
				if (SetProperty(ref _endExceptionTime, value)) {
					RaisePropertyChanged(nameof(EndExceptionTimeAsString));
					RaisePropertyChanged(nameof(ExceptionHandlingTime));
				}
			}
		}

		public string EndExceptionTimeAsString => EndExceptionTime?.ToTimeString();
		public TimeSpan? ExceptionHandlingTime => EndExceptionTime == null ? default : EndExceptionTime.Value - _data.TimeStamp;
	}
}
