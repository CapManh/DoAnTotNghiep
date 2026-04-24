using DoAnTotNghiep.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
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
        public ActionResult Thongke(string filter = "day", DateTime? selectedDate = null)
        {
            DateTime startDate;
            DateTime endDate;

            DateTime today = selectedDate ?? DateTime.Today;

            if (filter == "day")
            {
                startDate = today.Date;
                endDate = startDate.AddDays(1);
            }
            else if (filter == "week")
            {
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = today.AddDays(-diff).Date;
                endDate = startDate.AddDays(7);

                ViewBag.WeekRange = startDate.ToString("dd/MM/yyyy") +
                    " - " + endDate.AddDays(-1).ToString("dd/MM/yyyy");
            }
            else if (filter == "month")
            {
                startDate = new DateTime(today.Year, today.Month, 1);
                endDate = startDate.AddMonths(1);
            }
            else
            {
                startDate = new DateTime(today.Year, 1, 1);
                endDate = startDate.AddYears(1);
            }

            var doanhThu = db.DonHangs
                .Where(o => o.NgayDat >= startDate && o.NgayDat < endDate)
                .SelectMany(o => o.ChiTietDonHangs)
                .Sum(od => (decimal?)(od.Gia * od.SoLuong)) ?? 0;

            ViewBag.DoanhThu = doanhThu;
            ViewBag.Filter = filter;
            ViewBag.SelectedDate = selectedDate ?? today;

            var dataChart = db.DonHangs
                .Where(o => o.NgayDat != null)
                .SelectMany(o => o.ChiTietDonHangs.Select(od => new
                {
                    Ngay = o.NgayDat.Value,
                    Tien = (decimal)(od.Gia * od.SoLuong)
                }))
                .ToList();

            List<string> labels = new List<string>();
            List<decimal> data = new List<decimal>();

            if (filter == "day")
            {
                var group = dataChart
                    .Where(x => x.Ngay.Date == startDate)
                    .GroupBy(x => x.Ngay.Hour)
                    .Select(g => new { Gio = g.Key, Tien = g.Sum(x => x.Tien) })
                    .OrderBy(x => x.Gio);

                labels = group.Select(x => x.Gio + "h").ToList();
                data = group.Select(x => x.Tien).ToList();
            }
            else if (filter == "week")
            {
                var group = dataChart
                    .GroupBy(x =>
                    {
                        int diff = (7 + (x.Ngay.DayOfWeek - DayOfWeek.Monday)) % 7;
                        return x.Ngay.AddDays(-diff).Date;
                    })
                    .Select(g => new
                    {
                        StartWeek = g.Key,
                        EndWeek = g.Key.AddDays(6),
                        Tien = g.Sum(x => x.Tien)
                    })
                    .OrderBy(x => x.StartWeek);

                labels = group
                    .Select(x => x.StartWeek.ToString("dd/MM") + " - " + x.EndWeek.ToString("dd/MM"))
                    .ToList();

                data = group.Select(x => x.Tien).ToList();
            }
            else if (filter == "month")
            {
                var group = dataChart
                    .GroupBy(x => new { x.Ngay.Year, x.Ngay.Month })
                    .Select(g => new
                    {
                        Thang = g.Key.Month,
                        Nam = g.Key.Year,
                        Tien = g.Sum(x => x.Tien)
                    })
                    .OrderBy(x => x.Nam).ThenBy(x => x.Thang);

                labels = group.Select(x => "T" + x.Thang + "/" + x.Nam).ToList();
                data = group.Select(x => x.Tien).ToList();
            }
            else
            {
                var group = dataChart
                    .GroupBy(x => x.Ngay.Year)
                    .Select(g => new
                    {
                        Nam = g.Key,
                        Tien = g.Sum(x => x.Tien)
                    })
                    .OrderBy(x => x.Nam);

                labels = group.Select(x => x.Nam.ToString()).ToList();
                data = group.Select(x => x.Tien).ToList();
            }

            var topSP = db.DonHangs
                .Where(o => o.NgayDat >= startDate && o.NgayDat < endDate)
                .SelectMany(o => o.ChiTietDonHangs)
                .Join(db.ChiTietSanPhams,
                    ct => ct.MaChiTietSanPham,
                    cts => cts.MaChiTiet,
                    (ct, cts) => new { ct, cts })
                .Join(db.SanPhams,
                    x => x.cts.MaSanPham,
                    sp => sp.MaSanPham,
                    (x, sp) => new
                    {
                        TenSP = sp.TenSanPham,
                        SoLuong = x.ct.SoLuong,
                        Gia = x.ct.Gia
                    })
                .GroupBy(x => x.TenSP)
                .Select(g => new
                {
                    TenSP = g.Key,
                    TongBan = g.Sum(x => x.SoLuong),
                    DoanhThu = g.Sum(x => x.SoLuong * x.Gia)
                })
                .OrderByDescending(x => x.TongBan)
                .Take(5)
                .ToList();

            var soDon = db.DonHangs
                .Count(o => o.NgayDat >= startDate && o.NgayDat < endDate);

            ViewBag.SoDon = soDon;

            var tongSP = db.DonHangs
                .Where(o => o.NgayDat >= startDate && o.NgayDat < endDate)
                .SelectMany(o => o.ChiTietDonHangs)
                .Sum(x => (int?)x.SoLuong) ?? 0;

            ViewBag.TongSP = tongSP;

            DateTime prevStart = startDate.AddDays(-(endDate - startDate).Days);
            DateTime prevEnd = startDate;

            var doanhThuCu = db.DonHangs
                .Where(o => o.NgayDat >= prevStart && o.NgayDat < prevEnd)
                .SelectMany(o => o.ChiTietDonHangs)
                .Sum(x => (decimal?)(x.Gia * x.SoLuong)) ?? 0;

            decimal growth = 0;
            if (doanhThuCu > 0)
            {
                growth = ((ViewBag.DoanhThu - doanhThuCu) / doanhThuCu) * 100;
            }

            ViewBag.Growth = growth;

            var topKhachHang = db.DonHangs
     .Where(o => o.NgayDat >= startDate && o.NgayDat < endDate)
     .GroupBy(o => o.MaNguoiDung)
     .Select(g => new
     {
         MaNguoiDung = g.Key,
         SoDonHang = g.Count(),
         TongTien = g.SelectMany(o => o.ChiTietDonHangs)
                     .Sum(ct => (decimal?)(ct.Gia * ct.SoLuong)) ?? 0
     })
     .OrderByDescending(x => x.SoDonHang)
     .Take(5)
     .ToList();

            var listTopKhachHang = topKhachHang.Select(item =>
            {
                dynamic obj = new System.Dynamic.ExpandoObject();
                var user = db.NguoiDungs.FirstOrDefault(u => u.MaNguoiDung == item.MaNguoiDung);
                obj.FullName = user?.Ten ?? "Không rõ";
                obj.SoDonHang = item.SoDonHang;
                obj.TongTien = item.TongTien;
                return obj;
            }).ToList();

            ViewBag.TopKhachHang = listTopKhachHang;

            // ================== PHẦN BỔ SUNG ==================

            // ================== KHÁCH HÀNG MỚI ==================
            var khachHangMoi = db.NguoiDungs
      .Where(x => x.NgayTao != null
               && x.NgayTao >= startDate
               && x.NgayTao < endDate)
      .OrderByDescending(x => x.NgayTao)
      .Take(5)
      .Select(x => new
      {
          x.Ten,
          x.Email,
          x.NgayTao
      })
      .ToList();

            ViewBag.KhachHangMoi = khachHangMoi;
            // ================== SẢN PHẨM HẾT / SẮP HẾT ==================
            var tonKho = db.ChiTietSanPhams
                .Join(db.SanPhams,
                    ct => ct.MaSanPham,
                    sp => sp.MaSanPham,
                    (ct, sp) => new
                    {
                        sp.TenSanPham,
                        ct.SoLuongTon
                    })
                .ToList();

            ViewBag.HetHang = tonKho.Where(x => x.SoLuongTon == 0).ToList();
            ViewBag.SapHet = tonKho.Where(x => x.SoLuongTon > 0 && x.SoLuongTon <= 5).ToList();


            // ================== ĐÁNH GIÁ TRUNG BÌNH ==================
            var danhGiaTB = db.DanhGias
      .GroupBy(x => x.MaSanPham)
      .Select(g => new
      {
          MaSanPham = g.Key,
          SoSaoTB = g.Average(x => x.SoSao),
          SoLuot = g.Count()
      })
      .ToList();
            var danhGiaView = db.DanhGias
                .Where(x => x.SoSao != null)
                .OrderByDescending(x => x.NgayDanhGia)
                .Select(x => new
                {
                    TenSP = x.SanPham.TenSanPham,
                    SoSao = x.SoSao ?? 0,
                    NgayDanhGia = x.NgayDanhGia
                })
                .ToList();

            ViewBag.DanhGiaSP = danhGiaView;
            ViewBag.DanhGiaTB = danhGiaTB;



            // ================== TỶ LỆ TRẠNG THÁI ĐƠN HÀNG ==================
            var trangThaiDonHang = db.DonHangs
      .Where(x => x.NgayDat >= startDate && x.NgayDat < endDate)
      .GroupBy(x => x.TrangThai)
      .Select(g => new
      {
          TrangThai = g.Key,
          SoLuong = g.Count()
      })
      .ToList();

            ViewBag.OrderStatusLabels = trangThaiDonHang.Select(x => x.TrangThai).ToList();
            ViewBag.OrderStatusData = trangThaiDonHang.Select(x => x.SoLuong).ToList();
            var tongDon = db.DonHangs
    .Where(x => x.NgayDat >= startDate && x.NgayDat < endDate)
    .Count();

            // đơn thành công
            var donThanhCong = db.DonHangs
                .Where(x => x.NgayDat >= startDate && x.NgayDat < endDate
                    && x.TrangThai.Contains("Hoàn thành"))
                .Count();

            // đơn thất bại (hủy)
            var donThatBai = db.DonHangs
                .Where(x => x.NgayDat >= startDate && x.NgayDat < endDate
                    && x.TrangThai.Contains("hủy"))
                .Count();

            decimal tyLeThanhCong = tongDon > 0 ? (decimal)donThanhCong / tongDon * 100 : 0;
            decimal tyLeThatBai = tongDon > 0 ? (decimal)donThatBai / tongDon * 100 : 0;

            ViewBag.TyLeThanhCong = tyLeThanhCong;
            ViewBag.TyLeThatBai = tyLeThatBai;
            // ==================

            ViewBag.TopLabels = topSP.Select(x => x.TenSP).ToList();
            ViewBag.TopSL = topSP.Select(x => x.TongBan).ToList();
            ViewBag.TopDT = topSP.Select(x => x.DoanhThu).ToList();

            ViewBag.ChartLabels = labels;
            ViewBag.ChartData = data;

            return View();
        } // Danh sách liên hệ
        public ActionResult LienHe()
        {
            var ds = db.LienHes
                       .OrderByDescending(x => x.NgayGui)
                       .ToList();

            return View(ds);
        }

        // GET: xem chi tiết
        public ActionResult ChiTietLH(int id)
        {
            var lh = db.LienHes.Find(id);
            return View(lh);
        }

        // POST: cập nhật trạng thái ngay tại chi tiết
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChiTietLH(int id, string trangthai)
        {
            var lh = db.LienHes.Find(id);
            if (lh != null)
            {
                lh.TrangThai = trangthai;
                db.SaveChanges();
            }

            return RedirectToAction("ChiTietLH", new { id = id }); // quay lại chính nó
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GuiPhanHoi(int id, string noidung)
        {
            var lh = db.LienHes.Find(id);
            if (lh == null) return RedirectToAction("LienHe");

            try
            {
                var fromEmail = new MailAddress("buimanhtuan009@gmail.com", "Nội Thất Shop");
                var toEmail = new MailAddress(lh.Email);

                string subject = "Phản hồi liên hệ từ shop nội thất";
                string body = $"Xin chào {lh.HoTen},\n\n{noidung}\n\nTrân trọng!";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential("buimanhtuan009@gmail.com", "folvrenlfephvfew")
                };

                using (var message = new MailMessage(fromEmail, toEmail)
                {
                    Subject = subject,
                    Body = body
                })
                {
                    smtp.Send(message);
                }

                // Cập nhật trạng thái
                lh.TrangThai = "Đã xử lý";
                db.SaveChanges();

                TempData["msg"] = "Gửi email thành công!";
            }
            catch
            {
                TempData["msg"] = "Gửi email thất bại!";
            }

            return RedirectToAction("ChiTietLH", new { id = id });
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
                var orders = db.DonHang
                    .Include(o => o.NguoiDung)
                    .Include(o => o.ChiTietDonHang.Select(od => od.ChiTietSanPham.SanPham))
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
                    orders = orders.Where(o => o.ChiTietDonHang.Any(od =>
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
                    decimal tongTien = order.ChiTietDonHang.Sum(od => (od.Gia ?? 0) * (od.SoLuong ?? 0));
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


            var donHang = db.DonHang
                .Include(o => o.NguoiDung)
                .Include(o => o.ChiTietDonHang.Select(od => od.ChiTietSanPham.SanPham))
                .Include(o => o.ChiTietDonHang.Select(od => od.ChiTietSanPham.ThuongHieu))
                .Include(o => o.ChiTietDonHang.Select(od => od.ChiTietSanPham.MauSac))
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
                var donHang = db.DonHang
                    .Include(o => o.ChiTietDonHang.Select(cd => cd.ChiTietSanPham))
                    .FirstOrDefault(o => o.MaDonHang == id);

                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                if (donHang.TrangThai == "Hoàn thành" || donHang.TrangThai == "Đã hủy")
                {
                    TempData["Message"] = $"Đơn hàng đã ở trạng thái **{donHang.TrangThai}**, không thể thay đổi nữa!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }

                if (trangThaiMoi == "Đã hủy")
                {
                    foreach (var chiTiet in donHang.ChiTietDonHang)
                    {
                        if (chiTiet.ChiTietSanPham != null)
                        {
                            chiTiet.ChiTietSanPham.SoLuongTon += (chiTiet.SoLuong ?? 0);
                        }
                    }

                    donHang.TrangThai = "Đã hủy";
                    db.SaveChanges();

                    TempData["Message"] = "Đơn hàng đã được hủy thành công. Số lượng sản phẩm đã được trả lại kho!";
                    return RedirectToAction("ChiTietDonHang", new { id });
                }
                donHang.TrangThai = trangThaiMoi;
                db.SaveChanges();

                TempData["Message"] = $"Cập nhật trạng thái thành **{trangThaiMoi}** thành công!";
            }
            catch (Exception ex)
            {
                TempData["Message"] = "Lỗi khi cập nhật trạng thái: " + ex.Message;
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
                var donHang = db.DonHang
                    .Include(o => o.ChiTietDonHang)
                    .Include(o => o.ThanhToan)        // ← Bắt buộc phải thêm dòng này
                    .FirstOrDefault(o => o.MaDonHang == id);

                if (donHang == null)
                {
                    TempData["Message"] = "Không tìm thấy đơn hàng để xóa!";
                    return RedirectToAction("DanhSachDonHang");
                }

                // Xóa Chi tiết đơn hàng
                if (donHang.ChiTietDonHang != null && donHang.ChiTietDonHang.Any())
                {
                    db.ChiTietDonHang.RemoveRange(donHang.ChiTietDonHang);
                }

                // Xóa Thanh toán (rất quan trọng)
                if (donHang.ThanhToan != null && donHang.ThanhToan.Any())
                {
                    db.ThanhToan.RemoveRange(donHang.ThanhToan);
                }

                // Cuối cùng mới xóa đơn hàng
                db.DonHang.Remove(donHang);

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
            var list = db.TinTuc.OrderByDescending(x => x.NgayDang).ToList();
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
                db.TinTuc.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyTinTuc");
            }
            return View(model);
        }

        // 📌 Sửa
        public ActionResult Edit(int id)
        {
            var tin = db.TinTuc.Find(id);
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
            var tin = db.TinTuc.Find(id);
            db.TinTuc.Remove(tin);
            db.SaveChanges();
            return RedirectToAction("QuanLyTinTuc");
        }

        // 📌 Chi tiết
        public ActionResult Details(int id)
        {
            var tin = db.TinTuc.Find(id);
            return View(tin);
        }
        // 📌 Danh sách đánh giá
        public ActionResult Danhsachdanhgia()
        {
            var list = db.DanhGia
                         .Include("NguoiDung")
                         .Include("SanPham")
                         .OrderByDescending(x => x.NgayDanhGia)
                         .ToList();

            return View(list);
        }

        // 📌 Xem chi tiết
        public ActionResult chitietdanhgia(int id)
        {
            var dg = db.DanhGia
                       .Include("NguoiDung")
                       .Include("SanPham")
                       .FirstOrDefault(x => x.MaDanhGia == id);

            return View(dg);
        }

        // 📌 Xóa đánh giá
        public ActionResult xoadanhgia(int id)
        {
            var dg = db.DanhGia.Find(id);
            db.DanhGia.Remove(dg);
            db.SaveChanges();
            return RedirectToAction("Danhsachdanhgia");
        }
        public ActionResult QuanLyKhuyenMai()
        {
            var list = db.KhuyenMai
                         .OrderByDescending(x => x.NgayBatDau)
                         .ToList();

            return View(list);
        }

        public ActionResult themkm()
        {
            return View();
        }
        [HttpPost]
        public ActionResult themkm(KhuyenMai model)
        {
            if (ModelState.IsValid)
            {
                db.KhuyenMai.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyKhuyenMai");
            }
            return View(model);
        }
        public ActionResult suakm(int id)
        {
            var km = db.KhuyenMai.Find(id);
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
            var km = db.KhuyenMai.Find(id);
            db.KhuyenMai.Remove(km);
            db.SaveChanges();
            return RedirectToAction("QuanLyKhuyenMai");
        }
        // 📌 Danh sách
        public ActionResult QuanLyDanhMuc()
        {
            var list = db.DanhMuc.ToList();
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
                db.DanhMuc.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyDanhMuc");
            }
            return View(model);
        }

        public ActionResult suadm(int id)
        {
            var dm = db.DanhMuc.Find(id);
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

        public ActionResult xoadm(int id)
        {
            var dm = db.DanhMuc.Find(id);
            db.DanhMuc.Remove(dm);
            db.SaveChanges();
            return RedirectToAction("QuanLyDanhMuc");
        }
        public ActionResult QuanLyThuongHieu()
        {
            return View(db.ThuongHieu.ToList());
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
                db.ThuongHieu.Add(model);
                db.SaveChanges();
                return RedirectToAction("QuanLyThuongHieu");
            }
            return View(model);
        }

        public ActionResult suath(int id)
        {
            var th = db.ThuongHieu.Find(id);
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
            var th = db.ThuongHieu.Find(id);

            bool dangSuDung = db.ChiTietSanPham
                .Any(x => x.MaThuongHieu == id);

            if (dangSuDung)
            {
                TempData["Error"] =
                    "Thương hiệu đang có sản phẩm!";
                return RedirectToAction("QuanLyThuongHieu");
            }

            db.ThuongHieu.Remove(th);
            db.SaveChanges();

            return RedirectToAction("QuanLyThuongHieu");
        }
    }
}
