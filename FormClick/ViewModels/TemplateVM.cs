namespace FormClick.ViewModels
{
    public class TemplateVM
    {
        public int TemplateId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Topic { get; set; }
        public bool IsPublic { get; set; }
        public List<QuestionViewModel> Questions { get; set; }
    }

    public class QuestionViewModel
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public List<OptionViewModel> Options { get; set; }
    }

    public class OptionViewModel
    {
        public int OptionId { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}
