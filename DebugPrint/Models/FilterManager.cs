using Filtering.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint.Models {
	class FilterManager {
		public List<IFilter> Filters { get; } = new List<IFilter>(4);


	}
}
