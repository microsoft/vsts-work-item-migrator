using System.Threading.Tasks;

namespace Common.Validation
{
    /// <summary>
    /// Validates data not related to work items
    /// </summary>
    public interface IConfigurationValidator
    {
        string Name { get; }

        Task Validate(IValidationContext context);
    }
}
