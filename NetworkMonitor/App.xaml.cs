using NetworkMonitor.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NetworkMonitor {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		MainViewModel _mainViewModel;

		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			_mainViewModel = new MainViewModel();
			var win = new MainWindow {
				DataContext = _mainViewModel
			};
			win.Show();
		}

		protected override void OnExit(ExitEventArgs e) {
			_mainViewModel.SaveSettings();
			_mainViewModel.Dispose();

			base.OnExit(e);
		}
	}
}
