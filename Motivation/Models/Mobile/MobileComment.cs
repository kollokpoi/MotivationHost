using Microsoft.AspNetCore.Mvc;

namespace Motivation.Models.Mobile
{
    public class MobileComment
    {
        [FromForm(Name = "text")]
        public string Text { get; set; } = string.Empty;

        [FromForm(Name = "photo")]
        public IFormFile? Photo { get; set; } = null;

    }
}
