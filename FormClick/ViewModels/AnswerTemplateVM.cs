namespace FormClick.ViewModels
{
    public class AnswerTemplateVM
    {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ResponsedBYVM> Responses { get; set; } // Usamos ResponsedBYVM aquí
    }

    public class ResponsedBYVM
    {
        public int ResponseId { get; set; }
        public string UserName { get; set; }
        public float Score { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
