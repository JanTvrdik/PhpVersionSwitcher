using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
		public Process Process { get; set; }

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
			if (Process == null)
			{
				return false;
			}
			Process.Refresh();
			return !Process.HasExited;
		}

		public Task Start()
		{
			return Task.Run(() =>
			{
				try
				{
					Process = Process.Start(new ProcessStartInfo
					{
						WorkingDirectory = this.WorkingDirectory,
						FileName = this.FileName,
						Arguments = this.Arguments,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
					});

					if (Process != null && Process.WaitForExit(1000))
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
				if (Process == null)
				{
					return;
				}

				try
				{
					this.KillProcessAndChildren(Process.Id);
					if (!Process.WaitForExit(7000))
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


		private void KillProcessAndChildren(int pid)
		{
			try
			{
				Process proc = Process.GetProcessById(pid);
				proc.Kill();
			}
			catch (ArgumentException)
			{
			}

			var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
			var moc = searcher.Get();
			foreach (var mo in moc)
			{
				this.KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
			}
		}

	}
}
