namespace RH_CM.Service.DTOs
{
    public class ServiceAnswer
    {
        public bool Success { get; set; }
        public string? MessageType { get; set; }
        public string? Message { get; set; }

        public ServiceAnswer(bool Success = false, string MessageType = "", string Message = "") { 
        
            this.Success = Success;
            this.MessageType = MessageType;
            this.Message = Message;
        }

    }
}
