using System;
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

		public ProcessManager(string workingDirectory, string fileName, string arguments)
		{
			this.WorkingDirectory = workingDirectory;
			this.FileName = fileName;
			this.Arguments = arguments;
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
					Process.Start(new ProcessStartInfo()
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
				try
				{
					var process = this.GetProcess();
					if (process == null) return;

					process.Kill();
					if (process.WaitForExit(7000)) return;
				}
				catch { }

				throw new ProcessException(this.FileName, "stop");
			});
		}

		public async Task Restart()
		{
			await this.Stop();
			await this.Start();
		}

		private Process GetProcess()
		{
			var processes = Process.GetProcessesByName(this.Name);
			return processes.Length > 0 ? processes[0] : null;
		}
	}
}
