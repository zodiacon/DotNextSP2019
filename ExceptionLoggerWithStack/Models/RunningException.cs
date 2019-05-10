using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExceptionLogger.Models {
	struct RunningException : IEquatable<RunningException> {
		public int ProcessId;
		public int ThreadId;

		public override bool Equals(object obj) => Equals((RunningException)obj);

		public bool Equals(RunningException other) => ProcessId == other.ProcessId && ThreadId == other.ThreadId;

		public override int GetHashCode() => ProcessId ^ ThreadId;
	}
}
