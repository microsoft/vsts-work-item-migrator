using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests.Validation
{
    [TestClass]
    public class ValidateFieldsTests
    {
        //Dictionary<string, FieldType> targetDictionary;
        //private IList<WorkItemField> sourceFieldList;
        //private IList<WorkItemField> targetFieldList;

        //[TestInitialize]
        //public void Initialize()
        //{
        //    targetDictionary = new Dictionary<string, FieldType>(StringComparer.OrdinalIgnoreCase) {
        //        {"Acceptance Criteria", FieldType.Html },
        //        { "Priority", FieldType.Integer},
        //        { "Issue", FieldType.String},
        //        { "Area", FieldType.TreePath },
        //        { "Opened Date", FieldType.DateTime },
        //        { "Url", FieldType.Html }
        //    };

        //    sourceFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Area", Type = FieldType.TreePath },
        //                                                new WorkItemField() {Name = "Acceptance Criteria", Type = FieldType.Html }
        //                                                };
        //    //clear the fields before every test run 
        //   MigrationContext.Fields.Clear();
        //}

        ///// <summary>
        ///// Check if a certain field exists in the target.. Correct field name and fieldtype 
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_RightNameRightType()
        //{
        //    bool expected = true;
        //    WorkItemField sourceField = new WorkItemField() { Name = "Area", Type = FieldType.TreePath };
        //    bool actual = ValidateFields.CheckIfFieldExists(targetDictionary, sourceField);
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Check if a certain field exists in the target.. Correct field name and fieldtype 
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_RightNameRightTypeIgnoreCase()
        //{
        //    bool expected = true;
        //    WorkItemField sourceField = new WorkItemField() { Name = "area", Type = FieldType.TreePath };
        //    bool actual = ValidateFields.CheckIfFieldExists(targetDictionary, sourceField);
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Field exists, but field type is wrong
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_RightNameWrongType()
        //{
        //    bool expected = false;
        //    WorkItemField sourceField = new WorkItemField() { Name = "Area", Type = FieldType.Boolean };
        //    bool actual = ValidateFields.CheckIfFieldExists(targetDictionary, sourceField);
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// field name does not exist and wrong field type 
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_WrongNameAndType()
        //{
        //    bool expected = false;
        //    WorkItemField sourceField = new WorkItemField() { Name = "ugh", Type = FieldType.Boolean };
        //    bool actual = ValidateFields.CheckIfFieldExists(targetDictionary, sourceField);
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// passing in a null workitemfield 
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_NullNameNullType()
        //{
        //    WorkItemField sourceField = null;
        //    Assert.ThrowsException<ValidationException>(() => ValidateFields.CheckIfFieldExists(targetDictionary, sourceField));
        //}

        ///// <summary>
        ///// passing in an empty workitemfield 
        ///// </summary>
        //[TestMethod]
        //public void CheckIfFieldExists_EmptyName()
        //{
        //    bool expected = false;
        //    WorkItemField sourceField = new WorkItemField() { Name = "", Type = FieldType.PlainText };
        //    bool actual = ValidateFields.CheckIfFieldExists(targetDictionary, sourceField);
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Matching fieldlist for both source and target - fields and field types
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_MatchingFieldLists()
        //{
        //    bool expected = true;
        //    targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Area", Type = FieldType.TreePath },
        //                                                new WorkItemField() {Name = "Acceptance Criteria", Type = FieldType.Html }
        //                                                };
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Matching fieldlist for both source and target - fields and field types
        ///// Case Insensitive test - target field names have different case
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_MatchingFieldListsIgnoreCase()
        //{
        //    bool expected = true;
        //    targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "area", Type = FieldType.TreePath },
        //                                                new WorkItemField() {Name = "acceptance Criteria", Type = FieldType.Html }
        //                                                };
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// TargetFields list does not contain all the field (present in source)
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_FieldNotPresentInTarget()
        //{
        //    bool expected = false;
        //    IList<WorkItemField> targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Acceptance Criteria", Type = FieldType.Html }
        //                                                };

        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Fields are present in target, but one attribute is wrong
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_FieldsPresentAttributeWrong()
        //{
        //    targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Area", Type = FieldType.Boolean},
        //                                                new WorkItemField() {Name = "Acceptance Criteria", Type = FieldType.Html }
        //                                                };
        //    bool expected = false;
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Fields are present in targetList but attributes are not set. This is an interesting test
        ///// because since FieldType is enum, it automatically defaults to string
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_NoAttributeSetinTarget()
        //{
        //    targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Area"},
        //                                                new WorkItemField() {Name = "Acceptance Criteria" }
                                                        };
        //    bool expected = false;
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}


        ///// <summary>
        ///// Source Field list exists, but target list empty
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_TargetListEmpty()
        //{
        //    targetFieldList = new List<WorkItemField>();
        //    bool expected = false;
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Both source and field target list are empty. Technically, we will return false in the actual code 
        ///// (since CheckFieldFidelity will check this condition
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_SourceAndTargetEmpty()
        //{
        //    sourceFieldList = new List<WorkItemField>();
        //    targetFieldList = new List<WorkItemField>();
        //    bool expected = true; //since both are empty
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Source is empty but target list is empty. Although this code returns true in thie case, the actual code will 
        ///// return false since CheckFieldFidelity will check this condition
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_SourceEmptyTargetPresent()
        //{
        //    sourceFieldList = new List<WorkItemField>();
        //    targetFieldList = new List<WorkItemField>() {
        //                                                new WorkItemField() {Name = "Area"},
        //                                                new WorkItemField() {Name = "Acceptance Criteria" }
        //                                                };
        //    bool expected = true; //since source is empty, nothing to compare, Functionally we will return false (as CheckFieldFidelity checks this state)
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    bool actual = vf.CompareFields();
        //    Assert.AreEqual(expected, actual);
        //}

        ///// <summary>
        ///// Target Field list are null
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_TargetNullCheck()
        //{
        //    targetFieldList = null;
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    Assert.ThrowsException<ValidationException>(() => vf.CompareFields());
        //}

        ///// <summary>
        ///// Source and Target Field list are null
        ///// </summary>
        //[TestMethod]
        //public void CompareFields_SourceAndTargetNullCheck()
        //{
        //    sourceFieldList = null;
        //    targetFieldList = null;
        //    ValidateFields vf = new ValidateFields(sourceFieldList, targetFieldList);
        //    Assert.ThrowsException<ValidationException>(() => vf.CompareFields());
        //}
    //}
}
