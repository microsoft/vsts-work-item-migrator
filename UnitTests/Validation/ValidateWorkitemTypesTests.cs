using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Common.Validation;

namespace UnitTests.Validation
{
    [TestClass]
    public class ValidateWorkItemTypesTests
    {
        /// <summary>
        /// Work item fields exist on both source and target for a certain work item type
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_SameSourceAndTargetWorkItemTypesAndFieldsTest()
        {
            bool expected = true;
            
            ISet<string> sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id", "System.AreaPath" };
            ISet<string> targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id", "System.AreaPath", "Priority" };

            IValidationContext context = new ValidationContext();
            context.SourceFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.SourceFields.TryAdd("System.AreaPath", new WorkItemField() { ReferenceName = "Area", Type = FieldType.TreePath });
            context.SourceFields.TryAdd("Acceptance Criteria", new WorkItemField() { ReferenceName = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.TargetFields.TryAdd("System.AreaPath", new WorkItemField() { ReferenceName = "Area", Type = FieldType.TreePath });
            context.TargetFields.TryAdd("Acceptance Criteria", new WorkItemField() { ReferenceName = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Title", new WorkItemField() { ReferenceName = "System.Title", Type = FieldType.String });

            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
       }

        /// <summary>
        /// Work item fields exist on both source and target but the case is all different
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_SameSourceAndTargetWorkItemTypesAndFieldsIgnoreCaseTest()
        {
            bool expected = true;

            ISet<string> sourceFields = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "System.Id", "System.AREAPATH" };
            ISet<string> targetFields = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { "System.id", "System.areapath" };

            IValidationContext context = new ValidationContext();
            context.SourceFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.SourceFields.TryAdd("System.AreaPath", new WorkItemField() { ReferenceName = "Area", Type = FieldType.TreePath });
            context.SourceFields.TryAdd("Acceptance Criteria", new WorkItemField() { ReferenceName = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("SYSTEM.ID", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.TargetFields.TryAdd("system.areapath", new WorkItemField() { ReferenceName = "Area", Type = FieldType.TreePath });
            context.TargetFields.TryAdd("Acceptance Criteria", new WorkItemField() { ReferenceName = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Title", new WorkItemField() { ReferenceName = "System.Title", Type = FieldType.String });

            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Target is missing a work item field, system.areapath is present in source but not in target
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_TargetMissingAFieldTest()
        {
            bool expected = false;

            ISet<string> sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id", "System.AreaPath" };
            ISet<string> targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id"};

            IValidationContext context = new ValidationContext();
            context.SourceFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.SourceFields.TryAdd("System.AreaPath", new WorkItemField() { Name = "Area", Type = FieldType.TreePath });
            context.SourceFields.TryAdd("Acceptance Criteria", new WorkItemField() { Name = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.TargetFields.TryAdd("Acceptance Criteria", new WorkItemField() { Name = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Title", new WorkItemField() { Name = "System.Title", Type = FieldType.String });

            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Target has the field but the type is wrong - system.id has a fieldtype of int in source and string in target
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_TargetFieldDifferentFieldTypeTest()
        {
            bool expected = false;
            ISet<string> sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id"};
            ISet<string> targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id" };

            IValidationContext context = new ValidationContext();
            context.SourceFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.Integer });
            context.SourceFields.TryAdd("System.AreaPath", new WorkItemField() { Name = "Area", Type = FieldType.TreePath });
            context.SourceFields.TryAdd("Acceptance Criteria", new WorkItemField() { Name = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Id", new WorkItemField { ReferenceName = "System.Id", Type = FieldType.String });
            context.TargetFields.TryAdd("Acceptance Criteria", new WorkItemField() { Name = "Acceptance Criteria", Type = FieldType.Html });
            context.TargetFields.TryAdd("System.Title", new WorkItemField() { Name = "System.Title", Type = FieldType.String });

            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Target has the field but the type is wrong - system.id has a fieldtype of int in source and string in target
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_TargetEmptyTest()
        {
            bool expected = false;
            ISet<string> sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "System.Id" };
            ISet<string> targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IValidationContext context = new ValidationContext();

            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Target has the field but the type is wrong - system.id has a fieldtype of int in source and string in target
        /// </summary>
        [TestMethod]
        public void CompareWorkItemType_SourceAndTargetEmptyTest()
        {
            bool expected = false;
            ISet<string> sourceFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ISet<string> targetFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            IValidationContext context = new ValidationContext();
            ValidateWorkItemTypes instance = new ValidateWorkItemTypes();
            bool actual = instance.CompareWorkItemType(context, "Bug", sourceFields, targetFields);
            Assert.AreEqual(expected, actual);
        }
    }
}
