namespace RH_CM.Service.Shared
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// EF Core's DbUpdateException.Message is always the generic "An error occurred while
        /// saving the entity changes. See the inner exception for details." - the actual SQL
        /// error (constraint violation, timeout, truncation, etc.) lives in InnerException,
        /// sometimes nested another level deep for SqlException. This walks down to the
        /// deepest inner exception so the real reason reaches the user instead of a dead end.
        /// </summary>
        public static string GetRootErrorMessage(Exception ex)
        {
            var current = ex;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }
            return current.Message;
        }
    }
}
