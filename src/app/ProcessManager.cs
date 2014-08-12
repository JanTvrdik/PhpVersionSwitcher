using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	internal class ProcessManager : IProcessManager
	{
		public string Name { get; private set; }
		public string WorkingDirectory { get; private set; }
		public string FileName { get; private set; }
		public string Arguments { get; private set; }

		public ProcessManager(string path, string arguments = "", string name = null)
		{
			var info = new FileInfo(path);
			this.WorkingDirectory = info.DirectoryName;
			this.FileName = info.Name;
			this.Arguments = arguments;
			this.Name = name ?? info.Name;
		}

		public bool IsRunning()
		{
			return this.GetProcesses().Length > 0;
		}

		public Task Start()
		{
			return Task.Run(() =>
			{
				try
				{
					Process.Start(new ProcessStartInfo
					{
						WorkingDirectory = this.WorkingDirectory,
						FileName = this.FileName,
						Arguments = this.Arguments,
						CreateNoWindow = true,
						WindowStyle = ProcessWindowStyle.Hidden,
					});
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
				var processes = this.GetProcesses();

				try
				{
					foreach (var process in processes)
					{
						process.Kill();
					}

					foreach (var process in processes)
					{
						if (!process.WaitForExit(7000))
						{
							throw new ProcessException(this.FileName, "stop");
						}
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

		private Process[] GetProcesses()
		{
			return Process.GetProcessesByName(this.FileName.Replace(".exe", ""));
		}
	}
}
