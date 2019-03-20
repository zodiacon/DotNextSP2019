using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace DebugPrint.Behaviors {
	sealed class SelectedItemsBehavior : Behavior<DataGrid> {
		protected override void OnAttached() {
			base.OnAttached();

			AssociatedObject.SelectionChanged += OnSelectionChanged;
		}

		protected override void OnDetaching() {
			AssociatedObject.SelectionChanged -= OnSelectionChanged;
			base.OnDetaching();
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
			SelectedItems = AssociatedObject.SelectedItems;
		}

		public IList SelectedItems {
			get { return (IList)GetValue(SelectedItemsProperty); }
			set { SetValue(SelectedItemsProperty, value); }
		}

		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(SelectedItemsBehavior), new PropertyMetadata(null));


	}
}
