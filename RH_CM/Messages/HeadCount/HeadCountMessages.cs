namespace RH_CM.Messages.HeadCount
{
    /// <summary>
    /// User-facing messages for the HeadCount feature (HeadCountMessages).
    /// </summary>
    public static class HeadCountMessages
    {
        public const string HeadCountCreatedSuccessfully = "Head count created successfully.";
        public const string HeadCountAddedButUserCouldNotBe = "HeadCount Added, but user could not be created.";
        public const string ErrorOccurredWhileCreatingTheHeadCountFormat = "An error occurred while creating the head count: {0}";
        public const string RecordWasNotFound = "The record was not found.";
        public const string RecordHasBeenSuccessfullyUpdated = "The record has been successfully updated.";
        public const string ErrorOccurredWhileUpdatingTheHeadCountFormat = "An error occurred while updating the head count: {0}";
        public const string InvalidHeadCountIdentifier = "Invalid head count identifier.";
        public const string HeadCountDeletedSuccessfully = "Head count deleted successfully.";
        public const string HeadCountNotFound = "Head count not found.";
        public const string ConcurrencyErrorWhileUpdatingTheHeadCount = "Concurrency error while updating the head count.";
        public const string UnexpectedErrorWhileUpdatingTheHeadCountFormat = "Unexpected error while updating the head count: {0}";
    }
}
