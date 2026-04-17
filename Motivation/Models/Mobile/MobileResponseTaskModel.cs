namespace Motivation.Models.Mobile
{
    public class MobileResponseTaskModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? DeadLine { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public MobileResponseEmployeeModel? Author { get; set; }
    }

    public class MobileResponseEmployeeModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Photo { get; set; } = string.Empty;
    }
}
