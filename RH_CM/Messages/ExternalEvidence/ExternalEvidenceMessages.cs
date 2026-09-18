namespace RH_CM.Messages.ExternalEvidence
{
    /// <summary>
    /// User-facing messages for the ExternalEvidence feature (ExternalEvidenceMessages).
    /// </summary>
    public static class ExternalEvidenceMessages
    {
        public const string RecordNotFound = "Record not found.";
        public const string RecordToggledSuccessfully = "Record Toggled successfully";
        public const string ToggleFailed = "Toggle Failed";
        public const string FileEmptyOrPdfFormatNotCorrect = "File Empty or .PDF format not correct";
        public const string RecordCouldNotBeUpdatedPleaseTry = "The record could not be updated. Please try again or contact support.";
        public const string EvidenceWasUpdatedSuccessfully = "The evidence was updated successfully.";
        public const string RecordSuccessfullyDeletedFormat = "Record {0} Successfully Deleted";
        public const string RecordCouldNotBeDeleted = "Record could not be deleted";
        public const string SelectAValidCourse = "Please select a valid course.";
        public const string SelectAValidLevel = "Please select a valid level.";
        public const string CourseDoesNotHaveExternalLevelFormat = "The course '{0}' does not have a '{1}' level with External delivery type in Course Assignments.";
        public const string EvidenceAddedToEverySelectedRecord = "The evidence was successfully added to every selected record.";
        public const string MustAttachAnEvidenceFile = "You must attach an evidence file.";
        public const string FileMustBeInPdfFormat = "The file must be in PDF format.";
        public const string AttachedFileIsEmpty = "The attached file is empty.";
        public const string FileSizeMustNotExceed10Mb = "The file size must not exceed 10 MB.";
        public const string EvidenceIdentifierIsNotValid = "The evidence identifier is not valid.";
    }
}
