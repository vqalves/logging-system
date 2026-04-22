using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LogSystem.WebApp.Pages
{
    public class SearchLogsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public long LogCollectionId { get; set; }

        public void OnGet()
        {
        }
    }
}
