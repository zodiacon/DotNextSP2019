using ExceptionLogger.Models;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace ExceptionLogger.ViewModels {
	class MainViewModel : BindableBase {
		public ObservableCollection<ExceptionViewModel> Exceptions { get; } = new ObservableCollection<ExceptionViewModel>();
		public ObservableCollection<ExceptionCounterViewModel> AggregatedExceptions { get; } = new ObservableCollection<ExceptionCounterViewModel>();

		readonly Dictionary<RunningException, ExceptionViewModel> _runningExceptions = new Dictionary<RunningException, ExceptionViewModel>(64);
		readonly Dictionary<(int pid, string type), ExceptionCounterViewModel> _exceptionAggregator = new Dictionary<(int pid, string type), ExceptionCounterViewModel>(128);

		TraceEventSession _session;
		readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

		public MainViewModel() {
		}

		void EnableLogging(bool enable) {
			if (enable) {
				_session = new TraceEventSession("ExceptionLoggerSession");
				_session.EnableProvider(ClrTraceEventParser.ProviderGuid);
				var parser = _session.Source.Clr;

				parser.ExceptionStart += OnExceptionStart;
				parser.ExceptionStop += OnExceptionStop;
				parser.ExceptionCatchStart += OnExceptionCatchStart;
				var t = new Thread(() => _session.Source.Process());
				t.IsBackground = true;
				t.Start();
			}
			else {
				_session.Dispose();
				_session = null;
			}
		}

		bool _isLogging;
		public bool IsLogging {
			get => _isLogging;
			set {
				if (SetProperty(ref _isLogging, value))
					EnableLogging(value);
			}
		}

		private void OnExceptionStop(EmptyTraceData obj) {
			var clone = (EmptyTraceData)obj.Clone();
			_dispatcher.InvokeAsync(() => {
				var key = new RunningException {
					ProcessId = clone.ProcessID,
					ThreadId = clone.ThreadID
				};
				if (_runningExceptions.TryGetValue(key, out var ex)) {
					ex.EndExceptionTime = clone.TimeStamp;
					_runningExceptions.Remove(key);
				}
			});
		}

		private void OnExceptionStart(ExceptionTraceData obj) {
			var data = (ExceptionTraceData)obj.Clone();
			_dispatcher.InvokeAsync(() => AddException(new ExceptionViewModel(data)));
		}

		private void OnExceptionCatchStart(ExceptionHandlingTraceData obj) {
		}

		void AddException(ExceptionViewModel vm) {
			Exceptions.Add(vm);
			var key = new RunningException { ProcessId = vm.ProcessId, ThreadId = vm.ThreadId };
			if (!_runningExceptions.ContainsKey(key))
				_runningExceptions.Add(key, vm);

			var key2 = (vm.ProcessId, vm.ExceptionType);
			if (_exceptionAggregator.TryGetValue(key2, out var agg)) {
				agg.Count++;
			}
			else {
				var countervm = new ExceptionCounterViewModel(vm);
				_exceptionAggregator.Add(key2, countervm);
				AggregatedExceptions.Add(countervm);
			}
		}

		string _searchText;
		public string SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value)) {
					var view = CollectionViewSource.GetDefaultView(Exceptions);
					if (value == null)
						view.Filter = null;
					else {
						var text = value.ToLower();
						view.Filter = obj => {
							var item = (ExceptionViewModel)obj;
							return item.ProcessName.ToLower().Contains(text) || item.ExceptionType.ToLower().Contains(text);
						};
					}
				}
			}
		}

		public DelegateCommandBase ClearCommand => new DelegateCommand(() => {
			Exceptions.Clear();
			AggregatedExceptions.Clear();
			_runningExceptions.Clear();
			_exceptionAggregator.Clear();
		}, () => !IsLogging).ObservesProperty(() => IsLogging);

	}
}
