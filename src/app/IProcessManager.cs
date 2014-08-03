using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	interface IProcessManager
	{
		bool IsRunning();
		Task Start();
		Task Stop();
		Task Restart();
	}
}
