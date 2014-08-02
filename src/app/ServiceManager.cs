using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class ServiceManager
	{
		public const int WaitTime = 7;

		private ServiceController service;

		public ServiceManager(string serviceName)
		{
			this.service = new ServiceController(serviceName);
		}

		public string ServiceName
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
				throw new ServiceStartFailedException(this.ServiceName);
			}
		}

		public async Task Stop()
		{
			if (!await this.TrySetStatus(ServiceControllerStatus.Stopped, this.service.Stop))
			{
				throw new ServiceStopFailedException(this.ServiceName);
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

	abstract class ServiceException : Exception
	{
		public string ServiceName { get; private set; }

		protected ServiceException(string serviceName)
		{
			this.ServiceName = serviceName;
		}
	}

	class ServiceStartFailedException : ServiceException
	{
		public ServiceStartFailedException(string serviceName) : base(serviceName)
		{
		}
	}

	class ServiceStopFailedException : ServiceException
	{
		public ServiceStopFailedException(string serviceName) : base(serviceName)
		{
		}
	}
}
