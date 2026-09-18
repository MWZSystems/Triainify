namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (DepartmentMessages).
    /// </summary>
    public static class DepartmentMessages
    {
        public const string DepartmentNameAlreadyExistsPleaseChooseA = "The department name already exists. Please choose a different name.";
        public const string DepartmentCreatedSuccessfully = "Department created successfully.";
        public const string ErrorOccurredWhileCreatingTheDepartmentFormat = "An error occurred while creating the department: {0}";
        public const string DepartmentNameAlreadyExists = "The department name already exists.";
        public const string DepartmentUpdatedSuccessfully = "Department updated successfully.";
        public const string ConcurrencyErrorOccurredWhileUpdatingTheDepartment = "A concurrency error occurred while updating the department. Please try again.";
        public const string DepartmentDoesNotExistOrHasAlready = "The department does not exist or has already been deleted.";
        public const string DepartmentDeletedSuccessfully = "Department deleted successfully.";
    }
}
