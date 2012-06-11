using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace Worker {
	static class Program {
		static void Main() {
			var service = new MainService();
			service.InizializzaLogging();
			if (System.Diagnostics.Debugger.IsAttached || Environment.UserInteractive) {
				Application.EnableVisualStyles();
				Application.Run(new MainForm(service));
			} else {
				ServiceBase.Run(new ServiceBase[] { service });
			}
		}
	}
}
