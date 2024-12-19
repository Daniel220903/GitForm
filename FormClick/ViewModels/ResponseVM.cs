namespace FormClick.ViewModels
{
    public class ResponseVM
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Topic { get; set; }
        public List<QuestionVM> Questions { get; set; }
    }

    public class QuestionVM
    {
        public int QuestionId { get; set; }
        public string Text { get; set; }
        public string Type { get; set; }
        public List<OptionVM> Options { get; set; }
        public string SelectedAnswer { get; set; }
        public int? SelectedOptionId { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class OptionVM
    {
        public int OptionId { get; set; }
        public string Text { get; set; }
    }
}
