using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace NetworkMonitor.ViewModels {
	[Flags]
	public enum ConnectionType {
		TcpIpV4 = 1,
		TcpIpV6 = 2,
		UdpV4 = 4,
		UdpV6 = 8
	}

	class ConnectionKey : IEquatable<ConnectionKey> {
		public readonly IPAddress RemoteAddress;
		public readonly int LocalPort, RemotePort;
		public readonly int ProcessId;

		public ConnectionKey(IPAddress remoteAddress, int processId, int localPort, int remotePort) {
			RemoteAddress = remoteAddress;
			ProcessId = processId;
			LocalPort = localPort;
			RemotePort = remotePort;
		}

		public bool Equals(ConnectionKey other) {
			if (other == null)
				return false;

			return RemoteAddress.Equals(other.RemoteAddress) && ProcessId == other.ProcessId && RemotePort == other.RemotePort 
				&& LocalPort == other.LocalPort;
		}

		public override bool Equals(object obj) => Equals(obj as ConnectionKey);

		public override int GetHashCode() => RemoteAddress.GetHashCode() ^ LocalPort ^ RemotePort ^ ProcessId;
	}

	sealed class NetworkConnection : BindableBase {
		bool _isActive = true;
		public bool IsActive {
			get => _isActive;
			set => SetProperty(ref _isActive, value);
		}

		public DateTime ConnectTime { get; set; }
		DateTime? _disconnectTime;
		public DateTime? DisconnectTime {
			get => _disconnectTime;
			set {
				if (SetProperty(ref _disconnectTime, value)) {
					RaisePropertyChanged(nameof(ConnectionTime));
					RaisePropertyChanged(nameof(DisconnectTimeAsString));
				}
			}
		}

		public TimeSpan? ConnectionTime => DisconnectTime == null ? default(TimeSpan?) : (DisconnectTime.Value - ConnectTime);

		public ConnectionType ConnectionType { get; set; }
		public int ProcessId { get; set; }
		public string ProcessName { get; set; }

		long _sentBytes, _recvBytes;
		public long SentBytes {
			get => _sentBytes;
			set => SetProperty(ref _sentBytes, value);
		}
		public long RecvBytes {
			get => _recvBytes;
			set => SetProperty(ref _recvBytes, value);
		}

		public IPAddress LocalAddress { get; set; }
		public IPAddress RemoteAddress { get; set; }
		public int LocalPort { get; set; }
		public int RemotePort { get; set; }

		ConnectionKey _key;
		public ConnectionKey Key => _key ?? (_key = new ConnectionKey(RemoteAddress, ProcessId, LocalPort, RemotePort));

		public string ConnectTimeAsString => ConnectTime.ToString("G") + "." + ConnectTime.Millisecond.ToString("D3");
		public string DisconnectTimeAsString => DisconnectTime != null ? DisconnectTime.Value.ToString("G") + "." + DisconnectTime.Value.Millisecond.ToString("D3") : string.Empty;

		//public Brush RowBackground => IsActive ? Brushes.Transparent : Brushes.Red;

		string _remoteAddressDns;
		public string RemoteAddressDns {
			get => _remoteAddressDns;
			set => SetProperty(ref _remoteAddressDns, value);
		}

		public string LocalAddressFull => LocalAddress.ToString() + ":" + LocalPort;
		public string RemoteAddressFull => RemoteAddress.ToString() + ":" + RemotePort;
	}
}
