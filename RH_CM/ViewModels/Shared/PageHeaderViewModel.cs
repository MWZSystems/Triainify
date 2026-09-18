namespace RH_CM.ViewModels
{
    /// <summary>
    /// Model for the _PageHeader partial: title, icon, and contextual help for a page.
    /// </summary>
    public class PageHeaderViewModel
    {
        public string Title { get; set; } = string.Empty;

        /// <summary>Font Awesome class without the "fas" prefix, e.g. "fa-book".</summary>
        public string IconClass { get; set; } = string.Empty;

        /// <summary>Help modal title; defaults to <see cref="Title"/> when not set.</summary>
        public string? HelpTitle { get; set; }

        /// <summary>Short bullet points describing what this screen lets the user do.</summary>
        public List<string> HelpBullets { get; set; } = new();
    }
}
