using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleProvider {
	public partial class Form1 : Form {
		public Form1() {
			InitializeComponent();
		}

		int _counter;

		private void button1_Click(object sender, EventArgs e) {
			SimpleEventSource.Log.Event1(++_counter, "this is button 1");
		}

		private void button2_Click(object sender, EventArgs e) {
			SimpleEventSource.Log.Event2("this is button 2");
		}

		protected override void OnFormClosed(FormClosedEventArgs e) {
			SimpleEventSource.Log.EventStop();

			base.OnFormClosed(e);
		}

		private void button3_Click(object sender, EventArgs e) {
			for (int i = 0; i < 10; i++)
				Task.Run(() => SimpleEventSource.Log.Event1(Thread.CurrentThread.ManagedThreadId, "Text from Thread pool"));
		}
	}
}
