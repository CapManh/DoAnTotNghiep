namespace DoAnTotNghiep.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SanPham")]
    public partial class SanPham
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SanPham()
        {
            ChiTietSanPham = new HashSet<ChiTietSanPham>();
            DanhGia = new HashSet<DanhGia>();
            GioHang = new HashSet<GioHang>();
        }

        [Key]
        public int MaSanPham { get; set; }

        [StringLength(200)]
        public string TenSanPham { get; set; }

        public decimal? Gia { get; set; }

        public string MoTa { get; set; }

        [StringLength(255)]
        public string AnhChinh { get; set; }

        [StringLength(255)]
        public string AnhPhu1 { get; set; }

        [StringLength(255)]
        public string AnhPhu2 { get; set; }

        [StringLength(255)]
        public string AnhPhu3 { get; set; }

        public int? MaDanhMuc { get; set; }

        public bool? NoiBat { get; set; }

        public DateTime? NgayTao { get; set; }

        public int? MaGiamGia { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ChiTietSanPham> ChiTietSanPham { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DanhGia> DanhGia { get; set; }

        public virtual DanhMuc DanhMuc { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GioHang> GioHang { get; set; }

        public virtual KhuyenMai KhuyenMai { get; set; }
    }
}
