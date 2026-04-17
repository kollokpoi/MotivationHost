using Motivation.Models;

namespace Motivation.ViewModels
{
    public class BitrixSettingsViewModel
    {
        public BitrixSettings Settings { get; set; } = new();
        public List<BitrixPortal> Portals { get; set; } = new();
    }
}
