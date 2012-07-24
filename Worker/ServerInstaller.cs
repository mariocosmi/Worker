using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Worker {
	[RunInstaller(true)]
	public partial class ServerInstaller: Installer {
		public ServerInstaller() {
			ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
			ServiceInstaller serviceInstaller = new ServiceInstaller();

			//# Service Account Information
			serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
			serviceProcessInstaller.Username = null;
			serviceProcessInstaller.Password = null;

			//# Service Information
			serviceInstaller.DisplayName = "Worker";
			serviceInstaller.StartType = ServiceStartMode.Manual;

			// This must be identical to the WindowsService.ServiceBase name
			// set in the constructor of WindowsService.cs
			serviceInstaller.ServiceName = "Worker";

			this.Installers.Add(serviceProcessInstaller);
			this.Installers.Add(serviceInstaller);
		}
	}
}
