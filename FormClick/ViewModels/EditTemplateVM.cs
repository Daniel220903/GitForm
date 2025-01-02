namespace FormClick.ViewModels
{
    public class EditTemplateVM{
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string picture { get; set; }
        public int Version { get; set; }
        public string Topic { get; set; }
        public List<QuestionViewModelEdit> Questions { get; set; }
    }
    public class QuestionViewModelEdit {
        public int Id { get; set; }
        public string Text { get; set; }
        public string QuestionType { get; set; }
        public string openAnswer { get; set; }
        public List<OptionViewModelEdit> Options { get; set; }
    }

    public class OptionViewModelEdit {
        public int Id { get; set; }
        public string OptionText { get; set; }

        public bool IsCorrect { get; set; }
    }
}