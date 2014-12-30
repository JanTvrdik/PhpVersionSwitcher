using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Management;
using System;

namespace PhpVersionSwitcher
{
	internal class ProcessManager : IProcessManager
	{
		public string Name { get; private set; }
		public string WorkingDirectory { get; private set; }
		public string FileName { get; private set; }
		public string Arguments { get; private set; }
		public string GroupName { get; private set; }

		public ProcessManager(string path, string arguments = "", string name = null, string groupName = null)
		{
			var info = new FileInfo(path);
			this.WorkingDirectory = info.DirectoryName;
			this.FileName = info.Name;
			this.Arguments = arguments;
			this.Name = name ?? info.Name;
			this.GroupName = groupName;
		}

		public bool IsRunning()
		{
			return this.GetProcess() != null;
		}

		public Task Start()
		{
			return Task.Run(() =>
			{
				try
				{
					var process = Process.Start(new ProcessStartInfo
					{
						WorkingDirectory = this.WorkingDirectory,
						FileName = this.FileName,
						Arguments = this.Arguments,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
					});

					if (process != null && process.WaitForExit(1000))
					{
						throw new ProcessException(this.Name, "start");
					}
				}
				catch
				{
					throw new ProcessException(this.Name, "start");
				}
			});
		}

		public Task Stop()
		{
			return Task.Run(() =>
			{
				var process = this.GetProcess();
				if (process == null)
				{
					return;
				}

				try
				{
					process.Kill();
					if (!process.WaitForExit(7000))
					{
						throw new ProcessException(this.FileName, "stop");
					}
				}
				catch
				{
					throw new ProcessException(this.FileName, "stop");
				}
			});
		}

		public async Task Restart()
		{
			await this.Stop();
			await this.Start();
		}

		private Process GetProcess()
		{
			string wmiQuery = string.Format("select CommandLine, ProcessId from Win32_Process where Name='{0}'", this.FileName);
			ManagementObjectCollection managementObjects = (new ManagementObjectSearcher(wmiQuery)).Get();

			foreach (ManagementObject managementObject in managementObjects)
			{
				string line = (string) (managementObject["CommandLine"]);
				if (line != null && line.Contains(this.Arguments))
				{
					var pId = Convert.ToInt32(managementObject["ProcessId"]);
					return Process.GetProcessById(pId);
				}
			}

			return null;
		}

	}
}
