using System.Threading.Tasks;

namespace Common.Migration
{
    public interface IPhase1Processor : IProcessor
    {
        Task Process(IMigrationContext context);
    }
}
