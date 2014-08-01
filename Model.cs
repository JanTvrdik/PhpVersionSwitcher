using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace PhpVersionSwitcher
{
    class Model
    {
        private string phpDir;

        private ServiceController apache;

		private const int MAX_START_RETRIES = 5;

		private const int MAX_STOP_RETRIES = 5;

        public Model(string phpDir, string httpServiceName)
        {
            this.phpDir = phpDir;
            this.apache = new ServiceController(httpServiceName);
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
                    var target = Symlinks.GetTarget(this.ActivePhpDir);
                    var name = new DirectoryInfo(target).Name;
                    return Version.Parse(name);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public ServiceControllerStatus ApacheStatus
        {
            get { return this.apache.Status; }
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

        public void SwitchTo(Version version)
        {
			if (!this.StopApache()) throw new ApacheStopFailedException();
            this.updateSymlink(version);
            this.updatePhpIni(version);
			if (!this.StartApache()) throw new ApacheStartFailedException();
        }

        public bool StartApache()
        {
			for (int attempt = 1; attempt <= MAX_START_RETRIES && this.apache.Status != ServiceControllerStatus.Running; attempt++)
            {
                try
                {
                    this.apache.Start();
                    this.apache.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(1));
                }
                catch (System.ServiceProcess.TimeoutException) { }
                catch (InvalidOperationException) { }
            }

            return (this.apache.Status == ServiceControllerStatus.Running || this.apache.Status == ServiceControllerStatus.StartPending);
        }

        public bool StopApache()
        {
            for (int attempt = 1; attempt <= MAX_STOP_RETRIES && this.apache.Status != ServiceControllerStatus.Stopped; attempt++)
			{
				try
				{
					this.apache.Stop();
					this.apache.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(1));
				}
				catch (System.ServiceProcess.TimeoutException) { }
				catch (InvalidOperationException) { }
			}

            return (this.apache.Status == ServiceControllerStatus.Stopped || this.apache.Status == ServiceControllerStatus.StopPending);
        }

        private void updatePhpIni(Version version)
        {
            var files = new string[] {
                version.Major + ".x.x.ini",
                version.Major + "." + version.Minor + ".x.ini",
                version.Major + "." + version.Minor + "." + version.Build + ".ini"
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
}

abstract class ApacheFailedException : Exception
{
	public ServiceControllerStatus status;

	ApacheFailedException(ServiceControllerStatus status)
	{
		this.status = status;
	}
}

class ApacheStartFailedException : Exception
{
	
}

class ApacheStopFailedException : Exception
{

}
