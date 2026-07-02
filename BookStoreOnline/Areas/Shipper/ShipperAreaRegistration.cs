using System.Web.Mvc;

namespace BookStoreOnline.Areas.Shipper
{
    public class ShipperAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Shipper";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Shipper_default",
                "Shipper/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
