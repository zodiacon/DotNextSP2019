using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugPrint.ViewModels {
	class FilterViewModel : BindableBase {
		string _property = "Process Name", _value, _relation = "Contains", _action = "Include";

		public string Property { get => _property; set => SetProperty(ref  _property, value); }
		public string Relation { get => _relation; set => SetProperty(ref _relation, value); }
		public string Value { get => _value; set => SetProperty(ref _value, value); }
		public string Action { get => _action; set => SetProperty(ref _action, value); }
	}
}
