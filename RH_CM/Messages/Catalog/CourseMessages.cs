namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (CourseMessages).
    /// </summary>
    public static class CourseMessages
    {
        public const string ManagementSystemFieldIsRequired = "The Management System field is required.";
        public const string CourseIdFieldIsRequired = "The Course ID field is required.";
        public const string CourseNameFieldIsRequired = "The Course Name field is required.";
        public const string RevisionMustBeGreaterThan0 = "The Revision must be greater than 0.";
        public const string CourseIdAndCourseNameAlreadyExist = "The Course ID and Course Name already exist. Please choose different values.";
        public const string CourseIdAlreadyExistsPleaseChooseA = "The Course ID already exists. Please choose a different ID.";
        public const string CourseNameAlreadyExistsPleaseChooseA = "The Course Name already exists. Please choose a different name.";
        public const string CourseCreatedSuccessfully = "Course created successfully.";
        public const string ErrorOccurredWhileCreatingTheCourseFormat = "An error occurred while creating the course: {0}";
        public const string SpecifiedCourseWasNotFound = "The specified course was not found.";
        public const string CourseValidityDaysMustBeGreaterThan = "The Course Validity Days must be greater than 0.";
        public const string CourseNoLongerExistsInTheDatabase = "The course no longer exists in the database.";
        public const string CourseUpdatedSuccessfully = "Course updated successfully.";
        public const string ConcurrencyErrorOccurredWhileUpdatingTheCourse = "A concurrency error occurred while updating the course.";
        public const string ErrorOccurredWhileUpdatingTheCourseFormat = "An error occurred while updating the course: {0}";
        public const string SelectedCourseWasNotFound = "The selected course was not found.";
        public const string CourseAndAllRelatedCourseAssignmentsWere = "The course and all related course assignments were enabled successfully.";
        public const string CourseWasEnabledSuccessfully = "The course was enabled successfully.";
        public const string CourseAndAllRelatedCourseAssignmentsWereDisabled = "The course and all related course assignments were disabled successfully.";
        public const string CourseWasDisabledSuccessfully = "The course was disabled successfully.";
        public const string ErrorOccurredWhileUpdatingTheCourseAvailabilityFormat = "An error occurred while updating the course availability: {0}";
        public const string CourseCannotBeDeletedBecauseItIs = "The course cannot be deleted because it is linked to assignments. Please unlink it first.";
        public const string CourseDoesNotExistOrHasAlready = "The course does not exist or has already been deleted.";
        public const string CourseDeletedSuccessfully = "Course deleted successfully.";
    }
}
