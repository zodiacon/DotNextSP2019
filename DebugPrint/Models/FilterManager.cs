using Filtering.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint.Models {
	enum FilterResult {
		Include,
		Exclude,
	}

	class FilterManager {
		public List<(IFilter filter, FilterResult result)> Filters { get; } = new List<(IFilter, FilterResult)>(4);


	}
}
