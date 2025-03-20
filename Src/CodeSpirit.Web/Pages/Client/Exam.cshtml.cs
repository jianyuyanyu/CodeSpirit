using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CodeSpirit.Web.Pages.Client;

public class ExamModel : PageModel
{
    public IActionResult OnGet(long id)
    {
        //if (id <= 0)
        //{
        //    return RedirectToPage("/client/index");
        //}
        
        return Page();
    }
} 