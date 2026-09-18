namespace RH_CM.Messages.Identity
{
    /// <summary>
    /// User-facing messages for the Identity feature (UsuariosMessages).
    /// </summary>
    public static class UsuariosMessages
    {
        public const string InvalidRequestUserIdIsRequired = "Invalid request: user id is required.";
        public const string UserNotFound = "User not found.";
        public const string InvalidRequestPleaseReviewTheSubmittedData = "Invalid request: please review the submitted data.";
        public const string InvalidRequestUserDataIsEmpty = "Invalid request: user data is empty.";
        public const string UsernameIsRequired = "Username is required.";
        public const string EmailIsRequired = "Email is required.";
        public const string FirstNameAndLastNameAreRequired = "First name and last name are required.";
        public const string YouMustSelectARoleToContinue = "You must select a role to continue.";
        public const string SelectedRoleDoesNotExist = "The selected role does not exist.";
        public const string UsernameAlreadyExistsPleaseChooseAnotherOne = "This username already exists. Please choose another one.";
        public const string EmailIsAlreadyInUse = "This email is already in use.";
        public const string CouldNotCreateTheUserFormat = "Could not create the user: {0}";
        public const string CouldNotAssignTheSelectedRoleFormat = "Could not assign the selected role: {0}";
        public const string UserCreatedAndRoleAssignedSuccessfully = "User created and role assigned successfully.";
        public const string InvalidRequestIncompleteUserData = "Invalid request: incomplete user data.";
        public const string CouldNotRemoveTheCurrentRoleFormat = "Could not remove the current role: {0}";
        public const string CouldNotAssignTheNewRoleFormat = "Could not assign the new role: {0}";
        public const string ChangesSavedSuccessfully = "Changes saved successfully.";
        public const string ErrorOccurredWhileSavingChangesFormat = "An error occurred while saving changes: {0}";
        public const string UserUnlockedSuccessfully = "User unlocked successfully.";
        public const string UserLockedSuccessfully = "User locked successfully.";
        public const string UserDeletedSuccessfully = "User deleted successfully.";
        public const string UserMarkedAsAvailable = "User marked as available.";
        public const string UserMarkedAsUnavailable = "User marked as unavailable.";
    }
}
