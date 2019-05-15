using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleProfiling {
	class Program {
		static void Main(string[] args) {
			int samples = 0;
			var samplesPerProcess = new Dictionary<(int, string), int>(500);

			Console.WriteLine("Press ENTER to start");
			Console.ReadLine();
			Console.WriteLine("Profiling for 10 seconds...");

			using (var session = new TraceEventSession(KernelTraceEventParser.KernelSessionName)) {
				session.EnableKernelProvider(KernelTraceEventParser.Keywords.Profile);
				var parser = session.Source.Kernel;

				parser.PerfInfoSample += sample => {
					var name = sample.ProcessName;
					samples++;
					if (sample.ProcessID < 0) {
						if (sample.NonProcess)
							name = "(DPC/ISR)";
					}
					try {
						if (string.IsNullOrEmpty(name) && sample.ProcessID >= 0)
							name = Process.GetProcessById(sample.ProcessID)?.ProcessName;
					}
					catch {
						name = "<Unknown>";
					}
					var key = (sample.ProcessID, name);
					if (samplesPerProcess.ContainsKey(key))
						samplesPerProcess[key]++;
					else
						samplesPerProcess.Add(key, 1);
				};

				parser.PerfInfoSetInterval += e => Console.WriteLine($"New interval: {e.NewInterval}");
				parser.LostEvent += e => Console.WriteLine("Event lost");
				Task.Run(() => session.Source.Process());
				Thread.Sleep(10000);
			}

			Console.WriteLine($"Analyzing {samples} samples");
			foreach (var item in samplesPerProcess.OrderByDescending(pair => pair.Value).TakeWhile(pair => pair.Value * 100.0f / samples >= 1))
				Console.WriteLine($"{item}: {item.Value * 100.0f / samples:N2} %");
		}
	}
}
