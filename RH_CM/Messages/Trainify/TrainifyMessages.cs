namespace RH_CM.Messages.Trainify
{
    /// <summary>
    /// User-facing messages for the Trainify feature (TrainifyMessages).
    /// </summary>
    public static class TrainifyMessages
    {
        public const string SelectAValidPosition = "Please select a valid position.";
        public const string UnableToRetrieveTheCurrentUser = "Unable to retrieve the current user.";
        public const string SqlErrorWhileQueryingDataFormat = "SQL error while querying data: {0}";
        public const string ErrorOccurredFormat = "An error occurred: {0}";
        public const string RecordCreatedSuccessfully = "Record created successfully.";
        public const string RecordUpdatedSuccessfully = "Record updated successfully.";
        public const string RecordDeletedSuccessfully = "Record deleted successfully.";
        public const string SelectAnExcelFile = "Please select an Excel file.";
        public const string ErrorReadingTheExcelFileFormat = "Error reading the Excel file: {0}";
        public const string ErrorProcessingTheUploadFormat = "Error processing the upload: {0}";
        public const string NoDiagnosticTestWasFoundForThis = "No diagnostic test was found for this course/level. Please contact HR.";
        public const string NoCourseMaterialPdfUrlIsLinked = "No course material (PDF/URL) is linked to this course/level. Please contact HR.";
        public const string DiagnosticTestNotFound = "Diagnostic test not found.";
        public const string ErrorLoadingCoursesFormat = "Error loading courses: {0}";
        public const string YouMustCompleteTheDiagnosticTestBefore = "You must complete the diagnostic test before opening the material.";
        public const string NoCourseMaterialIsLinkedToThis = "No course material is linked to this course/level, or it is unavailable. Please contact HR.";
        public const string MaterialExistsButHasNoFileOr = "The material exists but has no file or URL. Please contact HR.";
        public const string DiagnosticSubmittedReviewYourResultsBelow = "Diagnostic submitted. Review your results below.";
        public const string CourseAndLevelAreRequiredToOpen = "Course and level are required to open the final exam.";
        public const string YouMustCompleteTheDiagnosticTestBeforeTaking = "You must complete the diagnostic test before taking the final exam.";
        public const string TestNotFoundOrNotAvailable = "Test not found or not available.";
        public const string TestHasNoQuestionsPleaseContactHr = "This test has no questions. Please contact HR.";
        public const string ExamNotFoundOrNotAvailable = "Exam not found or not available.";
        public const string ExamHasNoQuestionsPleaseContactHr = "This exam has no questions. Please contact HR.";
        public const string ThereAreNoAnswersToSubmit = "There are no answers to submit.";
        public const string SelectAtLeastOneOptionBeforeSubmitting = "Please select at least one option before submitting.";
        public const string AnswerEveryQuestionBeforeSubmitting = "Please answer every question before submitting. Your exam has not been recorded.";
        public const string UnableToResolveTheCurrentUsersEmployee = "Unable to resolve the current user's employee (missing EmployeeNumber).";
        public const string EmployeeNumberIsNotAValidNumber = "EmployeeNumber is not a valid number.";
        public const string EmployeeHeadcountAssociatedWithTheCurrentUser = "The employee (Headcount) associated with the current user does not exist or is not available.";
        public const string ErrorSavingDiagnosticFormat = "Error saving diagnostic: {0}";
        public const string MinimumScoreAttemptRecordedFormat = "Minimum score is {0}. This exam attempt was recorded as PENDING; the course was not marked as completed.";
        public const string MissingCourseAssignmentPleaseContactYourProvider = "Missing course assignment. Please contact your provider or IT support.";
        public const string ErrorSavingExamFormat = "Error saving exam: {0}";
        public const string OnlyAdministratorsCanLookUpAnotherEmployeesMatrix = "Only administrators can look up another employee's matrix.";
        public const string PleaseEnterAnEmployeeNumberToSearch = "Please enter an employee number to search.";
        public const string NoUserFoundForThatEmployeeNumberFormat = "No user found for employee number '{0}'.";
    }
}
