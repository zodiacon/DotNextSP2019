using NetworkMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkMonitor.Models {
	public class Settings {
		public bool AlwaysOnTop { get; set; }
		public string AccentColor { get; set; }
		public string Theme { get; set; }
		public ConnectionType MonitoredConnections { get; set; }
	}
}
