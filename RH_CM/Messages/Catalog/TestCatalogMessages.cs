namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (TestCatalogMessages).
    /// </summary>
    public static class TestCatalogMessages
    {
        public const string TestNotFound = "Test not found.";
        public const string TestHasAlreadyBeenAnsweredByA = "This test has already been answered by a user. Please delete that record first.";
        public const string TestAndAllRelatedDataWereDeleted = "Test and all related data were deleted successfully.";
        public const string ErrorDeletingTestFormat = "Error deleting test: {0}";
        public const string ThereIsAlreadyATestActiveFor = "There is already a Test Active for this Course/Level, please disable it first.";
        public const string TestStatusUpdatedSuccessfully = "Test status updated successfully.";
        public const string NoTestIdWasProvided = "No test ID was provided.";
        public const string TestCouldNotBeFound = "The test could not be found.";
        public const string TestNotFoundInTheDatabase = "Test not found in the database.";
        public const string CourseLevelCombinationDoesntExistInCourseassignments = "This Course-Level combination doesn't exist in Courseassignments.";
        public const string CourseLevelCombinationAlreadyHasATest = "This Course-Level combination already has a Test Available, please disable it first.";
        public const string TestSuccessfullyUpdated = "Test Successfully Updated";
        public const string ConcurrencyErrorOccurredWhileUpdatingTheTest = "A concurrency error occurred while updating the test.";
        public const string UnexpectedErrorOccurredWhileUpdatingTheTest = "An unexpected error occurred while updating the test.";
        public const string TestQuestionsAndOptionsCreatedSuccessfully = "Test, questions, and options created successfully.";
        public const string ErrorSavingDataFormat = "Error saving data: {0}";
        public const string TestHasAlreadyBeenAnsweredByUsers = "This test has already been answered by users. Please disable this one, and create a new one.";
        public const string EachQuestionMustHaveAtLeastOne = "Each question must have at least one correct option.";
        public const string TestQuestionsUpdatedSuccessfully = "Test questions updated successfully.";
        public const string ErrorUpdatingQuestionsFormat = "Error updating questions: {0}";
    }
}
