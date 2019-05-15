using MahApps.Metro;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using NetworkMonitor.Models;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace NetworkMonitor.ViewModels {
	sealed class MainViewModel : BindableBase, IDisposable {
		TraceEventSession _session;
		Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		bool _isRunning;

		public ObservableCollection<NetworkConnection> ActiveConnections { get; } = new ObservableCollection<NetworkConnection>();

		readonly Dictionary<ConnectionKey, int> _connectionMap = new Dictionary<ConnectionKey, int>(128);
		readonly Dictionary<IPAddress, string> _dnsCache = new Dictionary<IPAddress, string>(64);

		ConnectionType _monitoredConnections = ConnectionType.TcpIpV4 | ConnectionType.TcpIpV6;

		public MainViewModel() {
			LoadSettings(Application.Current.MainWindow);
		}

		public ConnectionType MonitoredConnections {
			get => _monitoredConnections;
			set => SetProperty(ref _monitoredConnections, value);
		}

		public bool IsRunning {
			get => _isRunning;
			set {
				if (SetProperty(ref _isRunning, value)) {
					if (value && _session == null) {
						_session = new TraceEventSession("ETWNetworkMonitor");
						_session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
						var parser = _session.Source.Kernel;
						parser.TcpIpConnect += OnConnect;
						parser.TcpIpConnectIPV6 += OnConnectIpv6;
						parser.TcpIpDisconnect += OnDisconnect;
						parser.TcpIpDisconnectIPV6 += OnDisconnectIpv6;
						parser.TcpIpRecv += OnRecv;
						parser.TcpIpRecvIPV6 += OnRecvIPV6;
						parser.TcpIpSendIPV6 += OnSendIPV6;
						parser.TcpIpSend += OnSend;
						parser.UdpIpSend += OnUdpSend;
						parser.UdpIpRecv += OnUdpRecv;
						parser.UdpIpSendIPV6 += OnUdpSendIPV6;
						parser.UdpIpRecvIPV6 += OnUdpRecvIPV6;
					}
					if (value) {
						var t = new Thread(() => _session.Source.Process());
						t.IsBackground = true;
						t.Start();
					}
					else {
						_session.Dispose();
						_session = null;
					}
				}
			}
		}

		NetworkConnection CreateUdpConnection(UpdIpV6TraceData obj) {
			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.UdpV6,
				ConnectTime = obj.TimeStamp,
				ProcessId = obj.ProcessID,
				ProcessName = obj.ProcessName,
				LocalAddress = obj.saddr,
				RemoteAddress = obj.daddr,
				RemotePort = obj.dport,
				LocalPort = obj.sport,
				IsActive = false
			};
			return conn;
		}

		NetworkConnection CreateUdpConnection(UdpIpTraceData obj) {
			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.UdpV4,
				ConnectTime = obj.TimeStamp,
				ProcessId = obj.ProcessID,
				ProcessName = obj.ProcessName,
				LocalAddress = obj.saddr,
				RemoteAddress = obj.daddr,
				RemotePort = obj.dport,
				LocalPort = obj.sport,
				IsActive = false
			};
			return conn;
		}

		private void OnUdpRecvIPV6(UpdIpV6TraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.UdpV6))
				return;

			var conn = CreateUdpConnection(obj);
			conn.RecvBytes = obj.size;
			_dispatcher.InvokeAsync(() => AddConnection(conn));
		}

		private void OnUdpSendIPV6(UpdIpV6TraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.UdpV6))
				return;

			var conn = CreateUdpConnection(obj);
			conn.SentBytes = obj.size;
			_dispatcher.InvokeAsync(() => AddConnection(conn));
		}

		private void OnUdpRecv(UdpIpTraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.UdpV4))
				return;

			var conn = CreateUdpConnection(obj);
			conn.RecvBytes = obj.size;
			_dispatcher.InvokeAsync(() => AddConnection(conn));
		}

		private void OnSendIPV6(TcpIpV6SendTraceData info) {
			if (!MonitoredConnections.HasFlag(ConnectionType.UdpV6))
				return;

			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			var size = info.size;
			_dispatcher.InvokeAsync(() => {
				if (_connectionMap.TryGetValue(key, out var index)) {
					ActiveConnections[index].SentBytes += size;
				}
			});
		}

		private void OnRecvIPV6(TcpIpV6TraceData info) {
			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			var size = info.size;
			_dispatcher.InvokeAsync(() => {
				if (_connectionMap.TryGetValue(key, out var index)) {
					ActiveConnections[index].RecvBytes += size;
				}
			});
		}

		private void OnUdpSend(UdpIpTraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.UdpV4))
				return;

			var conn = CreateUdpConnection(obj);
			conn.SentBytes = obj.size;
			_dispatcher.InvokeAsync(() => AddConnection(conn));

		}

		private void OnRecv(TcpIpTraceData info) {
			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			var size = info.size;
			_dispatcher.InvokeAsync(() => {
				if (_connectionMap.TryGetValue(key, out var index)) {
					ActiveConnections[index].RecvBytes += size;
				}
			});
		}

		private void OnSend(TcpIpSendTraceData info) {
			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			var size = info.size;
			_dispatcher.InvokeAsync(() => {
				if (_connectionMap.TryGetValue(key, out var index)) {
					ActiveConnections[index].SentBytes += size;
				}
			});
		}

		private void OnDisconnect(TcpIpTraceData info) {
			var time = info.TimeStamp;
			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			_dispatcher.InvokeAsync(() => DisconnectItem(time, key));
		}

		private void DisconnectItem(DateTime time, ConnectionKey key) {
			if (_connectionMap.TryGetValue(key, out var index)) {
				var item = ActiveConnections[index];
				item.DisconnectTime = time;
				item.IsActive = false;
				_connectionMap.Remove(key);
				RaisePropertyChanged(nameof(OpenConnections));
			}
		}

		private void OnDisconnectIpv6(TcpIpV6TraceData info) {
			var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
			var time = info.TimeStamp;
			_dispatcher.InvokeAsync(() => DisconnectItem(time, key));
		}

		private void OnConnectIpv6(TcpIpV6ConnectTraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.TcpIpV6))
				return;

			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.TcpIpV6,
				ConnectTime = obj.TimeStamp,
				ProcessId = obj.ProcessID,
				ProcessName = obj.ProcessName,
				LocalAddress = obj.saddr,
				RemoteAddress = obj.daddr,
				RemotePort = obj.dport,
				LocalPort = obj.sport
			};
			_dispatcher.InvokeAsync(() => AddConnection(conn));
		}

		private void OnConnect(TcpIpConnectTraceData obj) {
			if (!MonitoredConnections.HasFlag(ConnectionType.TcpIpV4))
				return;

			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.TcpIpV4,
				ConnectTime = obj.TimeStamp,
				ProcessId = obj.ProcessID,
				ProcessName = obj.ProcessName,
				LocalAddress = obj.saddr,
				RemoteAddress = obj.daddr,
				RemotePort = obj.dport,
				LocalPort = obj.sport
			};
			_dispatcher.InvokeAsync(() => AddConnection(conn));
		}

		private async void AddConnection(NetworkConnection conn) {
			ActiveConnections.Add(conn);
			if (conn.IsActive) {
				_connectionMap.Add(conn.Key, ActiveConnections.Count - 1);
				RaisePropertyChanged(nameof(OpenConnections));
			}
			try {
				if (!_dnsCache.TryGetValue(conn.RemoteAddress, out var name)) {
					var iphost = await Dns.GetHostEntryAsync(conn.RemoteAddress);
					name = iphost.HostName;
					_dnsCache[conn.RemoteAddress] = name;
				}
				conn.RemoteAddressDns = name;
			}
			catch {
				_dnsCache[conn.RemoteAddress] = string.Empty;
			}
		}

		public int OpenConnections => _connectionMap.Count;

		public DelegateCommand ClearInactiveConnectionsCommand => new DelegateCommand(() => {
			for (int i = 0; i < ActiveConnections.Count; i++) {
				var item = ActiveConnections[i];
				if (!item.IsActive) {
					_connectionMap.Remove(new ConnectionKey(item.RemoteAddress, item.ProcessId, item.LocalPort, item.RemotePort));
					ActiveConnections.RemoveAt(i);
					i--;
				}
			}
			RaisePropertyChanged(nameof(OpenConnections));
		});

		public DelegateCommand ClearAllCommand => new DelegateCommand(() => {
			ActiveConnections.Clear();
			RaisePropertyChanged(nameof(OpenConnections));
			_connectionMap.Clear();
		});

		public bool MonitorTcpV4 {
			get => GetMonitoredConnection(ConnectionType.TcpIpV4);
			set => SetMonitoredConnection(ConnectionType.TcpIpV4, value);
		}

		public bool MonitorTcpV6 {
			get => GetMonitoredConnection(ConnectionType.TcpIpV6);
			set => SetMonitoredConnection(ConnectionType.TcpIpV6, value);
		}

		public bool MonitorUdpV4 {
			get => GetMonitoredConnection(ConnectionType.UdpV4);
			set => SetMonitoredConnection(ConnectionType.UdpV4, value);
		}

		public bool MonitorUdpV6 {
			get => GetMonitoredConnection(ConnectionType.UdpV6);
			set => SetMonitoredConnection(ConnectionType.UdpV6, value);
		}

		bool GetMonitoredConnection(ConnectionType type) => MonitoredConnections.HasFlag(type);
		void SetMonitoredConnection(ConnectionType type, bool set) {
			if (set)
				MonitoredConnections |= type;
			else
				MonitoredConnections &= ~type;
			RaisePropertyChanged(nameof(MonitorTcpV4));
			RaisePropertyChanged(nameof(MonitorTcpV6));
			RaisePropertyChanged(nameof(MonitorUdpV4));
			RaisePropertyChanged(nameof(MonitorUdpV6));
		}

		public void Dispose() {
			_session?.Dispose();
		}

		public AccentViewModel[] Accents => ThemeManager.Accents.Select(a => new AccentViewModel(a)).ToArray();
		public AppTheme[] Themes => ThemeManager.AppThemes.ToArray();

		public AccentViewModel CurrentAccent { get; private set; }

		bool _alwaysOnTop;
		public ICommand AlwaysOnTopCommand => new DelegateCommand<Window>(win => win.Topmost = _alwaysOnTop = !win.Topmost);

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
			var directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\networkmonitor";
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);
			return directory + @"\NetworkMonitor.Settings.Xml";
		}

		public void LoadSettings(Window window) {
			try {
				using (var fs = File.OpenRead(GetSettingsPath())) {
					var serializer = new DataContractSerializer(typeof(Settings));
					var settings = serializer.ReadObject(fs) as Settings;
					if (settings != null) {
						if (settings.AlwaysOnTop)
							window.Topmost = true;
						var accent = Accents.FirstOrDefault(acc => acc.Name == settings.AccentColor);
						if (accent != null)
							ChangeAccentCommand.Execute(accent);
						var theme = Themes.FirstOrDefault(t => t.Name == settings.Theme);
						if (theme != null)
							ChangeThemeCommand.Execute(theme);
						MonitoredConnections = settings.MonitoredConnections;
					}
				}
			}
			catch {
			}
		}

		public void SaveSettings() {
			var style = ThemeManager.DetectAppStyle();
			var settings = new Settings {
				AlwaysOnTop = _alwaysOnTop,
				AccentColor = style.Item2.Name,
				Theme = style.Item1.Name,
				MonitoredConnections = MonitoredConnections
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
