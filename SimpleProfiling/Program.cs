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
			var samples = new List<SampledProfileTraceData>(100000);
			var d = new Dictionary<(int, string), int>(500);

			Console.WriteLine("Press ENTER to start");
			Console.ReadLine();
			Console.WriteLine("Profiling for 10 seconds...");
			using (var session = new TraceEventSession(KernelTraceEventParser.KernelSessionName)) {
				session.EnableKernelProvider(KernelTraceEventParser.Keywords.Profile);
				var parser = session.Source.Kernel;

				parser.PerfInfoSample += sample => {
					samples.Add((SampledProfileTraceData)sample.Clone());
					var name = sample.ProcessName;
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
					if (d.ContainsKey(key))
						d[key]++;
					else
						d.Add(key, 1);
				};

				parser.PerfInfoSetInterval += e => Console.WriteLine($"New interval: {e.NewInterval}");
				parser.LostEvent += e => Console.WriteLine("Event lost");
				Task.Run(() => session.Source.Process());
				Thread.Sleep(10000);
			}

			Console.WriteLine($"Analyzing {samples.Count} events");
			foreach (var item in d.OrderByDescending(pair => pair.Value).Take(10))
				Console.WriteLine($"{item}: {item.Value * 100.0f / samples.Count:N2} %");
		}
	}
}
