using LogSystem.Core.Caching;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogSystem.WebApp.Pages.LogCollections
{
    public class ManageModel : PageModel
    {
        private readonly LogSystemConfig _logSystemConfig;

        public ManageModel(LogSystemConfig logSystemConfig)
        {
            _logSystemConfig = logSystemConfig;
        }

        public int DefaultMaxLogsPerFile => _logSystemConfig.DefaultMaxLogsPerFile;

        public void OnGet()
        {
        }
    }
}
