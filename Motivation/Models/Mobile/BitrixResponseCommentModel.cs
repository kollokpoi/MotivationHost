namespace Motivation.Models.Mobile
{
    public class BitrixResponseCommentModel
    {
        public int AuthorId { get; set; }
        public string PostMessage { get; set; } = string.Empty;
        public string PostDate { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}
