using DoAnTotNghiep.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;

namespace DoAnTotNghiep.Controllers
{
    public class AdminController : Controller
    {
        private Model1 db = new Model1();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            int role = (int)Session["VaiTro"];

            if (Session["VaiTro"] == null || (role != 1 && role != 3 && role != 5))
            {
                filterContext.Result = new RedirectResult("~/Admin/Login");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
        public ActionResult admin()
        {
            if (Session["VaiTro"] == null || (int)Session["VaiTro"] != 1)
                return RedirectToAction("Login", "Home");
            {
                var db = new Model1();

                ViewBag.SoSanPham = db.SanPham.Count();
                ViewBag.SoNguoiDung = db.NguoiDung.Count();
                ViewBag.SoDonHang = db.DonHang.Count();
                ViewBag.DoanhThu = db.DonHang.Sum(x => (decimal?)x.TongTien) ?? 0;

                return View();
            }
        }
        public ActionResult Thongke()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
            var doanhThu = db.ChiTietDonHang.Sum(od => (decimal?)(od.Gia * od.SoLuong)) ?? 0;
            var soLuongDonHang = db.DonHang.Count();
            var soLuongKhachHang = db.NguoiDung.Count(u => u.MaVaiTro == 2);
            var soLuongNhanVien = db.NguoiDung.Count(u => u.MaVaiTro == 3 || u.MaVaiTro == 4);
            var soLuongSanPham = db.SanPham.Count();
            var soLuongDanhMuc = db.DanhMuc.Count();
            var tongDanhGia = db.DanhGia.Count();
            var tongTonKho = db.ChiTietSanPham.Sum(ct => (int?)ct.SoLuongTon) ?? 0;

            var giaTriTonKho = db.ChiTietSanPham
                .Include(ct => ct.SanPham)
                .Sum(ct => (decimal?)(ct.SoLuongTon * ct.SanPham.Gia)) ?? 0;

            var donHangHomNay = db.DonHang.Count(o => o.NgayDat >= today && o.NgayDat < tomorrow);
            var sanPhamHetHang = db.ChiTietSanPham.Count(ct => ct.SoLuongTon == 0);
            var sanPhamSapHet = db.ChiTietSanPham.Count(ct => ct.SoLuongTon > 0 && ct.SoLuongTon <= 5);

            var khachHangMoiThangNay = db.NguoiDung
                .Count(u => u.MaVaiTro == 2 && u.NgayTao >= firstDayOfMonth);

            var donHangHuy = db.DonHang.Count(o => o.TrangThai == "Đã hủy");
            var donHangHoanThanh = db.DonHang.Count(o => o.TrangThai == "Hoàn thành");
            var tyLeHoanThanh = soLuongDonHang > 0
                ? Math.Round((decimal)donHangHoanThanh * 100 / soLuongDonHang, 1) : 0;

            var trungBinhDanhGia = tongDanhGia > 0
                ? Math.Round(db.DanhGia.Average(r => (decimal)r.SoSao), 1) : 0;

            var averageOrderValue = soLuongDonHang > 0
                ? Math.Round(doanhThu / soLuongDonHang, 0) : 0;
            ViewBag.DoanhThu = doanhThu;
            ViewBag.SoLuongDonHang = soLuongDonHang;
            ViewBag.SoLuongKhachHang = soLuongKhachHang;
            ViewBag.SoLuongNhanVien = soLuongNhanVien;
            ViewBag.SoLuongSanPham = soLuongSanPham;
            ViewBag.SoLuongDanhMuc = soLuongDanhMuc;
            ViewBag.TongDanhGia = tongDanhGia;
            ViewBag.TongTonKho = tongTonKho;
            ViewBag.GiaTriTonKho = giaTriTonKho;
            ViewBag.DonHangHomNay = donHangHomNay;
            ViewBag.SanPhamHetHang = sanPhamHetHang;
            ViewBag.SanPhamSapHet = sanPhamSapHet;
            ViewBag.KhachHangMoi = khachHangMoiThangNay;
            ViewBag.DonHangHuy = donHangHuy;
            ViewBag.TyLeHoanThanh = tyLeHoanThanh;
            ViewBag.TrungBinhDanhGia = trungBinhDanhGia;
            ViewBag.AverageOrderValue = averageOrderValue;

            var doanhThuTheoThangRaw = db.DonHang
                .Where(o => o.NgayDat.HasValue)
                .SelectMany(o => o.ChiTietDonHang.Select(od => new
                {
                    Thang = o.NgayDat.Value.Month,
                    Nam = o.NgayDat.Value.Year,
                    Tien = (decimal)(od.Gia * od.SoLuong)
                }))
                .GroupBy(x => new { x.Nam, x.Thang })
                .Select(g => new
                {
                    Nam = g.Key.Nam,
                    Thang = g.Key.Thang,
                    DoanhThu = g.Sum(x => x.Tien)
                })
                .OrderByDescending(x => x.Nam).ThenByDescending(x => x.Thang)
                .ToList();

            ViewBag.DoanhThuTheoThang = doanhThuTheoThangRaw.Select(x =>
            {
                dynamic d = new ExpandoObject();
                d.ThangNam = $"{x.Thang}/{x.Nam}";
                d.DoanhThu = x.DoanhThu;
                return d;
            }).ToList();
            var tyLeDonHang = db.DonHang
                .GroupBy(o => o.TrangThai ?? "Không xác định")
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            ViewBag.TyLeDonHang = tyLeDonHang.Select(x =>
            {
                dynamic d = new ExpandoObject();
                d.TrangThai = x.TrangThai;
                d.SoLuong = x.SoLuong;
                return d;
            }).ToList();

            // === Top 5 Sản phẩm bán chạy (group theo MaSanPham) ===
            var topSanPham = db.ChiTietDonHang
                .Include(cd => cd.ChiTietSanPham.SanPham)
                .GroupBy(cd => cd.ChiTietSanPham.MaSanPham)
                .Select(g => new
                {
                    MaSanPham = g.Key,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            var listTopSanPham = topSanPham.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                obj.ProductName = item.MaSanPham != null
                    ? db.SanPham.FirstOrDefault(p => p.MaSanPham == item.MaSanPham)?.TenSanPham ?? "Không rõ"
                    : "Không rõ";
                obj.Quantity = item.SoLuongBan;
                return obj;
            }).ToList();

            ViewBag.TopSanPhamBanChay = listTopSanPham;

            // === Top 5 Khách hàng mua nhiều nhất ===
            var topKhachHang = db.DonHang
                .GroupBy(o => o.MaNguoiDung)
                .Select(g => new { MaNguoiDung = g.Key, SoDonHang = g.Count() })
                .OrderByDescending(x => x.SoDonHang)
                .Take(5)
                .ToList();

            var listTopKhachHang = topKhachHang.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                var user = db.NguoiDung.FirstOrDefault(u => u.MaNguoiDung == item.MaNguoiDung);
                obj.FullName = user?.Ten ?? "Không rõ";
                obj.SoDonHang = item.SoDonHang;
                return obj;
            }).ToList();

            ViewBag.TopKhachHang = listTopKhachHang;

            // === Sản phẩm đánh giá thấp (1-2 sao) ===
            var sanPhamThap = db.DanhGia
                .Where(r => r.SoSao >= 1 && r.SoSao <= 2)
                .GroupBy(r => r.MaSanPham)
                .Select(g => new { MaSanPham = g.Key, SoLuong = g.Count() })
                .OrderByDescending(g => g.SoLuong)
                .Take(5)
                .ToList();

            ViewBag.SanPhamDanhGiaThap = sanPhamThap.Select(item =>
            {
                dynamic obj = new ExpandoObject();
                var sp = db.SanPham.FirstOrDefault(p => p.MaSanPham == item.MaSanPham);
                obj.ProductName = sp?.TenSanPham ?? "Không rõ";
                obj.SoDanhGiaThap = item.SoLuong;
                return obj;
            }).ToList();

            var sanPhamChuaBanRaw = db.SanPham
                .Where(p => !db.ChiTietDonHang.Any(cd =>
                    db.ChiTietSanPham.Any(ct => ct.MaChiTiet == cd.MaChiTietSanPham && ct.MaSanPham == p.MaSanPham)))
                .Select(p => p.TenSanPham)
                .ToList();

            ViewBag.SanPhamChuaBan = sanPhamChuaBanRaw.Select(name =>
            {
                dynamic obj = new ExpandoObject();
                obj.ProductName = name;
                return obj;
            }).ToList();

            return View();
        }
        public ActionResult QuanLySanPham(
    string search,
    int? danhMuc,
    int? thuongHieu,
    int? chatLieu,
    int? mauSac,
    bool? noiBat,
    int page = 1,
    int pageSize = 10)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 50) pageSize = 10;

                var query = db.SanPham
                    .Include(s => s.DanhMuc)
                    .Include(s => s.ChiTietSanPham)
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.ThuongHieu))
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.ChatLieu))
                    .Include(s => s.ChiTietSanPham.Select(ct => ct.MauSac))
                    .AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower().Trim();
                    query = query.Where(s =>
                        s.TenSanPham.ToLower().Contains(search) ||
                        s.DanhMuc.TenDanhMuc.ToLower().Contains(search));
                }


                if (danhMuc.HasValue)
                    query = query.Where(x => x.MaDanhMuc == danhMuc);

                if (noiBat.HasValue)
                    query = query.Where(x => x.NoiBat == noiBat);

                if (thuongHieu.HasValue || chatLieu.HasValue || mauSac.HasValue)
                {
                    query = query.Where(x =>
                        x.ChiTietSanPham.Any(ct =>
                            (!thuongHieu.HasValue || ct.MaThuongHieu == thuongHieu) &&
                            (!chatLieu.HasValue || ct.MaChatLieu == chatLieu) &&
                            (!mauSac.HasValue || ct.MaMau == mauSac)
                        )
                    );
                }

                int totalProducts = query.Count();

                var result = query
                    .OrderByDescending(s => s.NoiBat)
                    .ThenByDescending(s => s.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);
                ViewBag.CurrentPage = page;
                ViewBag.Search = search;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalProducts = totalProducts;

                // DROPDOWN DATA
                ViewBag.DanhMucList = new SelectList(db.DanhMuc, "MaDanhMuc", "TenDanhMuc");
                ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu, "MaThuongHieu", "TenThuongHieu");
                ViewBag.ChatLieuList = new SelectList(db.ChatLieu, "MaChatLieu", "TenChatLieu");
                ViewBag.MauSacList = new SelectList(db.MauSac, "MaMau", "TenMau");

                return View(result);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new List<SanPham>());
            }
        }
        public ActionResult ThemSanPham()
        {
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");
            return View(new SanPham());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham(
    SanPham model,
    HttpPostedFileBase fileAnhChinh,
    HttpPostedFileBase fileAnhPhu1,
    HttpPostedFileBase fileAnhPhu2,
    HttpPostedFileBase fileAnhPhu3,
    int MaThuongHieu,
    int MaChatLieu,
    int MaMau,
    int SoLuongTon = 10)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    model.AnhChinh = SaveImage(fileAnhChinh);
                    model.AnhPhu1 = SaveImage(fileAnhPhu1);
                    model.AnhPhu2 = SaveImage(fileAnhPhu2);
                    model.AnhPhu3 = SaveImage(fileAnhPhu3);

                    db.SanPham.Add(model);
                    db.SaveChanges();

                    var chiTiet = new ChiTietSanPham
                    {
                        MaSanPham = model.MaSanPham,
                        MaThuongHieu = MaThuongHieu,
                        MaChatLieu = MaChatLieu,
                        MaMau = MaMau,
                        SoLuongTon = SoLuongTon > 0 ? SoLuongTon : 10
                    };

                    db.ChiTietSanPham.Add(chiTiet);
                    db.SaveChanges();

                    TempData["Message"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi thêm sản phẩm: " + ex.Message;

                    if (ex.InnerException != null)
                        ViewBag.Error += " - " + ex.InnerException.Message;
                }
            }
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc");
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");

            return View(model);
        }
        private string SaveImage(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0)
                return null;

            string extension = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid() + extension;

            string folder = Server.MapPath("~/AnhWeb");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string path = Path.Combine(folder, fileName);

            file.SaveAs(path);

            return "/AnhWeb/" + fileName;
        }
        public ActionResult ChinhSuaSanPham(int id)
        {
            try
            {
                var sanPham = db.SanPham
                    .Include(s => s.DanhMuc)
                    .FirstOrDefault(s => s.MaSanPham == id);

                if (sanPham == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm!";
                    return RedirectToAction("QuanLySanPham");
                }
                ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
                ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu");
                ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu");
                ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau");

                var chiTiet = db.ChiTietSanPham
                    .FirstOrDefault(ct => ct.MaSanPham == id);

                if (chiTiet != null)
                {
                    ViewBag.MaThuongHieu = chiTiet.MaThuongHieu;
                    ViewBag.MaChatLieu = chiTiet.MaChatLieu;
                    ViewBag.MaMau = chiTiet.MaMau;
                    ViewBag.SoLuongTon = chiTiet.SoLuongTon;
                }
                else
                {
                    ViewBag.MaThuongHieu = null;
                    ViewBag.MaChatLieu = null;
                    ViewBag.MaMau = null;
                    ViewBag.SoLuongTon = 0;
                }

                return View(sanPham);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tải sản phẩm: " + ex.Message;
                if (ex.InnerException != null)
                    TempData["ErrorMessage"] += " - Chi tiết: " + ex.InnerException.Message;

                return RedirectToAction("QuanLySanPham");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaSanPham(SanPham model, int? MaThuongHieu, int? MaChatLieu, int? MaMau, int? SoLuongTon)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var sanPham = db.SanPham.Find(model.MaSanPham);
                    if (sanPham == null)
                    {
                        ViewBag.Error = "Không tìm thấy sản phẩm để chỉnh sửa.";
                        return View(model);
                    }
                    sanPham.TenSanPham = model.TenSanPham;
                    sanPham.Gia = model.Gia;
                    sanPham.MoTa = model.MoTa;
                    sanPham.AnhChinh = model.AnhChinh;
                    sanPham.AnhPhu1 = model.AnhPhu1;
                    sanPham.AnhPhu2 = model.AnhPhu2;
                    sanPham.AnhPhu3 = model.AnhPhu3;
                    sanPham.MaDanhMuc = model.MaDanhMuc;
                    sanPham.NoiBat = model.NoiBat;
                    var chiTiet = db.ChiTietSanPham.FirstOrDefault(ct => ct.MaSanPham == model.MaSanPham);

                    if (chiTiet != null)
                    {
                        if (MaThuongHieu.HasValue) chiTiet.MaThuongHieu = MaThuongHieu.Value;
                        if (MaChatLieu.HasValue) chiTiet.MaChatLieu = MaChatLieu.Value;
                        if (MaMau.HasValue) chiTiet.MaMau = MaMau.Value;
                        if (SoLuongTon.HasValue) chiTiet.SoLuongTon = SoLuongTon.Value;
                    }
                    else
                    {
                        chiTiet = new ChiTietSanPham
                        {
                            MaSanPham = model.MaSanPham,
                            MaThuongHieu = MaThuongHieu ?? 1,
                            MaChatLieu = MaChatLieu ?? 1,
                            MaMau = MaMau ?? 1,
                            SoLuongTon = SoLuongTon ?? 10
                        };
                        db.ChiTietSanPham.Add(chiTiet);
                    }

                    db.SaveChanges();

                    TempData["Message"] = "Chỉnh sửa sản phẩm thành công!";
                    return RedirectToAction("QuanLySanPham");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi chỉnh sửa sản phẩm: " + ex.Message;
                    if (ex.InnerException != null)
                        ViewBag.Error += " - Chi tiết: " + ex.InnerException.Message;
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.Error = "Dữ liệu không hợp lệ: " + string.Join(", ", errors);
            }
            ViewBag.DanhMucList = new SelectList(db.DanhMuc.ToList(), "MaDanhMuc", "TenDanhMuc", model.MaDanhMuc);
            ViewBag.ThuongHieuList = new SelectList(db.ThuongHieu.ToList(), "MaThuongHieu", "TenThuongHieu", MaThuongHieu);
            ViewBag.ChatLieuList = new SelectList(db.ChatLieu.ToList(), "MaChatLieu", "TenChatLieu", MaChatLieu);
            ViewBag.MauSacList = new SelectList(db.MauSac.ToList(), "MaMau", "TenMau", MaMau);
            return View(model);
        }
        [HttpPost]

        public ActionResult XoaSanPham(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var sanPham = db.SanPham.Find(id);
                    if (sanPham == null)
                    {
                        TempData["Message"] = "Không tìm thấy sản phẩm để xóa!";
                        return RedirectToAction("QuanLySanPham");
                    }
                    var chiTietGioHangs = db.ChiTietGioHang
                        .Where(cgh => db.ChiTietSanPham
                            .Any(ct => ct.MaChiTiet == cgh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietGioHangs.Any())
                        db.ChiTietGioHang.RemoveRange(chiTietGioHangs);
                    var chiTietDonHangs = db.ChiTietDonHang
                        .Where(cdh => db.ChiTietSanPham
                            .Any(ct => ct.MaChiTiet == cdh.MaChiTietSanPham && ct.MaSanPham == id))
                        .ToList();

                    if (chiTietDonHangs.Any())
                        db.ChiTietDonHang.RemoveRange(chiTietDonHangs);

                    var danhGias = db.DanhGia.Where(d => d.MaSanPham == id).ToList();
                    if (danhGias.Any())
                        db.DanhGia.RemoveRange(danhGias);

                    var chiTietSanPhams = db.ChiTietSanPham.Where(ct => ct.MaSanPham == id).ToList();
                    if (chiTietSanPhams.Any())
                        db.ChiTietSanPham.RemoveRange(chiTietSanPhams);

                    db.SanPham.Remove(sanPham);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Message"] = $"Xóa sản phẩm '{sanPham.TenSanPham}' thành công!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string errorMsg = "Lỗi khi xóa sản phẩm: " + ex.Message;
                    if (ex.InnerException != null)
                    {
                        errorMsg += " - Chi tiết: " + ex.InnerException.Message;

                        if (ex.InnerException.Message.Contains("REFERENCE") ||
                            ex.InnerException.Message.Contains("constraint"))
                        {
                            errorMsg = "Không thể xóa sản phẩm vì vẫn còn đơn hàng hoặc giỏ hàng liên quan. " +
                                       "Vui lòng kiểm tra lại!";
                        }
                    }

                    TempData["Message"] = errorMsg;
                    TempData["MessageType"] = "danger";
                }
            }

            return RedirectToAction("QuanLySanPham");
        }
        public ActionResult QuanLyBanner(string search = "", int page = 1, int pageSize = 10)
        {
            var query = db.Banner.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim();
                query = query.Where(b =>
                    b.TieuDe.ToLower().Contains(search) ||
                    (b.ViTri != null && b.ViTri.ToLower().Contains(search))
                );
            }

            int total = query.Count();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);

            var banners = query
                .OrderByDescending(b => b.ThuTu)
                .ThenByDescending(b => b.NgayBatDau)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.TotalBanners = total;

            return View(banners);
        }
        public ActionResult ThemBanner()
        {
            Banner model = new Banner
            {
                ThuTu = 1,
                TrangThai = true
            };

            return View(model);
        }
        public ActionResult ChinhSuaBanner(int id)
        {
            var banner = db.Banner.Find(id);
            if (banner == null) return HttpNotFound();

            return View(banner);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaBanner(Banner model, HttpPostedFileBase fileUpload)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var banner = db.Banner.Find(model.MaBanner);
                    if (banner == null) return HttpNotFound();

                    banner.TieuDe = model.TieuDe;
                    banner.Link = model.Link;
                    banner.ViTri = model.ViTri;
                    banner.TrangThai = model.TrangThai;
                    banner.ThuTu = model.ThuTu;
                    banner.NgayBatDau = model.NgayBatDau;
                    banner.NgayKetThuc = model.NgayKetThuc;

                    if (fileUpload != null && fileUpload.ContentLength > 0)
                    {
                        string extension = Path.GetExtension(fileUpload.FileName);
                        string fileName = Guid.NewGuid() + extension;

                        string path = Path.Combine(Server.MapPath("~/AnhWeb"), fileName);
                        fileUpload.SaveAs(path);

                        banner.HinhAnh = "/AnhWeb/" + fileName;
                    }

                    db.SaveChanges();

                    TempData["Message"] = "Cập nhật banner thành công!";
                    return RedirectToAction("QuanLyBanner");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi khi cập nhật: " + ex.Message;
                }
            }

            return View(model);
        }
        [HttpPost]

        public ActionResult XoaBanner(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var banner = db.Banner.Find(id);

                    if (banner == null)
                    {
                        TempData["Message"] = "Không tìm thấy banner!";
                        TempData["MessageType"] = "warning";
                        return RedirectToAction("QuanLyBanner");
                    }

                    db.Banner.Remove(banner);
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Message"] = $"Xóa banner '{banner.TieuDe}' thành công!";
                    TempData["MessageType"] = "success";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    string errorMsg = "Lỗi khi xóa banner: " + ex.Message;

                    if (ex.InnerException != null)
                    {
                        errorMsg += " | " + ex.InnerException.Message;
                    }

                    TempData["Message"] = errorMsg;
                    TempData["MessageType"] = "danger";
                }
            }

            return RedirectToAction("QuanLyBanner");
        }

        public ActionResult DanhSachTaiKhoan(string search, int? status, int? role, string sort, int page = 1, int pageSize = 10)
        {
            if (Session["VaiTro"] == null)
                return RedirectToAction("Login", "Admin");

            int vaiTro = Convert.ToInt32(Session["VaiTro"]);
            int currentUserId = Convert.ToInt32(Session["MaNguoiDung"]);

            var users = db.NguoiDung
                .Include(u => u.VaiTro)
                .Where(u => u.MaNguoiDung != currentUserId)
                .AsQueryable();


            if (vaiTro == 3)
                users = users.Where(u => u.MaVaiTro == 2);
            else if (vaiTro == 5)
                users = users.Where(u => u.MaVaiTro == 2 || u.MaVaiTro == 3);
            if (role != null)
            {
                users = users.Where(u => u.MaVaiTro == role);
            }

            if (status != null)
            {
                bool isActive = status == 1;
                users = users.Where(u => u.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.ToLower();

                users = users.Where(u =>
                    (u.Ten != null && u.Ten.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    (u.SoDienThoai != null && u.SoDienThoai.Contains(search)) ||
                    (u.DiaChi != null && u.DiaChi.ToLower().Contains(search)) ||
                    u.MaNguoiDung.ToString().Contains(search)
                );
            }

            switch (sort)
            {
                case "old":
                    users = users.OrderBy(u => u.MaNguoiDung);
                    break;

                default:
                    users = users.OrderByDescending(u => u.MaNguoiDung);
                    break;
            }
            int total = users.Count();

            var model = users
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.Search = search;
            ViewBag.VaiTro = vaiTro;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.VaiTroList = GetAllVaiTroList();

            return View(model);
        }
        public ActionResult ThemTaiKhoan()
        {
            if (Session["VaiTro"] == null)
                return RedirectToAction("Login", "Admin");

            int vaiTro = Convert.ToInt32(Session["VaiTro"]);

            List<SelectListItem> roles = new List<SelectListItem>();

            if (vaiTro == 1)
            {
                roles.Add(new SelectListItem { Value = "5", Text = "Quản trị viên" });
                roles.Add(new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" });
            }

            else if (vaiTro == 5)
            {
                roles.Add(new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" });
            }

            else
            {
                TempData["Message"] = "Bạn không có quyền thêm tài khoản!";
                return RedirectToAction("DanhSachTaiKhoan");
            }

            ViewBag.VaiTroList = roles;

            return View(new NguoiDung());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemTaiKhoan(NguoiDung model, string MatKhau)
        {
            if (Session["VaiTro"] == null)
                return RedirectToAction("Login", "Admin");

            int vaiTroNguoiTao = Convert.ToInt32(Session["VaiTro"]);


            if (vaiTroNguoiTao == 1)
            {
                if (model.MaVaiTro != 5 && model.MaVaiTro != 3 && model.MaVaiTro != 4)
                {
                    TempData["Error"] = "Bạn không được tạo vai trò này!";
                    return RedirectToAction("DanhSachTaiKhoan");
                }
            }

            else if (vaiTroNguoiTao == 5)
            {
                if (model.MaVaiTro != 3 && model.MaVaiTro != 4)
                {
                    TempData["Error"] = "Bạn chỉ được tạo nhân viên!";
                    return RedirectToAction("DanhSachTaiKhoan");
                }
            }

            else
            {
                TempData["Error"] = "Bạn không có quyền thêm tài khoản!";
                return RedirectToAction("DanhSachTaiKhoan");
            }

            if (string.IsNullOrWhiteSpace(model.Ten) || model.Ten.Length < 5)
            {
                ModelState.AddModelError("Ten", "Tên phải dài hơn 5 ký tự");
            }


            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("Email", "Email không được để trống");
            }
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.Email, pattern))
            {
                ModelState.AddModelError("Email", "Email không đúng định dạng");
            }


            if (string.IsNullOrWhiteSpace(model.SoDienThoai))
            {
                ModelState.AddModelError("SoDienThoai", "Số điện thoại không được để trống");
            }
            else if (!System.Text.RegularExpressions.Regex
                     .IsMatch(model.SoDienThoai, @"^\d{10,11}$"))
            {
                ModelState.AddModelError("SoDienThoai",
                    "Số điện thoại phải có 10 hoặc 11 chữ số");
            }


            if (string.IsNullOrWhiteSpace(MatKhau))
            {
                ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống");
            }

            if (db.NguoiDung.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại!");
            }

            if (!ModelState.IsValid)
            {
                GetVaiTroList(vaiTroNguoiTao);
                return View(model);
            }

            try
            {
                model.MatKhau = MatKhau;
                model.NgayTao = DateTime.Now;
                model.IsActive = true;
                db.NguoiDung.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm tài khoản thành công!";
                return RedirectToAction("DanhSachTaiKhoan");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                GetVaiTroList(vaiTroNguoiTao);
                return View(model);
            }
        }
        private List<SelectListItem> GetVaiTroList(int vaiTro)
        {
            var roles = new List<SelectListItem>();

            if (vaiTro == 1)
            {
                roles.Add(new SelectListItem { Value = "5", Text = "Quản trị viên" });
                roles.Add(new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" });
            }
            else if (vaiTro == 5)
            {
                roles.Add(new SelectListItem { Value = "3", Text = "Nhân viên bán hàng" });
            }
            else
            {
                roles.Add(new SelectListItem { Value = "2", Text = "Khách hàng" });
            }

            return roles;
        }
        private List<SelectListItem> GetAllVaiTroList()
        {
            return db.VaiTro
                .Select(v => new SelectListItem
                {
                    Value = v.MaVaiTro.ToString(),
                    Text = v.TenVaiTro
                })
                .ToList();
        }
        public ActionResult ChinhSuaTaiKhoan(int? id)
        {
            if (Session["VaiTro"] == null)
                return RedirectToAction("Login", "Admin");

            if (id == null)
                return HttpNotFound();

            int vaiTroHienTai = Convert.ToInt32(Session["VaiTro"]);

            var user = db.NguoiDung.Find(id);
            if (user == null)
                return HttpNotFound();

            ViewBag.VaiTroList = GetVaiTroList(vaiTroHienTai);

            return View(user);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChinhSuaTaiKhoan(NguoiDung model)
        {
            if (Session["VaiTro"] == null)
                return RedirectToAction("Login", "Admin");

            int vaiTroHienTai = Convert.ToInt32(Session["VaiTro"]);

            var user = db.NguoiDung.Find(model.MaNguoiDung);
            if (user == null)
                return HttpNotFound();

            if (string.IsNullOrWhiteSpace(model.Ten) || model.Ten.Length < 5)
                ModelState.AddModelError("Ten", "Tên phải dài hơn 5 ký tự");

            if (string.IsNullOrWhiteSpace(model.Email))
                ModelState.AddModelError("Email", "Email không được để trống");
            else
            {
                var emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Email, emailPattern))
                    ModelState.AddModelError("Email", "Email không hợp lệ");
            }

            if (db.NguoiDung.Any(x => x.Email == model.Email && x.MaNguoiDung != model.MaNguoiDung))
                ModelState.AddModelError("Email", "Email đã tồn tại");

            if (string.IsNullOrWhiteSpace(model.SoDienThoai))
                ModelState.AddModelError("SoDienThoai", "SĐT không được để trống");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(model.SoDienThoai, @"^\d{10,11}$"))
                ModelState.AddModelError("SoDienThoai", "SĐT phải 10 hoặc 11 số");


            if (!ModelState.IsValid)
            {
                ViewBag.VaiTroList = GetVaiTroList(vaiTroHienTai);
                return View(model);
            }

            user.Ten = model.Ten;
            user.Email = model.Email;
            user.SoDienThoai = model.SoDienThoai;
            user.DiaChi = model.DiaChi;
            user.MaVaiTro = model.MaVaiTro;

            db.SaveChanges();

            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction("DanhSachTaiKhoan");
        }
        [HttpPost]
        public ActionResult XoaTaiKhoan(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var user = db.NguoiDung
                        .Include(u => u.GioHang)
                        .Include(u => u.DonHang.Select(d => d.ChiTietDonHang))
                        .Include(u => u.DanhGia)
                        .FirstOrDefault(u => u.MaNguoiDung == id);

                    if (user == null)
                    {
                        TempData["Error"] = "Không tìm thấy tài khoản!";
                        return RedirectToAction("DanhSachTaiKhoan");
                    }

                    var chiTietDonHang = db.ChiTietDonHang
                        .Where(x => x.DonHang.MaNguoiDung == id)
                        .ToList();

                    db.ChiTietDonHang.RemoveRange(chiTietDonHang);

                    var thanhToan = db.ThanhToan
                        .Where(x => x.DonHang.MaNguoiDung == id)
                        .ToList();

                    db.ThanhToan.RemoveRange(thanhToan);

                    var donHang = db.DonHang
                        .Where(x => x.MaNguoiDung == id)
                        .ToList();

                    db.DonHang.RemoveRange(donHang);
                    var gioHang = db.GioHang
                        .Where(x => x.MaNguoiDung == id)
                        .ToList();

                    db.GioHang.RemoveRange(gioHang);

                    var danhGia = db.DanhGia
                        .Where(x => x.MaNguoiDung == id)
                        .ToList();

                    db.DanhGia.RemoveRange(danhGia);
                    db.NguoiDung.Remove(user);

                    db.SaveChanges();
                    transaction.Commit();

                    TempData["Success"] = "Xóa tài khoản thành công!";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    TempData["Error"] =
                        ex.InnerException?.InnerException?.Message
                        ?? ex.Message;
                }
            }

            return RedirectToAction("DanhSachTaiKhoan");
        }
        [HttpPost]
        public JsonResult ToggleKhoaTaiKhoan(int id)
        {
            var user = db.NguoiDung.FirstOrDefault(x => x.MaNguoiDung == id);

            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy tài khoản" });

            user.IsActive = !user.IsActive;
            db.SaveChanges();

            return Json(new
            {
                success = true,
                message = user.IsActive ? "Đã mở khóa tài khoản" : "Đã khóa tài khoản"
            });
        }
        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemBanner(Banner model, HttpPostedFileBase fileUpload)
        {

            if (ModelState.IsValid)
            {
                try
                {
                    if (fileUpload != null && fileUpload.ContentLength > 0)
                    {
                        string extension = Path.GetExtension(fileUpload.FileName);
                        string fileName = Guid.NewGuid() + extension;

                        string folder = Server.MapPath("~/AnhWeb");

                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }

                        string path = Path.Combine(folder, fileName);
                        fileUpload.SaveAs(path);

                        model.HinhAnh = "/AnhWeb/" + fileName;
                    }

                    db.Banner.Add(model);
                    db.SaveChanges();

                    TempData["Message"] = "Thêm banner thành công!";
                    return RedirectToAction("QuanLyBanner");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = "Lỗi: " + ex.Message;
                }
            }

            return View(model);
        }
        // QUẢN LÝ ĐƠN HÀNG - Admin Controller
        public ActionResult DanhSachDonHang(string search, string productFilter, string statusFilter,
      DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 10)
        {


            // Danh sách trạng thái
            ViewBag.StatusList = new SelectList(new[]
            {

        new SelectListItem { Value = "Chờ thanh toán", Text = "Chờ thanh toán" },
        new SelectListItem { Value = "Đã xác nhận", Text = "Đã xác nhận" },
        new SelectListItem { Value = "Đang giao", Text = "Đang giao" },
        new SelectListItem { Value = "Hoàn thành", Text = "Hoàn thành" },
        new SelectListItem { Value = "Đã hủy", Text = "Đã hủy" }
    }, "Value", "Text", statusFilter);

            ViewBag.SelectedStatus = statusFilter;

            try
            {
                var orders = db.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHangs.Select(od => od.ChiTietSanPham.SanPham))
                    .AsQueryable();

                // Tìm kiếm
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    orders = orders.Where(o =>
                        o.MaDonHang.ToString().Contains(search) ||
                        (o.NguoiDung != null && o.NguoiDung.Ten.ToLower().Contains(search)) ||
                        (o.TrangThai != null && o.TrangThai.ToLower().Contains(search)));
                }

                // Lọc theo tên sản phẩm
                if (!string.IsNullOrEmpty(productFilter))
                {
                    productFilter = productFilter.ToLower();
                    orders = orders.Where(o => o.ChiTietDonHangs.Any(od =>
                        od.ChiTietSanPham != null &&
                        od.ChiTietSanPham.SanPham != null &&
                        od.ChiTietSanPham.SanPham.TenSanPham.ToLower().Contains(productFilter)));
                }

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    orders = orders.Where(o => o.TrangThai == statusFilter);
                }

                // Lọc theo ngày
                if (fromDate.HasValue)
                    orders = orders.Where(o => o.NgayDat >= fromDate.Value);

                if (toDate.HasValue)
                    orders = orders.Where(o => o.NgayDat <= toDate.Value.AddDays(1).AddSeconds(-1));

                int totalOrders = orders.Count();
                int totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

                ViewBag.TotalPages = totalPages;
                ViewBag.CurrentPage = page;
                ViewBag.Search = search;
                ViewBag.ProductFilter = productFilter;
                ViewBag.StatusFilter = statusFilter;
                ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

                var result = orders
                    .OrderByDescending(o => o.MaDonHang)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Tính tổng tiền
                var tongTienDict = new Dictionary<int, decimal>();
                foreach (var order in result)
                {
                    decimal tongTien = order.ChiTietDonHangs.Sum(od => (od.Gia ?? 0) * (od.SoLuong ?? 0));
                    tongTienDict[order.MaDonHang] = tongTien;
                }

                ViewBag.TongTienDict = tongTienDict;

                return View(result);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Lỗi khi tải danh sách đơn hàng: " + ex.Message;
                return View(new List<DonHang>());
            }
        }
        public ActionResult ChiTietDonHang(int id)
        {


            var donHang = db.DonHangs
                .Include(o => o.NguoiDung)
                .Include(o => o.ChiTietDonHangs.Select(od => od.ChiTietSanPham.SanPham))
                .Include(o => o.ChiTietDonHangs.Select(od => od.ChiTietSanPham.ThuongHieu))
                .Include(o => o.ChiTietDonHangs.Select(od => od.ChiTietSanPham.MauSac))
                .FirstOrDefault(o => o.MaDonHang == id);

            if (donHang == null)
            {
                return HttpNotFound();
            }

            ViewBag.OrderStatuses = new List<SelectListItem>
    {
        new SelectListItem { Value = "Chờ thanh toán", Text = "Chờ thanh toán" },
        new SelectListItem { Value = "Đã xác nhận", Text = "Đã xác nhận" },
        new SelectListItem { Value = "Đang giao", Text = "Đang giao" },
        new SelectListItem { Value = "Hoàn thành", Text = "Hoàn thành" },
        new SelectListItem { Value = "Đã hủy", Text = "Đã hủy" }
    };

            return View(donHang);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TrangThaiDonHang(int id, string trangThaiMoi)
        {


            try
            {
                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                // ================== KIỂM TRA NGHIÊM NGẶT ==================
                if (donHang.TrangThai == "Hoàn thành")
                {
                    TempData["Message"] = "Đơn hàng đã hoàn thành, không thể thay đổi trạng thái nữa!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                // (Tùy chọn) Không cho hủy nếu đang giao
                if (trangThaiMoi == "Đã hủy" && donHang.TrangThai == "Đang giao")
                {
                    TempData["Message"] = "Không thể hủy đơn hàng đang trong quá trình giao hàng!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                // Nếu qua hết kiểm tra thì mới cập nhật
                donHang.TrangThai = trangThaiMoi;
                db.SaveChanges();

                TempData["Message"] = $"Cập nhật trạng thái thành **{trangThaiMoi}** thành công!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi cập nhật: " + ex.Message;
            }

            return RedirectToAction("ChiTietDonHang", new { id = id });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XoaDonHang(int id)
        {


            try
            {
                // Load đầy đủ các bảng liên quan
                var donHang = db.DonHangs
                    .Include(o => o.ChiTietDonHangs)
                    .Include(o => o.ThanhToans)        // ← Bắt buộc phải thêm dòng này
                    .FirstOrDefault(o => o.MaDonHang == id);

                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng để xóa!";
                    return RedirectToAction("DanhSachDonHang");
                }

                // Xóa Chi tiết đơn hàng
                if (donHang.ChiTietDonHangs != null && donHang.ChiTietDonHangs.Any())
                {
                    db.ChiTietDonHangs.RemoveRange(donHang.ChiTietDonHangs);
                }

                // Xóa Thanh toán (rất quan trọng)
                if (donHang.ThanhToans != null && donHang.ThanhToans.Any())
                {
                    db.ThanhToans.RemoveRange(donHang.ThanhToans);
                }

                // Cuối cùng mới xóa đơn hàng
                db.DonHangs.Remove(donHang);

                db.SaveChanges();

                TempData["Message"] = "Xóa đơn hàng thành công!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi xóa đơn hàng: " + ex.Message;
                if (ex.InnerException != null)
                {
                    TempData["InnerMessage"] = "Chi tiết lỗi: " + ex.InnerException.Message;
                }
            }

            return RedirectToAction("DanhSachDonHang");
        }
        public ActionResult QuanLyTinTuc()
        {
            var list = db.TinTucs.OrderByDescending(x => x.NgayDang).ToList();
            return View(list);
        }

        // 📌 Thêm tin tức (GET)
        public ActionResult Create()
        {
            return View();
        }

        // 📌 Thêm tin tức (POST)
        [HttpPost]
        public ActionResult Create(TinTuc model)
        {
            if (ModelState.IsValid)
            {
                model.NgayDang = DateTime.Now;
                db.TinTucs.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyTinTuc");
            }
            return View(model);
        }

        // 📌 Sửa
        public ActionResult Edit(int id)
        {
            var tin = db.TinTucs.Find(id);
            return View(tin);
        }

        [HttpPost]
        public ActionResult Edit(TinTuc model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("QuanLyTinTuc");
            }
            return View(model);
        }

        // 📌 Xóa
        public ActionResult Delete(int id)
        {
            var tin = db.TinTucs.Find(id);
            db.TinTucs.Remove(tin);
            db.SaveChanges();
            return RedirectToAction("QuanLyTinTuc");
        }

        // 📌 Chi tiết
        public ActionResult Details(int id)
        {
            var tin = db.TinTucs.Find(id);
            return View(tin);
        }
        // 📌 Danh sách đánh giá
        public ActionResult Danhsachdanhgia()
        {
            var list = db.DanhGias
                         .Include("NguoiDung")
                         .Include("SanPham")
                         .OrderByDescending(x => x.NgayDanhGia)
                         .ToList();

            return View(list);
        }

        // 📌 Xem chi tiết
        public ActionResult chitietdanhgia(int id)
        {
            var dg = db.DanhGias
                       .Include("NguoiDung")
                       .Include("SanPham")
                       .FirstOrDefault(x => x.MaDanhGia == id);

            return View(dg);
        }

        // 📌 Xóa đánh giá
        public ActionResult xoadanhgia(int id)
        {
            var dg = db.DanhGias.Find(id);
            db.DanhGias.Remove(dg);
            db.SaveChanges();
            return RedirectToAction("Danhsachdanhgia");
        }
        // 📌 Danh sách
        public ActionResult QuanLyKhuyenMai()
        {
            var list = db.KhuyenMais
                         .OrderByDescending(x => x.NgayBatDau)
                         .ToList();

            return View(list);
        }

        // 📌 Thêm (GET)
        public ActionResult themkm()
        {
            return View();
        }

        // 📌 Thêm (POST)
        [HttpPost]
        public ActionResult themkm(KhuyenMai model)
        {
            if (ModelState.IsValid)
            {
                db.KhuyenMais.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyKhuyenMai");
            }
            return View(model);
        }

        // 📌 Sửa
        public ActionResult suakm(int id)
        {
            var km = db.KhuyenMais.Find(id);
            return View(km);
        }

        [HttpPost]
        public ActionResult suakm(KhuyenMai model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("QuanLyKhuyenMai");
            }
            return View(model);
        }

        // 📌 Xóa
        public ActionResult xoakm(int id)
        {
            var km = db.KhuyenMais.Find(id);
            db.KhuyenMais.Remove(km);
            db.SaveChanges();
            return RedirectToAction("QuanLyKhuyenMai");
        }
        // 📌 Danh sách
        public ActionResult QuanLyDanhMuc()
        {
            var list = db.DanhMucs.ToList();
            return View(list);
        }

        // 📌 Thêm
        public ActionResult themdm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult themdm(DanhMuc model)
        {
            if (ModelState.IsValid)
            {
                db.DanhMucs.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyDanhMuc");
            }
            return View(model);
        }

        // 📌 Sửa
        public ActionResult suadm(int id)
        {
            var dm = db.DanhMucs.Find(id);
            return View(dm);
        }

        [HttpPost]
        public ActionResult suadm(DanhMuc model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("QuanLyDanhMuc");
            }
            return View(model);
        }

        // 📌 Xóa
        public ActionResult xoadm(int id)
        {
            var dm = db.DanhMucs.Find(id);
            db.DanhMucs.Remove(dm);
            db.SaveChanges();
            return RedirectToAction("QuanLyDanhMuc");
        }
        public ActionResult QuanLyThuongHieu()
        {
            return View(db.ThuongHieux.ToList());
        }

        public ActionResult themth()
        {
            return View();
        }

        [HttpPost]
        public ActionResult themth(ThuongHieu model)
        {
            if (ModelState.IsValid)
            {
                db.ThuongHieux.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyThuongHieu");
            }
            return View(model);
        }

        public ActionResult suath(int id)
        {
            var th = db.ThuongHieux.Find(id);
            return View(th);
        }

        [HttpPost]
        public ActionResult suath(ThuongHieu model)
        {
            if (ModelState.IsValid)
            {
                db.Entry(model).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("QuanLyThuongHieu");
            }
            return View(model);
        }

        public ActionResult xoath(int id)
        {
            var th = db.ThuongHieux.Find(id);
            db.ThuongHieux.Remove(th);
            db.SaveChanges();
            return RedirectToAction("QuanLyThuongHieu");
        }
    }
}
