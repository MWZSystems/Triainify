namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (PositionMessages).
    /// </summary>
    public static class PositionMessages
    {
        public const string PositionCreatedSuccessfully = "Position created successfully.";
        public const string ErrorOccurredWhileCreatingThePositionFormat = "An error occurred while creating the position: {0}";
        public const string SpecifiedPositionWasNotFound = "The specified position was not found.";
        public const string PositionNoLongerExistsInTheDatabase = "The position no longer exists in the database.";
        public const string PositionUpdatedSuccessfully = "Position updated successfully.";
        public const string ConcurrencyErrorOccurredWhileUpdatingThePosition = "A concurrency error occurred while updating the position. Please try again.";
        public const string ErrorOccurredWhileUpdatingThePositionFormat = "An error occurred while updating the position: {0}";
        public const string PositionDoesNotExistOrHasAlready = "The position does not exist or has already been deleted.";
        public const string PositionDeletedSuccessfully = "Position deleted successfully.";
    }
}
