using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class ProcessManager : IProcessManager
	{
		public string WorkingDirectory { get; private set; }
		public string FileName { get; private set; }
		public string Arguments { get; private set; }
		public string Name { get { return this.FileName.Replace(".exe", ""); }}

		public ProcessManager(string path, string arguments = "")
		{
			var info = new FileInfo(path);
			this.WorkingDirectory = info.DirectoryName;
			this.FileName = info.Name;
			this.Arguments = arguments;
		}

		public bool IsRunning()
		{
			return Process.GetProcessesByName(this.Name).Length > 0;
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
					throw new ProcessException(this.FileName, "start");
				}
			});
		}

		public Task Stop()
		{
			return Task.Run(() =>
			{
				var processes = Process.GetProcessesByName(this.Name);

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
	}
}
