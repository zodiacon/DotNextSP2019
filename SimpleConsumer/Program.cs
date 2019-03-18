using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleConsumer {
	static class Program {
		static void Main(string[] args) {
			using (var session = new TraceEventSession("MySimpleSession")) {
				Console.CancelKeyPress += delegate {
					session.Source.StopProcessing();
					session.Dispose();
				};

				session.EnableProvider("DotNextSimpleEventSource", Microsoft.Diagnostics.Tracing.TraceEventLevel.Verbose);
				var parser = session.Source.Dynamic;
				parser.All += e => {
					Console.WriteLine($"{e.TimeStamp} ID={e.ID} PID={e.ProcessID} TID={e.ThreadID} Level={e.Level}");
					Console.Write("\t");
					for (int i = 0; i < e.PayloadNames.Length; i++)
						Console.Write($"{e.PayloadValue(i)} ");
					Console.WriteLine();
				};
				session.Source.Process();
			}
		}
	}
}
