namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (CourseCompletedMessages).
    /// </summary>
    public static class CourseCompletedMessages
    {
        public const string SelectACourseALevelAndAt = "Select a course, a level, and at least one person.";
        public const string AllSelectedPeopleAlreadyHaveTheCourse = "All selected people already have the course completed for that level.";
        public const string RecordsCreatedUpdatedSuccessfully = "Records created/updated successfully.";
        public const string SelectAtLeastOneRecordToDelete = "Select at least one record to delete.";
        public const string NoRecordsWereDeletedCheckYourSelection = "No records were deleted (check your selection and filters).";
        public const string OperationCompletedRowsAffectedFormat = "Operation completed. Rows affected: {0}.";
        public const string ErrorOccurredWhileBulkDeletingCompletedCoursesFormat = "An error occurred while bulk-deleting completed courses. Detail: {0}";
        public const string InvalidRecord = "Invalid record.";
        public const string RecordDoesNotExistOrHasAlready = "The record does not exist or has already been deleted.";
        public const string RecordDeletedSuccessfully = "Record deleted successfully.";
        public const string RecordCannotBeDeletedBecauseOfRelated = "The record cannot be deleted because of related database records.";
    }
}
