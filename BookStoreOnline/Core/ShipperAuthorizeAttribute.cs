using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Areas.Admin.Constants;

namespace BookStoreOnline.Core
{
    public class ShipperAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var shipper = filterContext.HttpContext.Session["TaiKhoan"] as NHANVIEN;

            if (shipper == null)
            {
                filterContext.Result = new RedirectResult("~/User/Login");
                return;
            }

            if (shipper.Quyen != (int)Constants.AdminRole.Shipper)
            {
                if (shipper.Quyen == (int)Constants.AdminRole.Admin)
                {
                    filterContext.Result = new RedirectResult("~/Admin/Home_Page");
                }
                else
                {
                    filterContext.Result = new RedirectResult("~/User/Login");
                }
                return;
            }

            if (!(shipper.TrangThai ?? false))
            {
                filterContext.Controller.TempData["ErrorMessage"] = "Tài khoản shipper đã bị khóa.";
                filterContext.Result = new RedirectResult("~/User/Login");
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
