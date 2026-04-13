using DoAnTotNghiep.Moldes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BCrypt.Net;
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
                var user = db.NguoiDung.FirstOrDefault(x => x.Ten == Ten);

                if (user != null)
                {
                    bool isPasswordCorrect = false;

                    try
                    {
                        isPasswordCorrect = BCrypt.Net.BCrypt.Verify(MatKhau, user.MatKhau);
                    }
                    catch
                    {
                        isPasswordCorrect = (user.MatKhau == MatKhau);
                    }

                    if (isPasswordCorrect)
                    {
                        Session["MaNguoiDung"] = user.MaNguoiDung;
                        Session["TenNguoiDung"] = user.Ten;
                        Session["VaiTro"] = user.MaVaiTro;
                        Session["Email"] = user.Email;
                        Session["SDT"] = user.SoDienThoai;
                        Session["DiaChi"] = user.DiaChi;

                        return Json(new { success = true, message = "Chào mừng " + user.Ten + " đã đến với Website của tôi!" });
                    }
                }

                return Json(new { success = false, message = "Tên đăng nhập hoặc mật khẩu không chính xác." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdateProfile(string Ten, string Email, string SoDienThoai, string DiaChi)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
                }

                int maND = int.Parse(Session["MaNguoiDung"].ToString());
                var user = db.NguoiDung.SingleOrDefault(x => x.MaNguoiDung == maND);

                if (user != null)
                {
                    user.Ten = Ten;
                    user.Email = Email;
                    user.SoDienThoai = SoDienThoai;
                    user.DiaChi = DiaChi;
                    db.SaveChanges();

                    Session["TenNguoiDung"] = Ten;
                    Session["Email"] = Email;
                    Session["SDT"] = SoDienThoai;
                    Session["DiaChi"] = DiaChi;

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Không tìm thấy người dùng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [HttpPost]
        public JsonResult UpdatePassword(string OldPass, string NewPass)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                int maND = int.Parse(Session["MaNguoiDung"].ToString());
                var user = db.NguoiDung.Find(maND);

                if (user != null)
                {
                    bool isOldPassCorrect = false;

                    try
                    {
                        isOldPassCorrect = BCrypt.Net.BCrypt.Verify(OldPass, user.MatKhau);
                    }
                    catch
                    {
                        isOldPassCorrect = (user.MatKhau == OldPass);
                    }

                    if (!isOldPassCorrect)
                    {
                        return Json(new { success = false, message = "Mật khẩu hiện tại không đúng!" });
                    }
                    user.MatKhau = BCrypt.Net.BCrypt.HashPassword(NewPass);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Lỗi xác thực!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
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
            var banners = db.Banner
                .Where(b => b.TrangThai == true
                         && b.NgayBatDau <= DateTime.Now
                         && b.NgayKetThuc >= DateTime.Now)
                .OrderBy(b => b.ThuTu)
                .ToList();
            ViewBag.BannerTopLeft = banners.FirstOrDefault(b => b.ThuTu == 1);
            ViewBag.BannerTopRight = banners.FirstOrDefault(b => b.ThuTu == 2);
            ViewBag.BannerBottomLeft = banners.FirstOrDefault(b => b.ThuTu == 3);
            ViewBag.BannerBottomRight = banners.FirstOrDefault(b => b.ThuTu == 4);
            DateTime ngay30NgayTruoc = DateTime.Now.AddDays(-30);
            var sanPhamMoi = db.SanPham
     .Where(sp => sp.NoiBat == true || sp.NgayTao >= ngay30NgayTruoc)
     .OrderByDescending(sp => sp.NgayTao)
     .Take(8)
     .ToList();
            ViewBag.SaoSanPham = db.DanhGia
                .GroupBy(dg => dg.MaSanPham)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(x => x.SoSao)
                );

            ViewBag.SanPhamMoi = sanPhamMoi;
            var sanPhamThinhHanh = db.SanPham
                                     .OrderByDescending(sp => sp.DanhGia.Count())
                                     .Take(8)
                                     .ToList();
            ViewBag.SaoSanPham = db.DanhGia
               .GroupBy(dg => dg.MaSanPham)
               .ToDictionary(
                   g => g.Key,
                   g => g.Average(x => x.SoSao)
               );

            ViewBag.SanPhamMoi = sanPhamMoi;
            ViewBag.SanPhamThinhHanh = sanPhamThinhHanh;
            LoadMenu();
            return View(banners);
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
        public ActionResult chitietsanpham(int id)
        {
            var sanPham = db.SanPham
                .FirstOrDefault(sp => sp.MaSanPham == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }
            int reviewCount = 0;
            double averageRating = 0.0;

            try
            {
                reviewCount = db.DanhGia.Count(r => r.MaSanPham == id);

                if (reviewCount > 0)
                {
                    averageRating = db.DanhGia
                        .Where(r => r.MaSanPham == id)
                        .Average(r => (double)r.SoSao);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi AverageRating: {ex.Message}");
            }
            int soldCount = 0;
            try
            {
                var total = db.ChiTietDonHang
                    .Join(db.ChiTietSanPham,
                        cd => cd.MaChiTietSanPham,
                        ct => ct.MaChiTiet,
                        (cd, ct) => new { cd, ct })
                    .Where(x => x.ct.MaSanPham == id)
                    .Sum(x => (int?)x.cd.SoLuong);

                soldCount = total ?? 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi SoldCount: {ex.Message}");
                soldCount = 0;
            }
            var chiTiet = db.ChiTietSanPham
                .Include("ThuongHieu")
                .Include("ChatLieu")
                .Include("MauSac")
                .FirstOrDefault(ct => ct.MaSanPham == id);
            var danhGias = db.DanhGia
                .Where(d => d.MaSanPham == id)
                .OrderByDescending(d => d.NgayDanhGia)
                .ToList();

            ViewBag.ChiTietSanPham = chiTiet;
            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.SoldCount = soldCount;
            ViewBag.DanhGias = danhGias;
            LoadMenu();
            return View(sanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiDanhGia(int MaSanPham, int SoSao, string NoiDung)
        {
            if (string.IsNullOrWhiteSpace(NoiDung))
            {
                TempData["Error"] = "Vui lòng nhập nội dung đánh giá!";
                return RedirectToAction("ChiTietSanPham", new { id = MaSanPham });
            }

            if (SoSao < 1 || SoSao > 5) SoSao = 5;

            var danhGia = new DanhGia
            {
                MaNguoiDung = 1,
                MaSanPham = MaSanPham,
                SoSao = SoSao,
                NoiDung = NoiDung.Trim(),
                NgayDanhGia = DateTime.Now
            };

            db.DanhGia.Add(danhGia);
            db.SaveChanges();

            TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm!";
            return RedirectToAction("ChiTietSanPham", new { id = MaSanPham });
        }
    }
}
    