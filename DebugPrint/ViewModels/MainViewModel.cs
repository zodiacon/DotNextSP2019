using DebugPrint.Filters;
using DebugPrint.Models;
using DebugPrint.Views;
using Filtering.Core;
using MahApps.Metro;
using Microsoft.O365.Security.ETW;
using Microsoft.O365.Security.ETW.Kernel;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Zodiacon.WPF;

namespace DebugPrint.ViewModels {
	sealed class MainViewModel : BindableBase, IDisposable {
		public ObservableCollection<DebugItem> DebugItems { get; } = new ObservableCollection<DebugItem>();
		public ObservableCollection<FilterViewModel> Filters { get; private set; } = new ObservableCollection<FilterViewModel>();

		readonly KernelTrace _trace;
		readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		readonly EventWaitHandle _dataReadyEvent, _bufferReadyEvent;
		readonly MemoryMappedFile _mmf;
		readonly MemoryMappedViewStream _stm;
		readonly FilterManager _filterManager = new FilterManager();
		readonly IUIServices UI;

		const int _bufferSize = 1 << 12;

		public MainViewModel(IUIServices ui) {
			UI = ui;
			_trace = new KernelTrace("DebugPrintTrace");
			var provider = new DebugPrintProvider();
			provider.OnEvent += OnEvent;
			_trace.Enable(provider);

			_bufferReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_BUFFER_READY");
			_dataReadyEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "DBWIN_DATA_READY");
			_stopEvent = new AutoResetEvent(false);

			_mmf = MemoryMappedFile.CreateOrOpen("DBWIN_BUFFER", _bufferSize);
			_stm = _mmf.CreateViewStream();
		}

		private void OnEvent(IEventRecord record) {
			var item = new DebugItem {
				Time = record.Timestamp,
				ProcessId = (int)record.ProcessId,
				ProcessName = TryGetProcessName(record.ProcessId),
				ThreadId = (int)record.ThreadId,
				Text = record.GetAnsiString("Message").TrimEnd('\n', '\r'),
				Component = record.GetUInt32("Component", 0),
				IsKernel = true
			};
			AddDebugItem(item);
		}

		void AddDebugItem(DebugItem item) {
			if(Filters.Count == 0)
				_dispatcher.InvokeAsync(() => DebugItems.Add(item));

			foreach (var (filter, result) in _filterManager.Filters) {
				if (filter.Eval(item)) {
					if(result == FilterResult.Include)
						_dispatcher.InvokeAsync(() => DebugItems.Add(item));
					return;
				}
			}
		}

		public DelegateCommand ShowFiltersCommand => new DelegateCommand(() => {
			var vm = UI.DialogService.CreateDialog<FilterEditingViewModel, FilterEditingView>(Filters);
			if (vm.ShowDialog() == true) {
				// update filters
				Filters = vm.Filters;
				RaisePropertyChanged(nameof(Filters));
				BuildFilters();
			}
		});

		private void BuildFilters() {
			_filterManager.Filters.Clear();
			foreach (var filter in Filters) {
				var result = (FilterResult)Enum.Parse(typeof(FilterResult), filter.Action);
				var dfilter = new DebugItemGenericFilter((Relation)Enum.Parse(typeof(Relation), filter.Relation.ToEnum()), filter.Value, 
					(DebugItemFilterType)Enum.Parse(typeof(DebugItemFilterType), filter.Property.ToEnum()));
				_filterManager.Filters.Add((dfilter, result));
			}
		}

		private string TryGetProcessName(uint processId) {
			try {
				return Process.GetProcessById((int)processId).ProcessName;
			}
			catch {
				return string.Empty;
			}
		}

		public void Dispose() {
			_trace.Dispose();
			_stopEvent?.Dispose();
			_bufferReadyEvent?.Dispose();
			_dataReadyEvent?.Dispose();
			_stm?.Dispose();
			_mmf?.Dispose();
		}

		string _searchText;
		public string SearchText {
			get => _searchText;
			set {
				if (SetProperty(ref _searchText, value)) {
					var view = CollectionViewSource.GetDefaultView(DebugItems);
					if (value == null)
						view.Filter = null;
					else {
						var text = value.ToLower();
						view.Filter = obj => {
							var item = (DebugItem)obj;
							return item.ProcessName.ToLower().Contains(text) || item.Text.ToLower().Contains(text);
						};
					}
				}
			}
		}

		IList _selectedItems;
		public IList SelectedItems {
			get => _selectedItems;
			set {
				SetProperty(ref _selectedItems, value);
				RaisePropertyChanged(nameof(CanCopy));
			}
		}

		public bool CanCopy => SelectedItems?.Count > 0;

		public DelegateCommand CopyCommand => new DelegateCommand(() => {
			var text = SelectedItems.Cast<DebugItem>().Aggregate(new StringBuilder(512), (sb, item) => sb.Append(item.ToString()).Append(Environment.NewLine));
			Clipboard.SetText(text.ToString());
		}).ObservesCanExecute(() => CanCopy);

		public DelegateCommand CutCommand => new DelegateCommand(() => {
			CopyCommand.Execute();
			foreach (var item in SelectedItems.Cast<DebugItem>().ToArray())
				DebugItems.Remove(item);
		}).ObservesCanExecute(() => CanCopy);

		public DelegateCommand ClearAllCommand => new DelegateCommand(() => DebugItems.Clear());
		public DelegateCommand ExitCommand => new DelegateCommand(() => Application.Current.Shutdown());

		public DelegateCommand SaveCommand => new DelegateCommand(() => {
			var filename = ShowSaveDialog();
			if(filename != null)
				DoSave(filename, false);
		});

		public DelegateCommand SaveFilteredCommand => new DelegateCommand(() => {
			var filename = ShowSaveDialog();
			if (filename != null)
				DoSave(filename, true);
		});

		string ShowSaveDialog() {
			var dlg = new SaveFileDialog {
				Title = "Select File",
				DefaultExt = ".csv",
				Filter = "CSV files|*.csv|All Files|*.*",
				OverwritePrompt = true
			};
			return dlg.ShowDialog() == true ? dlg.FileName : null;
		}

		void DoSave(string filename, bool filtered) {
			try {
				var view = CollectionViewSource.GetDefaultView(DebugItems);
				using (var writer = File.CreateText(filename)) {
					// write headers
					writer.WriteLine("Time,User/kernel,PID,TID,Component,Process Name,Text");
					foreach (var item in DebugItems)
						if (!filtered || view.Filter?.Invoke(item) == true)
							writer.WriteLine(item.ToString(","));
				}
			}
			catch (IOException ex) {
				MessageBox.Show(ex.Message, Constants.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		bool _alwaysOnTop;
		public bool AlwaysOnTop {
			get => _alwaysOnTop;
			set {
				SetProperty(ref _alwaysOnTop, value);
				Application.Current.MainWindow.Topmost = value;
			}
		}

		bool _isRunningKernel, _isRunningUser;
		public bool IsRunningKernel {
			get => _isRunningKernel;
			set {
				if (SetProperty(ref _isRunningKernel, value)) {
					if (value) {
						var t = new Thread(() => _trace.Start()) {
							IsBackground = true
						};
						t.Start();
					}
					else {
						_trace.Stop();
					}
				}
			}
		}

		readonly AutoResetEvent _stopEvent;
		public bool IsRunningUser {
			get => _isRunningUser;
			set {
				if (SetProperty(ref _isRunningUser, value)) {
					if (value) {
						var t = new Thread(() => {
							var reader = new BinaryReader(_stm);
							var bytes = new byte[_bufferSize];
							do {
								_bufferReadyEvent.Set();
								if (_dataReadyEvent.WaitOne(400)) {
									var time = DateTime.Now;
									_stm.Position = 0;
									var pid = reader.ReadInt32();
									_stm.Read(bytes, 0, _bufferSize - sizeof(int));
									int index = Array.IndexOf(bytes, (byte)0);
									var text = Encoding.ASCII.GetString(bytes, 0, index - 1).TrimEnd('\n', '\r');
									var item = new DebugItem {
										ProcessId = pid,
										Text = text,
										Time = time,
										ProcessName = TryGetProcessName((uint)pid)
									};
									AddDebugItem(item);
								}
							} while (!_stopEvent.WaitOne(0));
						});
						t.IsBackground = true;
						t.Start();
					}
					else {
						_stopEvent.Set();
					}
				}
			}

		}

		public AccentViewModel[] Accents => ThemeManager.Accents.Select(a => new AccentViewModel(a)).ToArray();
		public AppTheme[] Themes => ThemeManager.AppThemes.ToArray();

		public AccentViewModel CurrentAccent { get; private set; }


		public ICommand ChangeAccentCommand => new DelegateCommand<AccentViewModel>(accent => {
			if (CurrentAccent != null)
				CurrentAccent.IsCurrent = false;
			CurrentAccent = accent;
			accent.IsCurrent = true;
			RaisePropertyChanged(nameof(CurrentAccent));
		}, accent => accent != CurrentAccent).ObservesProperty(() => CurrentAccent);

		public ICommand ChangeThemeCommand => new DelegateCommand<AppTheme>(theme => {
			var style = ThemeManager.DetectAppStyle();
			if (theme != style.Item1) {
				ThemeManager.ChangeAppStyle(Application.Current, style.Item2, theme);
			}
		});

		private string GetSettingsPath() {
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\debugprint";
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			return directory + @"\Settings.Xml";
		}

		public void LoadSettings() {
			try {
				using (var fs = File.OpenRead(GetSettingsPath())) {
					var serializer = new DataContractSerializer(typeof(Settings));
					var settings = serializer.ReadObject(fs) as Settings;
					if (settings != null) {
						AlwaysOnTop = settings.AlwaysOnTop;
						var accent = Accents.FirstOrDefault(acc => acc.Name == settings.AccentColor);
						if (accent != null)
							ChangeAccentCommand.Execute(accent);
						var theme = Themes.FirstOrDefault(t => t.Name == settings.Theme);
						if (theme != null)
							ChangeThemeCommand.Execute(theme);
					}
				}
			}
			catch {
			}
		}

		public void SaveSettings() {
			var style = ThemeManager.DetectAppStyle();
			var settings = new Settings {
				AlwaysOnTop = AlwaysOnTop,
				AccentColor = style.Item2.Name,
				Theme = style.Item1.Name,
			};
			try {
				using (var fs = new FileStream(GetSettingsPath(), FileMode.Create)) {
					var serializer = new DataContractSerializer(typeof(Settings));
					serializer.WriteObject(fs, settings);
				}
			}
			catch {
			}
		}

	}
}
