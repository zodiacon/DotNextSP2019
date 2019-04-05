using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filtering.Core {
	public abstract class FilterBase : IFilter {
		public Relation Relation { get; set; }
		public object TargetValue { get; set; }

		public abstract bool Eval(object context);

		protected virtual bool EvalString(string value) {
			if (TargetValue == null)
				return false;

			switch (Relation) {
				case Relation.Equals:
					return value.Equals(TargetValue.ToString(), StringComparison.InvariantCultureIgnoreCase);
				case Relation.NotEquals:
					return !value.Equals(TargetValue.ToString(), StringComparison.InvariantCultureIgnoreCase);
				case Relation.Contains:
					return value.IndexOf(TargetValue.ToString(), StringComparison.CurrentCultureIgnoreCase) >= 0;
			}

			return false;
		}

		protected virtual bool EvalInt32(int value) {
			var num = Convert.ToInt32(TargetValue);
			switch (Relation) {
				case Relation.Equals:
					return value == num;
				case Relation.NotEquals:
					return value != num;
				case Relation.LessThan:
					return num < value;
				case Relation.GreaterThan:
					return num > value;
			}
			return false;
		}
	}

	public abstract class FilterBase<TContext> : FilterBase where TContext : class {

		public abstract bool Eval(TContext context);

		public override bool Eval(object context) {
			return Eval(context as TContext);
		}

	}
}
