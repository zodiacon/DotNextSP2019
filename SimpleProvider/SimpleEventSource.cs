using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleProvider {
	[EventSource(Name = "DotNextSimpleEventSource")]
	class SimpleEventSource : EventSource {
		public void Event1(int value, string text) {
			WriteEvent(1, value, text);
		}

		[Event(2, Level = EventLevel.Informational)]
		public void Event2(string text) {
			WriteEvent(2, text);
		}

		[Event(99, Level = EventLevel.Warning)]
		public void EventStop() {
			WriteEvent(99);
		}

		public static readonly SimpleEventSource Log = new SimpleEventSource();
	}
}
