using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filtering.Core {
	public enum Relation {
		Equals,
		NotEquals,
		GreaterThan,
		LessThan,
		BeginsWith,
		EndsWith,
		Contains,
	}

	public interface IFilter {
		bool Eval(object context);
		Relation Relation { get; set; }
		object TargetValue { get; set; }
	}

	public interface IFilter<TContext> : IFilter where TContext : class {
		bool Eval(TContext context, Action<TContext> action);
	}

}
