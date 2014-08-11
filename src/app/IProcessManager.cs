using System.Threading.Tasks;

namespace PhpVersionSwitcher
{
	internal interface IProcessManager
	{
		string Name { get; }
		bool IsRunning();
		Task Start();
		Task Stop();
		Task Restart();
	}
}
