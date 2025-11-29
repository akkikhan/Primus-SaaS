using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace PrimusSaaS.Notifications.TestAgent.Scenarios;

public interface IScenario
{
    string Name { get; }
    string Description { get; }
    string Difficulty { get; } // Easy, Moderate, Hard, Complex
    Task RunAsync(IServiceProvider services);
}
