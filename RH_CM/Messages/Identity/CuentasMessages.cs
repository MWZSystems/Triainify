namespace RH_CM.Messages.Identity
{
    /// <summary>
    /// User-facing messages for the Identity feature (CuentasMessages).
    /// </summary>
    public static class CuentasMessages
    {
        public const string UserRegisteredSuccessfully = "User registered successfully.";
        public const string ThereIsAProblemWithTheDatabase = "There is a problem with the database structure (missing table). Please contact IT support.";
        public const string UnableToConnectToTheDatabasePlease = "Unable to connect to the database. Please try again later or contact IT support.";
        public const string UnexpectedErrorOccurredWhileRegisteringTheUser = "An unexpected error occurred while registering the user. If the problem persists, please contact IT support.";
        public const string EmployeeUserCreatedSuccessfully = "Employee user created successfully.";
        public const string LoginSuccessfulWelcomeBack = "Login successful. Welcome back!";
        public const string AccountIsLockedPleaseContactItSupport = "Your account is locked. Please contact IT support.";
        public const string InvalidUsernameOrPassword = "Invalid username or password.";
        public const string YouHaveBeenSignedOut = "You have been signed out.";
        public const string UserDoesNotExist = "User does not exist.";
        public const string PasswordChangedSuccessfully = "Password changed successfully.";
        public const string UnexpectedErrorOccurredWhileTryingToReset = "An unexpected error occurred while trying to reset the password. If the problem persists, please contact IT support.";
        public const string YouDoNotHavePermissionToAccess = "You do not have permission to access this section.";
        public const string ThereIsAProblemWithTheDatabaseStructureSqlError208 = "There is a problem with the database structure (missing table, SQL error 208). Please contact IT support.";
        public const string DatabaseServerCouldNotBeFoundOrIsNotReachable = "The database server could not be found or is not reachable (SQL error 53). Please try again later or contact IT support.";
        public const string ConfiguredDatabaseAccountDoesNotHavePermission = "The configured database account does not have permission to access the database or the credentials are invalid. Please contact IT support.";
        public const string ConfiguredDatabaseCouldNotBeFoundOrOpened = "The configured database could not be found or opened (SQL error 4060). Please contact IT support.";
        public const string UnexpectedErrorOccurredWhileTryingToSignIn = "An unexpected error occurred while trying to sign in. If the problem persists, please contact IT support.";
        public const string ApplicationCouldNotConnectToTheDatabase = "The application could not establish a connection to the database. Please try again later or contact IT support.";
        public const string DatabaseServerNotReachableVerifyInstance = "The database server could not be found or is not reachable (SQL error 53). Please verify the SQL Server instance or contact IT support.";
        public const string DatabaseAccountNoPermissionDetailedFormat = "The account configured for the database connection (IIS application pool identity or the user in the 'ConexionSQL' connection string) does not have permission to access the database or the credentials are invalid (SQL error {0}). Please verify the database login configuration or contact IT support.";
        public const string ConfiguredDatabaseNotFoundVerifyNameSqlError4060 = "The configured database could not be found or opened (SQL error 4060). Please verify the database name and that the configured account has access to it, or contact IT support.";
        public const string DatabaseErrorLoadingTheLoginPageFormat = "A database error occurred while loading the login page (SQL error {0}). Please try again later or contact IT support.";
        public const string UnexpectedErrorOccurredWhileLoadingTheLoginPage = "An unexpected error occurred while loading the login page. If the problem persists, please contact IT support.";
        public const string ConfiguredDatabaseNotFoundVerifyExistsSqlError4060 = "The configured database could not be found or opened (SQL error 4060). Please verify the database exists and that the configured account has access to it, or contact IT support.";
        public const string UnableToConnectToTheDatabaseSqlErrorFormat = "Unable to connect to the database (SQL error {0}). Please try again later or contact IT support.";
        public const string AccountUnlockedSuccessfully = "Account unlocked successfully.";
        public const string UnexpectedErrorOccurredWhileTryingToUnlock = "An unexpected error occurred while trying to unlock the account. If the problem persists, please contact IT support.";
    }
}
