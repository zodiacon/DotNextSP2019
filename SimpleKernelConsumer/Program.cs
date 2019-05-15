using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleKernelConsumer {
	class ProcessInfo {
		public int Id { get; set; }
		public string Name { get; set; }
	}

	class Program {
		static void Main(string[] args) {
			var processes = Process.GetProcesses().Select(p => new ProcessInfo {
				Name = p.ProcessName,
				Id = p.Id
			}).ToDictionary(p => p.Id);

			using (var session = new TraceEventSession(Environment.OSVersion.Version.Build >= 9200 ? "MyKernelSession" : KernelTraceEventParser.KernelSessionName)) {
				session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process | KernelTraceEventParser.Keywords.ImageLoad);
				var parser = session.Source.Kernel;

				parser.ProcessStart += e => {
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Process {e.ProcessID} ({e.ProcessName}) Created by {e.ParentID}: {e.CommandLine}");
					processes.Add(e.ProcessID, new ProcessInfo { Id = e.ProcessID, Name = e.ProcessName });
				};
				parser.ProcessStop += e => {
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Process {e.ProcessID} ({TryGetProcessName(e.ProcessID)}) Exited");
				};

				parser.ImageLoad += e => {
					Console.ForegroundColor = ConsoleColor.Yellow;
					var name = string.IsNullOrEmpty(e.ProcessName) ? processes[e.ProcessID].Name : e.ProcessName;
					Console.WriteLine($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Image Loaded: {e.FileName} into process {e.ProcessID} ({name}) Size=0x{e.ImageSize:X}");
				};

				parser.ImageUnload += e => {
					Console.ForegroundColor = ConsoleColor.DarkYellow;
					var name = string.IsNullOrEmpty(e.ProcessName) ? TryGetProcessName(e.ProcessID) : e.ProcessName;
					Console.WriteLine($"{e.TimeStamp}.{e.TimeStamp.Millisecond:D3}: Image Unloaded: {e.FileName} from process {e.ProcessID} ({name})");
				};

				Task.Run(() => session.Source.Process());
				Thread.Sleep(TimeSpan.FromSeconds(60));
			}

			string TryGetProcessName(int pid) {
				return processes.TryGetValue(pid, out var info) ? info.Name : string.Empty;
			}

		}

		}
}
