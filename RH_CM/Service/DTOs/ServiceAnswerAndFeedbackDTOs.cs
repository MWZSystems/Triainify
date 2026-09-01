using System.Collections.Generic;
using System.Data;

namespace RH_CM.Service.DTOs
{
    public class ServiceAnswerAndFeedbackDTOs
    {
        public ServiceAnswer? ServiceAnswer { get; set; }
        public byte[]? FeedbackFile { get; set; }
        public List<ExternalEvidenceFeedbackItemDTOs>? FeedbackItems { get; set; }
    }
}
