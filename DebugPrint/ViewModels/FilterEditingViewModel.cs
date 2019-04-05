using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Zodiacon.WPF;

namespace DebugPrint.ViewModels {
	sealed class FilterEditingViewModel : DialogViewModelBase {
		public FilterEditingViewModel(System.Windows.Window dialog, IEnumerable<FilterViewModel> filters) : base(dialog) {
			Filters = new ObservableCollection<FilterViewModel>(filters);
		}

        public ObservableCollection<FilterViewModel> Filters { get; }

		static string[] _allRelations = { "Equals", "Not Equals", "Contains", "Greater Than", "Less Than", "Starts With", "Ends With" };
		static string[] _allProperties = { "Process Name", "Process Id", "Thread Id", "Component", "Text" };
		static string[] _allActions = { "Include", "Exclude" };

		public string[] AllRelations => _allRelations;
		public string[] AllProperties => _allProperties;
		public string[] AllActions => _allActions;

		FilterViewModel _selectedFilter;
		public FilterViewModel SelectedFilter { get => _selectedFilter; set => SetProperty(ref _selectedFilter, value); }
		int _selectedIndex = -1;
		public int SelectedIndex { get => _selectedIndex; set => SetProperty(ref _selectedIndex, value); }
		public DelegateCommandBase NewFilterCommand => new DelegateCommand(() => {
			var filter = new FilterViewModel();
			Filters.Add(filter);
			SelectedFilter = filter;
		});

		public DelegateCommandBase DeleteFilterCommand => new DelegateCommand(() => {
			Debug.Assert(SelectedFilter != null);
			Filters.Remove(SelectedFilter);
		}, () => SelectedFilter != null).ObservesProperty(() => SelectedFilter);

		public DelegateCommandBase ClearAllCommand => new DelegateCommand(() => Filters.Clear());
		public DelegateCommandBase MoveUpCommand => new DelegateCommand(() => {
			var index = SelectedIndex;
			var f1 = Filters[index];
			Filters[index] = Filters[index - 1];
			Filters[index - 1] = f1;
			SelectedFilter = f1;
		}, () => SelectedIndex > 0).ObservesProperty(() => SelectedIndex);

		public DelegateCommandBase MoveDownCommand => new DelegateCommand(() => {
			var index = SelectedIndex;
			var f1 = Filters[index];
			Filters[index] = Filters[index + 1];
			Filters[index + 1] = f1;
			SelectedFilter = f1;
		}, () => SelectedIndex >= 0 && SelectedIndex < Filters.Count - 1).ObservesProperty(() => SelectedIndex);

	}
}
