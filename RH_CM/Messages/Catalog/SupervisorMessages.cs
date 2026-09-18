namespace RH_CM.Messages.Catalog
{
    /// <summary>
    /// User-facing messages for the Catalog feature (SupervisorMessages).
    /// </summary>
    public static class SupervisorMessages
    {
        public const string SupervisorWithTheSameHeadcountAndDepartment = "A supervisor with the same Headcount and Department already exists and is active.";
        public const string SupervisorCreatedSuccessfully = "Supervisor created successfully.";
        public const string ErrorOccurredWhileCreatingTheSupervisorFormat = "An error occurred while creating the supervisor: {0}";
        public const string SupervisorNotFound = "Supervisor not found.";
        public const string AnotherActiveSupervisorWithTheSameHeadcount = "Another active supervisor with the same Headcount and Department already exists.";
        public const string SupervisorNoLongerExists = "Supervisor no longer exists.";
        public const string SupervisorUpdatedSuccessfully = "Supervisor updated successfully.";
        public const string ErrorOccurredWhileUpdatingTheSupervisorFormat = "An error occurred while updating the supervisor: {0}";
        public const string SupervisorCannotBeEnabledBecauseAnotherActive = "This supervisor cannot be enabled because another active supervisor with the same Headcount and Department already exists.";
        public const string SupervisorStatusUpdatedSuccessfully = "Supervisor status updated successfully.";
        public const string SupervisorDoesNotExistOrHasAlready = "The supervisor does not exist or has already been deleted.";
        public const string SupervisorDeletedSuccessfully = "Supervisor deleted successfully.";
    }
}
