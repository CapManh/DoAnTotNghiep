using DoAnTotNghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    
    public class HomeController : Controller
    {
        private Model1 db = new Model1();
        private void LoadMenu()
        {
            ViewBag.DanhMucMenu = db.DanhMuc.ToList();
        }
        public ActionResult TrangChu()
        {
            LoadMenu();
            return View();
        }
        public ActionResult GioiThieu()
        {
            LoadMenu();
            return View();
        }
        public ActionResult sanpham()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSTT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSQRT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSMH()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSGH()
        {
            LoadMenu();
            return View();
        }
        public ActionResult CSDT()
        {
            LoadMenu();
            return View();
        }
        public ActionResult LienHe()
        {
            LoadMenu();
            return View();
        }
        public ActionResult TinTuc()
        {
            LoadMenu();
            return View();
        }
        public ActionResult GioHang()
        {
            LoadMenu();
            return View();
        }
    }
}
