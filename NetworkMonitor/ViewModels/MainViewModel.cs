using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace NetworkMonitor.ViewModels {
	class MainViewModel : BindableBase, IDisposable {
		TraceEventSession _session;
		Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		bool _isRunning;

		public ObservableCollection<NetworkConnection> ActiveConnections { get; } = new ObservableCollection<NetworkConnection>();
		Dictionary<ConnectionKey, int> _connectionMap = new Dictionary<ConnectionKey, int>(128);
		Dictionary<IPAddress, string> _dnsCache = new Dictionary<IPAddress, string>(64);

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
						parser.TcpIpSend += OnSend;
						parser.UdpIpSend += OnUdpSend;
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

		private void OnUdpSend(UdpIpTraceData obj) {
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

		private void OnDisconnectIpv6(TcpIpV6TraceData obj) {
			var info = (TcpIpV6TraceData)obj.Clone();
			_dispatcher.InvokeAsync(() => {
				var key = new ConnectionKey(info.daddr, info.ProcessID, info.sport, info.dport);
				DisconnectItem(info.TimeStamp, key);
			});
		}

		private void OnConnectIpv6(TcpIpV6ConnectTraceData obj) {
			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.TcpipV6,
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
			var conn = new NetworkConnection {
				ConnectionType = ConnectionType.TcpipV4,
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
			_connectionMap.Add(conn.Key, ActiveConnections.Count - 1);
			RaisePropertyChanged(nameof(OpenConnections));
			var item = ActiveConnections.Last();
			try {
				if (!_dnsCache.TryGetValue(conn.RemoteAddress, out var name)) {
					var iphost = await Dns.GetHostEntryAsync(conn.RemoteAddress);
					name = iphost.HostName;
					_dnsCache.Add(conn.RemoteAddress, name);
				}
				item.RemoteAddressDns = name;
			}
			catch { }
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

		public void Dispose() {
			_session?.Dispose();
		}
	}
}
