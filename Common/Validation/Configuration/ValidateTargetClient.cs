using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.Logging;

namespace Common.Validation
{
    [RunOrder(2)]
    public class ValidateTargetPermissions : IConfigurationValidator
    {
        private ILogger Logger { get; } = MigratorLogging.CreateLogger<ValidateTargetPermissions>();

        public string Name => "Target account permissions";

        public async Task Validate(IValidationContext context)
        {
            await ValidationHelpers.CheckBypassRulesPermission(context.TargetClient, context.Config.TargetConnection.Project);
        }
    }
}
