using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	class Model
	{
		private string phpDir;

		private ServiceController httpServer;

		/** how long to wait for status service change (in seconds) */
		private const int WAIT_TIME = 7;

		public Model(string phpDir, string httpServiceName)
		{
			this.phpDir = phpDir;
			this.httpServer = new ServiceController(httpServiceName);
		}

		public SortedSet<Version> AvailableVersions
		{
			get
			{
				var versions = new SortedSet<Version>();
				var dirs = Directory.EnumerateDirectories(this.VersionsDir);
				Version version;
				foreach (string dir in dirs)
				{
					var info = new DirectoryInfo(dir);
					if (File.Exists(dir + "\\php.exe") && Version.TryParse(info.Name, out version))
					{
						versions.Add(version);
					}
				}

				return versions;
			}
		}

		public Version ActiveVersion
		{
			get
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
		}

		public bool IsHttpServerRunning
		{
			get { return this.httpServer.Status == ServiceControllerStatus.Running; }
		}

		public string PhpDir
		{
			get { return this.phpDir; }
		}

		public string ActivePhpDir
		{
			get { return this.phpDir + "\\active"; }
		}

		public string ConfigurationDir
		{
			get { return this.phpDir + "\\configuration"; }
		}

		public string VersionsDir
		{
			get { return this.phpDir + "\\versions"; }
		}

		public async Task SwitchTo(Version version)
		{
			await StopHttpServer();
			this.updateSymlink(version);
			this.updatePhpIni(version);
			await StartHttpServer();
		}

		public async Task StartHttpServer()
		{
			if (!await trySetHttpServerState(ServiceControllerStatus.Running, this.httpServer.Start))
			{
				throw new HttpServerStartFailedException();
			}
		}

		public async Task StopHttpServer()
		{
			if (!await trySetHttpServerState(ServiceControllerStatus.Stopped, this.httpServer.Stop))
			{
				throw new HttpServerStopFailedException();
			}
		}

		private Task<bool> trySetHttpServerState(ServiceControllerStatus status, Action method)
		{
			return Task.Run(() =>
			{
				try
				{
					method();
					this.httpServer.WaitForStatus(status, TimeSpan.FromSeconds(WAIT_TIME));
				}
				catch (System.ServiceProcess.TimeoutException) { }
				catch (InvalidOperationException) { }

				this.httpServer.Refresh();
				return this.httpServer.Status == status;
			});
		}

		private void updatePhpIni(Version version)
		{
			var files = new string[] {
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
					content = content.Replace("%phpDir%", this.getVersionDir(version));
					ini.AppendLine();
					ini.AppendLine(content);
				}
			}

			File.WriteAllText(this.ActivePhpDir + "\\php.ini", ini.ToString());
		}

		private void updateSymlink(Version version)
		{
			var symlink = this.ActivePhpDir;
			var target = this.getVersionDir(version);

			try
			{
				Directory.Delete(symlink, true);
			}
			catch (DirectoryNotFoundException) { }

			Symlinks.CreateDir(symlink, target);
		}

		private string getVersionDir(Version version)
		{
			return this.VersionsDir + "\\" + version.ToString();
		}
	}

	abstract class HttpServerFailedException : Exception
	{
		public ServiceControllerStatus status;

		HttpServerFailedException(ServiceControllerStatus status)
		{
			this.status = status;
		}
	}

	class HttpServerStartFailedException : Exception
	{

	}

	class HttpServerStopFailedException : Exception
	{

	}
}
