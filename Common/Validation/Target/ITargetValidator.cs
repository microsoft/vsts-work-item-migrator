using System.Threading.Tasks;

namespace Common.Validation
{
    public interface ITargetValidator
    {
        string Name { get; }

        Task Validate(IValidationContext context);
    }
}
