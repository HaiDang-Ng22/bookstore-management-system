using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Areas.Admin.Constants;

namespace BookStoreOnline.Core
{
    public class AdminAuthorizeAttribute : ActionFilterAttribute
    {
        public Constants.AdminRole[] AllowedRoles { get; set; }

        public AdminAuthorizeAttribute(params Constants.AdminRole[] roles)
        {
            AllowedRoles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var session = filterContext.HttpContext.Session;
            var admin = session["TaiKhoan"] as NHANVIEN;

            if (admin == null)
            {
                // If it is an AJAX request, return a JSON response instead of a redirect
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { success = false, message = "Vui lòng đăng nhập với tài khoản quản trị." },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
                else
                {
                    // Redirect to login page
                    filterContext.Result = new RedirectResult("~/User/Login");
                }
                return;
            }

            // If roles are specified, check if the employee's role is in the allowed roles list
            if (AllowedRoles != null && AllowedRoles.Length > 0)
            {
                bool isAuthorized = AllowedRoles.Any(r => (int)r == admin.Quyen);
                if (!isAuthorized)
                {
                    if (filterContext.HttpContext.Request.IsAjaxRequest())
                    {
                        filterContext.Result = new JsonResult
                        {
                            Data = new { success = false, message = "Bạn không có quyền thực hiện thao tác này." },
                            JsonRequestBehavior = JsonRequestBehavior.AllowGet
                        };
                    }
                    else
                    {
                        // Set error message and redirect to Admin Home Page
                        filterContext.Controller.TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào chức năng này.";
                        filterContext.Result = new RedirectResult("~/Admin/Home_Page");
                    }
                }
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
