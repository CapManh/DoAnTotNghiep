using DoAnTotNghiep.Moldes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    
    public class HomeController : Controller
    {
        private Model1 db = new Model1();
        [HttpPost]
        public JsonResult Login(string Ten, string MatKhau)
        {
            try
            {
                if (string.IsNullOrEmpty(Ten) || string.IsNullOrEmpty(MatKhau))
                {
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ thông tin!" });
                }
                var user = db.NguoiDung.FirstOrDefault(x => x.Ten == Ten && x.MatKhau == MatKhau);

                if (user != null)
                {
                    Session["MaNguoiDung"] = user.MaNguoiDung;
                    Session["TenNguoiDung"] = user.Ten;
                    Session["VaiTro"] = user.MaVaiTro;

                    return Json(new { success = true, message = "Chào mừng " + user.Ten + " đã quay trở lại!" });
                }
                else
                {
                    return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult Register(NguoiDung model)
        {
            if (model == null || string.IsNullOrEmpty(model.Email))
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
            }

            try
            {
                var check = db.NguoiDung.FirstOrDefault(x => x.Email == model.Email);
                if (check != null)
                {
                    return Json(new { success = false, message = "Email này đã được sử dụng!" });
                }

                model.NgayTao = DateTime.Now;
                model.MaVaiTro = 2;

                db.NguoiDung.Add(model);
                db.SaveChanges();

                return Json(new { success = true, message = "Tạo tài khoản thành công." });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return Json(new { success = false, message = "Lỗi hệ thống: " + msg });
            }
        }
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("TrangChu", "Home");
        }
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
