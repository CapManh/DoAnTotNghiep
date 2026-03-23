using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;

namespace DoAnTotNghiep.Models
{
    public partial class Model1 : DbContext
    {
        public Model1()
            : base("name=Model1")
        {
        }

        public virtual DbSet<Banner> Banner { get; set; }
        public virtual DbSet<ChatLieu> ChatLieu { get; set; }
        public virtual DbSet<ChiTietDonHang> ChiTietDonHang { get; set; }
        public virtual DbSet<ChiTietGioHang> ChiTietGioHang { get; set; }
        public virtual DbSet<ChiTietSanPham> ChiTietSanPham { get; set; }
        public virtual DbSet<DanhGia> DanhGia { get; set; }
        public virtual DbSet<DanhMuc> DanhMuc { get; set; }
        public virtual DbSet<DonHang> DonHang { get; set; }
        public virtual DbSet<GioHang> GioHang { get; set; }
        public virtual DbSet<KhuyenMai> KhuyenMai { get; set; }
        public virtual DbSet<MauSac> MauSac { get; set; }
        public virtual DbSet<NguoiDung> NguoiDung { get; set; }
        public virtual DbSet<PhuongThucThanhToan> PhuongThucThanhToan { get; set; }
        public virtual DbSet<SanPham> SanPham { get; set; }
        public virtual DbSet<TinTuc> TinTuc { get; set; }
        public virtual DbSet<ThanhToan> ThanhToan { get; set; }
        public virtual DbSet<ThuongHieu> ThuongHieu { get; set; }
        public virtual DbSet<VaiTro> VaiTro { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChiTietSanPham>()
                .HasMany(e => e.ChiTietDonHang)
                .WithOptional(e => e.ChiTietSanPham)
                .HasForeignKey(e => e.MaChiTietSanPham);

            modelBuilder.Entity<ChiTietSanPham>()
                .HasMany(e => e.ChiTietGioHang)
                .WithOptional(e => e.ChiTietSanPham)
                .HasForeignKey(e => e.MaChiTietSanPham);
        }
    }
}
