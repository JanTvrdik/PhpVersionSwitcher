using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class Model
	{
		public const int WaitTime = 7;

		private string phpBaseDir;

		private ServiceController httpServer;

		public Model(string phpBaseDir, string httpServiceName)
		{
			this.phpBaseDir = phpBaseDir;
			this.httpServer = new ServiceController(httpServiceName);
		}

		public SortedSet<Version> GetAvailableVersions()
		{
			var versions = new SortedSet<Version>();
			var dirs = Directory.EnumerateDirectories(this.VersionsDir);
			foreach (var dir in dirs)
			{
				var info = new DirectoryInfo(dir);
				Version version;
				if (File.Exists(dir + "\\php.exe") && Version.TryParse(info.Name, out version))
				{
					versions.Add(version);
				}
			}
			return versions;
		}

		public Version GetActiveVersion()
		{
			try
			{
				var target = Symlinks.GetTarget(this.ActivePhpDir); // may throw exception
				var name = new DirectoryInfo(target).Name;
				Version version;
				Version.TryParse(name, out version);
				return version;
			}
			catch (System.ComponentModel.Win32Exception)
			{
				return null;
			}
		}

		public bool IsHttpServerRunning()
		{
			this.httpServer.Refresh();
			return this.httpServer.Status == ServiceControllerStatus.Running;
		}

		public async Task SwitchTo(Version version)
		{
			await StopHttpServer();
			await this.UpdateSymlink(version);
			await this.UpdatePhpIni(version);
			await StartHttpServer();
		}

		public async Task StartHttpServer()
		{
			if (!await TrySetHttpServerState(ServiceControllerStatus.Running, this.httpServer.Start))
			{
				throw new HttpServerStartFailedException();
			}
		}

		public async Task StopHttpServer()
		{
			if (!await TrySetHttpServerState(ServiceControllerStatus.Stopped, this.httpServer.Stop))
			{
				throw new HttpServerStopFailedException();
			}
		}

		private string ActivePhpDir
		{
			get { return this.phpBaseDir + "\\active"; }
		}

		private string ConfigurationDir
		{
			get { return this.phpBaseDir + "\\configuration"; }
		}

		private string VersionsDir
		{
			get { return this.phpBaseDir + "\\versions"; }
		}

		private string GetVersionDir(Version version)
		{
			return this.VersionsDir + "\\" + version;
		}

		private Task<bool> TrySetHttpServerState(ServiceControllerStatus status, Action method)
		{
			return Task.Run(() =>
			{
				try
				{
					method();
					this.httpServer.WaitForStatus(status, TimeSpan.FromSeconds(WaitTime));
				}
				catch (System.ServiceProcess.TimeoutException) { }
				catch (InvalidOperationException) { }

				this.httpServer.Refresh();
				return this.httpServer.Status == status;
			});
		}

		private Task UpdatePhpIni(Version version)
		{
			return Task.Run(() =>
			{
				var files = new string[]
				{
					version.Major + ".x.x.ini",
					version.Major + "." + version.Minor + ".x.ini",
					version.Major + "." + version.Minor + "." + version.Patch + ".ini"
				};

				var ini = new StringBuilder();
				foreach (var file in files)
				{
					var path = this.ConfigurationDir + "\\" + file;
					if (File.Exists(path))
					{
						var content = File.ReadAllText(path);
						content = content.Replace("%phpDir%", this.GetVersionDir(version));
						ini.AppendLine();
						ini.AppendLine(content);
					}
				}

				File.WriteAllText(this.ActivePhpDir + "\\php.ini", ini.ToString());
			});
		}

		private Task UpdateSymlink(Version version)
		{
			return Task.Run(() =>
			{
				var symlink = this.ActivePhpDir;
				var target = this.GetVersionDir(version);

				try
				{
					Directory.Delete(symlink, true);
				}
				catch (DirectoryNotFoundException) { }

				Symlinks.CreateDir(symlink, target);
			});
		}
	}

	class HttpServerStartFailedException : Exception
	{

	}

	class HttpServerStopFailedException : Exception
	{

	}
}
