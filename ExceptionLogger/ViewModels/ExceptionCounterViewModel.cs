using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLogger.ViewModels {
	class ExceptionCounterViewModel : BindableBase {
		int _count = 1;
		public int Count { get => _count; set => SetProperty(ref _count, value); }

		ExceptionViewModel _ex;
		public ExceptionCounterViewModel(ExceptionViewModel ex) {
			_ex = ex;
		}

		public int ProcessId => _ex.ProcessId;
		public string ProcessName => _ex.ProcessName;
		public string ExceptionType => _ex.ExceptionType;
	}
}
