using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult TrangChu()
        {
            return View();
        }
        public ActionResult GioiThieu()
        {
            return View();
        }
    }
}