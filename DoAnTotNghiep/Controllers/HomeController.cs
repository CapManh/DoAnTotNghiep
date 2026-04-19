using DoAnTotNghiep.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using BCrypt.Net;
using System.Data.Entity;

namespace DoAnTotNghiep.Controllers
{
    
    public class HomeController : Controller
    {
        private Model1 db = new Model1();
        [HttpPost]
        public JsonResult Login(string Ten, string MatKhau)
        {
            var user = db.NguoiDung.FirstOrDefault(x => x.Ten == Ten);

            if (user != null)
            {
                bool isPasswordCorrect = false;
                if (user.IsActive == false)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Tài khoản của bạn hiện tại đã bị khóa. Vui lòng liên hệ quản trị viên để được hỗ trợ!"
                    });
                }

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

                    string redirectUrl = "/";

                    switch (user.MaVaiTro)
                    {
                        case 1:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;

                        case 2:
                            redirectUrl = Url.Action("TrangChu", "Home");
                            break;

                        case 3:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;

                        case 5:
                            redirectUrl = Url.Action("ThongKe", "Admin");
                            break;
                    }

                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công!",
                        redirect = redirectUrl
                    });
                }
            }

            return Json(new { success = false, message = "Sai tài khoản hoặc mật khẩu!" });
        }
        public ActionResult SanPhamTheoDanhMuc(int id)
        {
            var danhMuc = db.DanhMuc.Find(id);

            ViewBag.TenDanhMucDangChon = danhMuc?.TenDanhMuc;

            var sanPhams = db.SanPham
                             .Where(x => x.MaDanhMuc == id)
                             .ToList();

            return View(sanPhams);
        }
        [HttpPost]
        public JsonResult ThemGioHang(int id, int quantity = 1)
        {
            try
            {
                if (Session["MaNguoiDung"] == null)
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });

                if (quantity < 1) quantity = 1;

                int maND = Convert.ToInt32(Session["MaNguoiDung"]);

                // Không kiểm tra tồn kho khi thêm (theo yêu cầu)
                var item = db.GioHangs
                    .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == id);

                if (item == null)
                {
                    db.GioHangs.Add(new GioHang
                    {
                        MaNguoiDung = maND,
                        MaSanPham = id,
                        SoLuong = quantity,
                        NgayTao = DateTime.Now
                    });
                }
                else
                {
                    item.SoLuong += quantity;   // Cộng dồn thoải mái
                }

                db.SaveChanges();

                return Json(new { success = true, message = $"Đã thêm {quantity} sản phẩm vào giỏ hàng!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        private JsonResult LayMiniCart(int maND)
        {
            db.Configuration.ProxyCreationEnabled = false;

            var cart = db.GioHang
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .ToList();

            return Json(new
            {
                success = true,
                totalQuantity = cart.Sum(x => x.SoLuong),
                totalAmount = cart.Sum(x => x.SoLuong * (x.SanPham.Gia ?? 0)),
                cartItems = cart.Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    AnhChinh =x.SanPham.AnhChinh,
                    Gia = x.SanPham.Gia ?? 0,
                    SL = x.SoLuong,
                    Link = "/Home/chitietsanpham/" + x.MaSanPham
                })
            });
        }
        [HttpGet]
        public JsonResult LayGioHang()
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            db.Configuration.ProxyCreationEnabled = false;

            var cart = db.GioHang
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .AsEnumerable()
                .Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    AnhChinh = x.SanPham.AnhChinh,
                    Gia = x.SanPham.Gia ?? 0,
                    SL = x.SoLuong ?? 0,
                    Link = "/Home/chitietsanpham/" + x.MaSanPham,
                    ThanhTien = (x.SanPham.Gia ?? 0) * (x.SoLuong ?? 0)
                })
                .ToList();

            return Json(new
            {
                success = true,
                totalQuantity = cart.Sum(x => x.SL),
                totalAmount = cart.Sum(x => x.ThanhTien),
                cartItems = cart
            }, JsonRequestBehavior.AllowGet);
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
        public JsonResult XoaGioHang(int id)
        {
            if (Session["MaNguoiDung"] == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại!" });
            }

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            // Tìm và xóa sản phẩm
            var item = db.GioHangs
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == id);

            if (item != null)
            {
                db.GioHangs.Remove(item);
                db.SaveChanges();
            }
            else
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
            }

            // Lấy lại giỏ hàng sau khi xóa
            var cart = db.GioHangs
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND)
                .Select(x => new
                {
                    MaSP = x.MaSanPham,
                    TenSP = x.SanPham.TenSanPham,
                    Anh = "/AnhWeb/" + x.SanPham.AnhChinh,
                    Gia = x.SanPham.Gia ?? 0,
                    SL = x.SoLuong ?? 0,
                    ThanhTien = (x.SanPham.Gia ?? 0) * (x.SoLuong ?? 0)
                })
                .ToList();

            return Json(new
            {
                success = true,
                message = "Đã xóa sản phẩm khỏi giỏ hàng!",
                totalQuantity = cart.Sum(x => x.SL),
                totalAmount = cart.Sum(x => x.ThanhTien),
                cartItems = cart
            });
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
                model.IsActive = true;
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
        public ActionResult sanpham1(
          string sort = "mac-dinh",
          int? danhMuc = null,
          int[] thuongHieu = null,
          int[] mauSac = null,
          decimal? giaDen = null,
          int? page = 1,
          string keyword = null)
        {
            int pageSize = 6;
            int pageNumber = (page ?? 1);
            var query = db.SanPham
                          .Include("ChiTietSanPham")
                          .AsQueryable();

            if (danhMuc.HasValue && danhMuc.Value > 0)
            {
                query = query.Where(sp => sp.MaDanhMuc == danhMuc.Value);
            }
            if (thuongHieu != null && thuongHieu.Length > 0)
            {
                var thuongHieuList = thuongHieu.ToList();
                query = query.Where(sp => sp.ChiTietSanPham
                    .Any(ct => ct.MaThuongHieu.HasValue && thuongHieuList.Contains(ct.MaThuongHieu.Value)));
            }
            if (mauSac != null && mauSac.Length > 0)
            {
                query = query.Where(sp => sp.ChiTietSanPham
                    .Any(ct => ct.MaMau.HasValue && mauSac.Contains(ct.MaMau.Value)));
            }
            if (giaDen.HasValue && giaDen.Value > 0)
            {
                query = query.Where(sp => sp.Gia <= giaDen.Value);
            }
            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim().ToLower();
                query = query.Where(sp => sp.TenSanPham.ToLower().Contains(keyword));
            }
            switch (sort.ToLower())
            {
                case "gia-asc":
                    query = query.OrderBy(sp => sp.Gia);
                    ViewBag.SortOrder = "gia-asc";
                    break;

                case "gia-desc":
                    query = query.OrderByDescending(sp => sp.Gia);
                    ViewBag.SortOrder = "gia-desc";
                    break;

                default:
                    query = query.OrderByDescending(sp => sp.NoiBat)
                                 .ThenByDescending(sp => sp.NgayTao);
                    ViewBag.SortOrder = "mac-dinh";
                    break;
            }

            int totalProducts = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var sanPhamList = query
                              .Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToList();
            ViewBag.SaoSanPham = db.DanhGia
                .GroupBy(dg => dg.MaSanPham)
                .ToDictionary(g => g.Key, g => g.Average(x => x.SoSao));
            ViewBag.SanPhamMoi = sanPhamList;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalProducts;
            ViewBag.PageSize = pageSize;

            ViewBag.DanhMucList = db.DanhMuc.ToList();
            ViewBag.ThuongHieuList = db.ThuongHieu.ToList();
            ViewBag.MauSacList = db.MauSac.ToList();

            ViewBag.SelectedDanhMuc = danhMuc;
            ViewBag.SelectedThuongHieu = thuongHieu ?? new int[0];
            ViewBag.SelectedMauSac = mauSac ?? new int[0];
            ViewBag.GiaDen = giaDen ?? 15000000m;

            ViewBag.SanPhamMoi = sanPhamList;
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
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var cartItems = db.GioHangs
                .Where(x => x.MaNguoiDung == maND)
                .Include(x => x.SanPham)
                .Include(x => x.SanPham.ChiTietSanPhams)
                .OrderBy(x => x.NgayTao)
                .ToList();

            LoadMenu();
            return View(cartItems);
        }
        [HttpPost]
        public JsonResult CapNhatSoLuongGioHang(int maSanPham, int soLuong)
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            if (soLuong < 1) soLuong = 1;

            var item = db.GioHangs
                .FirstOrDefault(x => x.MaNguoiDung == maND && x.MaSanPham == maSanPham);

            if (item == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

            // Kiểm tra tồn kho (tùy chọn, bạn có thể bỏ nếu muốn cho phép cộng dồn thoải mái)
            var tonKho = db.ChiTietSanPhams
                .Where(x => x.MaSanPham == maSanPham)
                .Select(x => x.SoLuongTon)
                .FirstOrDefault() ?? 0;

            if (soLuong > tonKho)
            {
                return Json(new { success = false, message = $"Chỉ còn {tonKho} sản phẩm trong kho!" });
            }

            item.SoLuong = soLuong;
            db.SaveChanges();

            return Json(new { success = true });
        }
        public ActionResult chitietsanpham(int id)
        {
            var sanPham = db.SanPhams
                .FirstOrDefault(sp => sp.MaSanPham == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy chi tiết sản phẩm (variant) để lấy số lượng tồn kho
            var chiTiet = db.ChiTietSanPhams
                .Include("ThuongHieu")
                .Include("ChatLieu")
                .Include("MauSac")
                .FirstOrDefault(ct => ct.MaSanPham == id);

            // Tính số lượng tồn kho hiện có (nếu có nhiều chi tiết thì lấy tổng)
            int soLuongTon = 0;
            if (chiTiet != null)
            {
                soLuongTon = chiTiet.SoLuongTon ?? 0;
            }
            else
            {
                // Nếu chưa có chi tiết, mặc định tồn kho = 0
                soLuongTon = 0;
            }

            // Các phần còn lại giữ nguyên
            int reviewCount = 0;
            double averageRating = 0.0;
            try
            {
                reviewCount = db.DanhGias.Count(r => r.MaSanPham == id);
                if (reviewCount > 0)
                {
                    averageRating = db.DanhGias
                        .Where(r => r.MaSanPham == id)
                        .Average(r => (double)r.SoSao);
                }
            }
            catch { }

            int soldCount = 0;
            try
            {
                var total = db.ChiTietDonHangs
                    .Join(db.ChiTietSanPhams,
                        cd => cd.MaChiTietSanPham,
                        ct => ct.MaChiTiet,
                        (cd, ct) => new { cd, ct })
                    .Where(x => x.ct.MaSanPham == id)
                    .Sum(x => (int?)x.cd.SoLuong);
                soldCount = total ?? 0;
            }
            catch { }

            var danhGias = db.DanhGias
                .Include(x => x.NguoiDung)
                .Where(x => x.MaSanPham == id)
                .OrderByDescending(x => x.NgayDanhGia)
                .ToList();

            ViewBag.DanhGias = danhGias;
            ViewBag.ChiTietSanPham = chiTiet;
            ViewBag.AverageRating = averageRating;
            ViewBag.ReviewCount = reviewCount;
            ViewBag.SoldCount = soldCount;
            ViewBag.SoLuongTon = soLuongTon;        // ← Thêm dòng này
            ViewBag.DanhGias = danhGias;

            LoadMenu();
            return View(sanPham);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult GuiDanhGia(int MaSanPham, int SoSao, string NoiDung)
        {
            if (Session["MaNguoiDung"] == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập!" });

            if (SoSao <= 0)
                return Json(new { success = false, message = "Vui lòng chọn sao!" });

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var dg = new DanhGia()
            {
                MaNguoiDung = maND,
                MaSanPham = MaSanPham,
                SoSao = SoSao,
                NoiDung = NoiDung,
                NgayDanhGia = DateTime.Now
            };

            db.DanhGia.Add(dg);
            db.SaveChanges();
            var user = db.NguoiDung.Find(maND);
            var danhGias = db.DanhGia
                .Where(x => x.MaSanPham == MaSanPham)
                .ToList();

            double avgStar = danhGias.Average(x => x.SoSao ?? 0);
            int reviewCount = danhGias.Count;

            return Json(new
            {
                success = true,
                tenNguoiDung = user.Ten,
                soSao = SoSao,
                noiDung = NoiDung,
                ngay = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                avgStar,
                reviewCount
            });
        }
        public JsonResult Suggest(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            keyword = keyword.Trim().ToLower();

            var data = db.SanPham
                .AsEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.TenSanPham) &&
                            p.TenSanPham.ToLower().Contains(keyword))
                .Take(3)
                .Select(p => new
                {
                    p.MaSanPham,
                    p.TenSanPham,
                    AnhChinh = p.AnhChinh
                })
                .ToList();

            return Json(data, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult ApDungVoucher(string code, decimal tongTien)
        {
            if (Session["MaNguoiDung"] == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập"
                });
            }

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var voucher = db.KhuyenMai
                .FirstOrDefault(x => x.MaCode == code);

            if (voucher == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Mã không tồn tại"
                });
            }
            if (voucher.NgayBatDau > DateTime.Now ||
                voucher.NgayKetThuc < DateTime.Now)
            {
                return Json(new
                {
                    success = false,
                    message = "Mã đã hết hạn"
                });
            }
            bool daDung = db.DonHang.Any(x =>
                x.MaNguoiDung == maND &&
                x.MaKhuyenMai == voucher.MaKhuyenMai);

            if (daDung)
            {
                return Json(new
                {
                    success = false,
                    message = "Bạn đã sử dụng mã này rồi"
                });
            }

            return Json(new
            {
                success = true,
                giamGia = voucher.PhanTramGiam,
                maKhuyenMai = voucher.MaKhuyenMai,
                message = "Áp dụng mã thành công với mức " + voucher.PhanTramGiam + "%"
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DatHang(
string HoTen,
string SoDienThoai,
string Email,
string DiaChi,
int MaPhuongThuc,
decimal TongTien,
string ids = null,        // danh sách ID sản phẩm được chọn
string MaGiamGia = null)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            List<GioHang> gioHang = new List<GioHang>();

            // ================= XỬ LÝ MUA NGAY =================
            if (Request.Form["MuaNgay"] == "true")
            {
                if (int.TryParse(Request.Form["MaSanPham"], out int maSP) &&
                    int.TryParse(Request.Form["SoLuong"], out int soLuong))
                {
                    var sp = db.SanPhams.Find(maSP);
                    if (sp != null)
                    {
                        gioHang.Add(new GioHang
                        {
                            MaSanPham = maSP,
                            SoLuong = soLuong,
                            SanPham = sp
                        });
                    }
                }
            }
            else
            {
                // ================= LẤY SẢN PHẨM TỪ GIỎ HÀNG =================
                var query = db.GioHangs
                              .Include(x => x.SanPham)
                              .Where(x => x.MaNguoiDung == maND);

                if (!string.IsNullOrEmpty(ids))
                {
                    var listId = ids.Split(',')
                                    .Select(x => int.TryParse(x.Trim(), out int id) ? id : 0)
                                    .Where(x => x > 0)
                                    .ToList();

                    query = query.Where(x => x.MaSanPham.HasValue && listId.Contains(x.MaSanPham.Value));
                }

                gioHang = query.ToList();
            }

            if (!gioHang.Any())
            {
                TempData["Error"] = "Không có sản phẩm nào để đặt hàng!";
                return RedirectToAction("GioHang", "Home");
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int? maKhuyenMai = null;
                    if (!string.IsNullOrEmpty(MaGiamGia))
                    {
                        var voucher = db.KhuyenMais.FirstOrDefault(x => x.MaCode == MaGiamGia);
                        if (voucher != null)
                            maKhuyenMai = voucher.MaKhuyenMai;
                    }

                    // Tạo đơn hàng
                    var donHang = new DonHang
                    {
                        MaNguoiDung = maND,
                        TongTien = TongTien,
                        TrangThai = "Chờ xác nhận",
                        DiaChiGiao = DiaChi,
                        NgayDat = DateTime.Now,
                        MaPhuongThuc = MaPhuongThuc,
                        MaKhuyenMai = maKhuyenMai
                    };

                    db.DonHangs.Add(donHang);
                    db.SaveChanges();

                    // Thêm chi tiết đơn hàng + trừ tồn kho
                    foreach (var item in gioHang)
                    {
                        var chiTietSP = db.ChiTietSanPhams
                            .FirstOrDefault(x => x.MaSanPham == item.MaSanPham);

                        if (chiTietSP == null) continue;

                        int soLuong = item.SoLuong ?? 1;

                        // Trừ tồn kho
                        chiTietSP.SoLuongTon = (chiTietSP.SoLuongTon ?? 0) - soLuong;

                        db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDonHang = donHang.MaDonHang,
                            MaChiTietSanPham = chiTietSP.MaChiTiet,
                            SoLuong = soLuong,
                            Gia = item.SanPham?.Gia ?? 0
                        });
                    }

                    // ================= KHÔNG XÓA SẢN PHẨM KHỎI GIỎ HÀNG =================
                    // (Bỏ hoàn toàn phần RemoveRange)

                    db.SaveChanges();
                    transaction.Commit();

                    // Redirect theo phương thức thanh toán
                    if (MaPhuongThuc == 2)
                        return RedirectToAction("QR", new { maDonHang = donHang.MaDonHang });
                    else if (MaPhuongThuc == 5)
                        return RedirectToAction("ThanhCongCOD", new { maDonHang = donHang.MaDonHang });

                    TempData["Success"] = "Đặt hàng thành công! Sản phẩm vẫn giữ trong giỏ hàng.";
                    return RedirectToAction("DonHangCuaToi", "Home");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    TempData["Error"] = "Đặt hàng thất bại: " + ex.Message;
                    return RedirectToAction("GioHang", "Home");
                }
            }
        }
        public ActionResult QR(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return RedirectToAction("GioHang");

            decimal tongTien = donHang.TongTien ?? 0;
            decimal tienCoc = tongTien * 0.1m;

            string stk = "123456789";
            string bank = "VCB";
            string ten = "LE TUAN MANH";
            string noiDung = "DH" + maDonHang;

            string qr =
                $"https://img.vietqr.io/image/{bank}-{stk}-compact2.png" +
                $"?amount={(long)tienCoc}" +
                $"&addInfo={noiDung}" +
                $"&accountName={ten}";

            ViewBag.MaDonHang = maDonHang;
            ViewBag.QR = qr;
            ViewBag.TienCoc = tienCoc;
            ViewBag.NoiDung = noiDung;

            return View();
        }
        [HttpPost]
        public JsonResult XacNhanChuyenKhoan(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
            {
                return Json(new { success = false });
            }

            donHang.TrangThai = "Đã thanh toán";

            db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = maDonHang,
                MaPhuongThuc = 2,
                SoTien = donHang.TongTien,
                TrangThai = "Hoàn thành",
                MaGiaoDich = Guid.NewGuid().ToString(),
                NgayThanhToan = DateTime.Now
            });

            db.SaveChanges();

            return Json(new { success = true });
        }
        public ActionResult ThanhCongCOD(int maDonHang)
        {
            var donHang = db.DonHangs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (donHang == null)
                return RedirectToAction("GioHang");

            ViewBag.MaDonHang = maDonHang;
            ViewBag.TongTien = donHang.TongTien;

            return View();
        }
        public ActionResult MuaNgay(int id, int soLuong)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            if (soLuong < 1) soLuong = 1;

            var sp = db.SanPhams.Find(id);
            if (sp == null)
                return HttpNotFound();

            // Tạo giỏ tạm (KHÔNG dùng DB)
            var cart = new List<GioHang>
{
    new GioHang
    {
        MaSanPham = id,
        SoLuong = soLuong,
        SanPham = sp
    }
};

            ViewBag.MuaNgay = true; // để phân biệt với giỏ hàng

            LoadMenu();
            return View("ThanhToan", cart);
        }
        public ActionResult ThanhToan(string ids)
        {
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("TrangChu", "Home");

            int maND = (int)Session["MaNguoiDung"];

            List<int> listId;

            if (string.IsNullOrWhiteSpace(ids))
            {
                listId = db.GioHangs
                    .Where(x => x.MaNguoiDung == maND && x.MaSanPham != null)
                    .Select(x => x.MaSanPham.Value)
                    .ToList();
            }
            else
            {
                listId = ids
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out int idParsed) ? idParsed : -1)
                    .Where(x => x > 0)
                    .ToList();
            }

            var cart = db.GioHangs
                .Include(x => x.SanPham)
                .Where(x => x.MaNguoiDung == maND
                         && x.MaSanPham != null
                         && listId.Contains(x.MaSanPham.Value))
                .ToList();

            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng trống.";
                return RedirectToAction("GioHang", "Home");
            }

            // QUAN TRỌNG: truyền xuống view
            ViewBag.Ids = string.Join(",", listId);

            return View(cart);
        }
        public ActionResult DonHangCuaToi(string trangThai = "all")
        {
            AutoConfirmOrder();
            if (Session["MaNguoiDung"] == null)
                return RedirectToAction("DangNhap", "Home");

            int maND = Convert.ToInt32(Session["MaNguoiDung"]);

            var donHangs = db.DonHang
                .Where(x => x.MaNguoiDung == maND)
                .OrderByDescending(x => x.NgayDat)
                .ToList();

            if (trangThai != "all")
            {
                donHangs = donHangs
                    .Where(x => x.TrangThai == trangThai)
                    .ToList();
            }
            LoadMenu();
            return View(donHangs);
        }

        [HttpPost]
        public JsonResult DaNhanHang(int id)
        {
            var dh = db.DonHang.Find(id);

            if (dh == null)
                return Json(new { success = false });

            dh.TrangThai = "Đã giao hàng";
            dh.NgayGiaoHang = DateTime.Now;
            db.SaveChanges();

            return Json(new { success = true });
        }
        void AutoConfirmOrder()
        {
            var now = DateTime.Now;

            var list = db.DonHang
                .Where(x => x.TrangThai == "Đã giao hàng"
                         && x.NgayGiaoHang != null)
                .ToList();

            foreach (var dh in list)
            {
                if ((now - dh.NgayGiaoHang.Value).TotalMinutes >= 10)
                {
                    dh.TrangThai = "Đã nhận hàng";
                }
            }

            db.SaveChanges();
        }
    }
}
    