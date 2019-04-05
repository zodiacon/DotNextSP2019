using DebugPrint.Models;
using DebugPrint.ViewModels;
using Filtering.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint.Filters {
	enum DebugItemFilterType {
		ProcessName,
		ProcessId,
		ThreadId,
		Component,
		Text
	}

	sealed class DebugItemGenericFilter : FilterBase<DebugItem> {
		public DebugItemFilterType Type { get; set; }

		public DebugItemGenericFilter(Relation relation, object value, DebugItemFilterType type) {
			Relation = relation;
			TargetValue = value;
			Type = type;
		}

		public override bool Eval(DebugItem context) {
			switch (Type) {
				case DebugItemFilterType.ProcessName:
					return EvalString(context.ProcessName);
				case DebugItemFilterType.ProcessId:
					return EvalInt32(context.ProcessId);
				case DebugItemFilterType.Text:
					return EvalString(context.Text);
				case DebugItemFilterType.Component:
					return EvalInt32(context.Component == null ? 0 : (int)(context.Component));
				case DebugItemFilterType.ThreadId:
					return EvalInt32(context.ThreadId == null ? 0 : (int)(context.ThreadId));
			}
			throw new NotImplementedException();
		}
	}
}
