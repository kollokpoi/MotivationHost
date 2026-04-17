namespace Motivation.Models.Mobile
{
    public class MobileResponseCommentModel
    {
        public string text { get; set; } = string.Empty;
        public string photo { get; set; } = string.Empty;
        public DateTime created { get; set; }
        public MobileResponseCommentEmployeeModel? author { get; set; }
    }

    public class MobileResponseCommentEmployeeModel
    {
        public string name { get; set; } = string.Empty;
        public string photo { get; set; } = string.Empty;
    }
}
