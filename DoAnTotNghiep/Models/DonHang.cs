namespace DoAnTotNghiep.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("DonHang")]
    public partial class DonHang
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DonHang()
        {
            ChiTietDonHang = new HashSet<ChiTietDonHang>();
            ThanhToan = new HashSet<ThanhToan>();
        }

        [Key]
        public int MaDonHang { get; set; }

        public int? MaNguoiDung { get; set; }

        public decimal? TongTien { get; set; }

        [StringLength(50)]
        public string TrangThai { get; set; }

        [StringLength(255)]
        public string DiaChiGiao { get; set; }

        public DateTime? NgayDat { get; set; }
        public DateTime? NgayGiaoHang { get; set; }

        public int? MaPhuongThuc { get; set; }

        public int? MaKhuyenMai { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChiTietDonHang> ChiTietDonHang { get; set; }

        public virtual NguoiDung NguoiDung { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ThanhToan> ThanhToan { get; set; }

        public virtual KhuyenMai KhuyenMai { get; set; }
    }
}
