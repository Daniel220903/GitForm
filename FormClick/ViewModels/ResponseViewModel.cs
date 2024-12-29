namespace FormClick.ViewModels
{
    // En la carpeta ViewModels (por convención)
    public class ResponseViewModel
    {
        public int ResponseId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Description { get; set; }
        public string Topic { get; set; }
        public string picture { get; set; }
        public string userPicture { get; set; }
        public string userName { get; set; }
        public float Score { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<AnswerViewModel> Answers { get; set; }
    }

    public class AnswerViewModel
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string ResponseText { get; set; }
        public int? OptionId { get; set; }
        public int ResponseId { get; set; }
        public bool IsCorrect {  get; set; }
    }

}
