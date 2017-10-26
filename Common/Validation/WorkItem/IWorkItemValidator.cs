using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Common.Validation
{
    /// <summary>
    /// Validates anything work item related
    /// </summary>
    public interface IWorkItemValidator
    {
        /// <summary>
        /// The name of the validator to use for logging
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Populates the context with any data required for validation
        /// </summary>
        /// <param name="context"></param>
        Task Prepare(IValidationContext context);

        /// <summary>
        /// Validates the work item
        /// </summary>
        Task Validate(IValidationContext context, WorkItem workItem);
    }
}
