using DebugPrint.ViewModels;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Zodiacon.WPF;

namespace DebugPrint {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		MainViewModel _mainViewModel;
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			var ui = new UIServicesDefaults();
			var vm = _mainViewModel = new MainViewModel(ui);
			var win = new MainWindow { DataContext = vm };
			vm.LoadSettings();
			win.Show();
			ui.MessageBoxService.SetOwner(win);
		}

		protected override void OnExit(ExitEventArgs e) {
			_mainViewModel.SaveSettings();
			_mainViewModel.Dispose();

			base.OnExit(e);
		}
	}
}
