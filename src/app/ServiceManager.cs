using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class ServiceManager : IProcessManager
	{
		public const int WaitTime = 7;

		private ServiceController service;

		public ServiceManager(string serviceName)
		{
			this.service = new ServiceController(serviceName);
		}

		public string Name
		{
			get { return this.service.ServiceName; }
		}

		public bool IsRunning()
		{
			return this.CheckStatus(ServiceControllerStatus.Running);
		}

		public async Task Start()
		{
			if (!await this.TrySetStatus(ServiceControllerStatus.Running, this.service.Start))
			{
				throw new ProcessException(this.Name, "start");
			}
		}

		public async Task Stop()
		{
			if (!await this.TrySetStatus(ServiceControllerStatus.Stopped, this.service.Stop))
			{
				throw new ProcessException(this.Name, "stop");
			}
		}

		public async Task Restart()
		{
			await this.Stop();
			await this.Start();
		}

		private bool CheckStatus(ServiceControllerStatus status)
		{
			try
			{
				this.service.Refresh();
				return this.service.Status == status;
			}
			catch (InvalidOperationException)
			{
				return false;
			}
		}

		private Task<bool> TrySetStatus(ServiceControllerStatus status, Action method)
		{
			return Task.Run(() =>
			{
				try
				{
					method();
					this.service.WaitForStatus(status, TimeSpan.FromSeconds(WaitTime));
				}
				catch (System.ServiceProcess.TimeoutException) { }
				catch (InvalidOperationException) { }

				return this.CheckStatus(status);
			});
		}
	}
}
