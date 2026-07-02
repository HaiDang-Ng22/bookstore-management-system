using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Core;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class RolesController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        public ActionResult Index()
        {
            var roleService = new RoleService(db);
            roleService.EnsureRoleTableExists();
            var roles = roleService.GetAllRoles();
            return View(roles);
        }
    }
}
