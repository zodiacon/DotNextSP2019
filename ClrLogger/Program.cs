using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ClrLogger {
	class Context {
		public int ProcessId;
		readonly Dictionary<string, int> _exceptions = new Dictionary<string, int>(16);

		public IReadOnlyDictionary<string, int> Exceptions => _exceptions;

		public void AddException(string type) {
			if (_exceptions.ContainsKey(type))
				_exceptions[type]++;
			else
				_exceptions.Add(type, 1);
		}
	}

	static class Program {
		static Context context;

		static void Main(string[] args) {
			var pid = args.Length == 0 ? -1 : int.Parse(args[0]);
			context = new Context {
				ProcessId = pid
			};

			using (var session = new TraceEventSession("MyClrSession")) {
				Console.CancelKeyPress += (s, e) => {
					e.Cancel = true;
					session.Source.StopProcessing();
				};

				session.EnableProvider(ClrTraceEventParser.ProviderGuid);
				var parser = session.Source.Clr;
				parser.GCStart += OnGCStart;
				parser.GCStop += OnGCStop;
				parser.ExceptionStart += OnExceptionStart;
				parser.ExceptionStop += OnExceptionEnd;

				var t = new Thread(() => session.Source.Process());
				t.Start();
				t.Join();
			}

			Console.WriteLine("Exceptions:");
			foreach(var pair in context.Exceptions.OrderByDescending(p => p.Value).Take(10))
				Console.WriteLine(pair);
		}

		private static void OnExceptionEnd(EmptyTraceData obj) {
		}

		static bool IgnoreProcess(TraceEvent obj) {
			return context.ProcessId > 0 && obj.ProcessID != context.ProcessId;
		}

		private static void OnExceptionStart(ExceptionTraceData obj) {
			if (IgnoreProcess(obj))
				return;

			Console.WriteLine($"Exception thrown (PID:{obj.ProcessID}) ({GetProcessName(obj)}): Type: {obj.ExceptionType} : {obj.ExceptionMessage}");
			context.AddException(obj.ExceptionType);
		}

		private static string GetProcessName(TraceEvent obj) {
			if (!string.IsNullOrEmpty(obj.ProcessName))
				return obj.ProcessName;

			try {
				return Process.GetProcessById(obj.ProcessID).ProcessName;
			}
			catch {
				return string.Empty;
			}
		}

		private static void OnGCStop(GCEndTraceData obj) {
			if (IgnoreProcess(obj))
				return;
			Console.WriteLine($"GC End PID={obj.ProcessID} {GetProcessName(obj)}");
		}

		private static void OnGCStart(GCStartTraceData obj) {
			if (IgnoreProcess(obj))
				return;
			Console.WriteLine($"GC Start PID={obj.ProcessID} ({GetProcessName(obj)}) reason: {obj.Reason}");
		}
	}
}
