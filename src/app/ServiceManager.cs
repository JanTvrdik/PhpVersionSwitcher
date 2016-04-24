using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	internal class ServiceManager : IProcessManager
	{
		public const int WaitTime = 15;

		private ServiceController service;
		public string GroupName { get; private set; }
		public string Name { get; private set; }

		public ServiceManager(string serviceName, string label = null, string groupName = null)
		{
			this.service = new ServiceController(serviceName);
			this.GroupName = groupName;
			this.Name = label ?? serviceName;
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
				var timeSpan = TimeSpan.FromSeconds(WaitTime);

				try
				{
					method();
					this.service.WaitForStatus(status, timeSpan);
				}
				catch (System.ServiceProcess.TimeoutException) { }
				catch (InvalidOperationException)
				{
					Task.Delay(timeSpan).Wait(); // force-wait
				}

				return this.CheckStatus(status);
			});
		}
	}
}
