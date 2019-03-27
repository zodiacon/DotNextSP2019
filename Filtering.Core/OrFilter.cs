using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filtering.Core {
	public sealed class OrFilter : FilterBase {
		public IFilter LeftFilter { get; set; }
		public IFilter RightFilter { get; set; }

		public OrFilter(IFilter left, IFilter right) {
			LeftFilter = left;
			RightFilter = right;
		}

		public override bool Eval(object context) {
			return LeftFilter.Eval(context) || RightFilter.Eval(context);
		}
	}
}
