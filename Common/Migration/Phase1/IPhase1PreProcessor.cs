using System.Threading.Tasks;

namespace Common.Migration
{
    public interface IPhase1PreProcessor : IProcessor
    {
        Task Prepare(IMigrationContext context);

        Task Process(IBatchMigrationContext batchContext);
    }
}