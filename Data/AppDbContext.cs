using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QLNS.Models.Entities;

namespace QLNS.Data
{
    public class AppDbContext : IdentityDbContext<TaiKhoan, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Bảng cũ
        public DbSet<NhanVien> NhanViens { get; set; }
        public DbSet<ChucVu> ChucVus { get; set; }
        public DbSet<PhongBan> PhongBans { get; set; }
        public DbSet<Luong> Luongs { get; set; }
        public DbSet<ChamCong> ChamCongs { get; set; }
        public DbSet<HopDong> HopDongs { get; set; }
        public DbSet<TaiLieuHopDong> TaiLieuHopDongs { get; set; }
        public DbSet<LoaiHopDong> LoaiHopDongs { get; set; }
        public DbSet<NghiPhep> NghiPheps { get; set; }
        public DbSet<NghiPhepMinhChung> NghiPhepMinhChungs { get; set; }

        // Bảng bổ sung
        public DbSet<PhuCap> PhuCaps { get; set; }
        public DbSet<ChinhSachPhuCap> ChinhSachPhuCaps { get; set; }
        public DbSet<ChinhSachBhxh> ChinhSachBhxhs { get; set; }
        public DbSet<TaiKhoan> TaiKhoans { get; set; }
        public DbSet<LoaiBaoHiem> LoaiBaoHiems { get; set; }
        public DbSet<BaoHiem> BaoHiems { get; set; }
        public DbSet<ThuongPhat> ThuongPhats { get; set; }
        public DbSet<NhatKyHeThong> NhatKyHeThongs { get; set; }
        public DbSet<TrinhDoChungChi> TrinhDoChungChis { get; set; }
        public DbSet<QuaTrinhCongTac> QuaTrinhCongTacs { get; set; }
        public DbSet<TaiLieuNhanVien> TaiLieuNhanViens { get; set; }
        public DbSet<LoaiNghiPhep> LoaiNghiPheps { get; set; }
        public DbSet<SoDuNghiPhep> SoDuNghiPheps { get; set; }
        public DbSet<DieuChinhChamCong> DieuChinhChamCongs { get; set; }
        public DbSet<DieuChinhChamCongMinhChung> DieuChinhChamCongMinhChungs { get; set; }
        public DbSet<YeuCauCapNhatThongTinCaNhan> YeuCauCapNhatThongTinCaNhans { get; set; }
        public DbSet<ThongBaoCaNhan> ThongBaoCaNhans { get; set; }
        public DbSet<CaLamViec> CaLamViecs { get; set; }
        public DbSet<PhanCaNhanVien> PhanCaNhanViens { get; set; }
        public DbSet<DangKyTangCa> DangKyTangCas { get; set; }
        public DbSet<DangKyTangCaMinhChung> DangKyTangCaMinhChungs { get; set; }
        public DbSet<CongTac> CongTacs { get; set; }
        public DbSet<PhanCongCongTac> PhanCongCongTacs { get; set; }
        public DbSet<TaiLieuNoiBo> TaiLieuNoiBos { get; set; }
        public DbSet<TepTaiLieuNoiBo> TepTaiLieuNoiBos { get; set; }
        public DbSet<XacNhanTaiLieu> XacNhanTaiLieus { get; set; }
        public DbSet<ThongBaoNoiBo> ThongBaoNoiBos { get; set; }
        public DbSet<BinhLuanThongBao> BinhLuanThongBaos { get; set; }
        public DbSet<KhaoSatNoiBo> KhaoSatNoiBos { get; set; }
        public DbSet<LuaChonKhaoSat> LuaChonKhaoSats { get; set; }
        public DbSet<PhieuKhaoSat> PhieuKhaoSats { get; set; }
        public DbSet<QuyTrinhPheDuyet> QuyTrinhPheDuyets { get; set; }
        public DbSet<BuocPheDuyet> BuocPheDuyets { get; set; }
        public DbSet<YeuCauNhanLuc> YeuCauNhanLuc { get; set; }
        public DbSet<DanhGiaHieuSuat> DanhGiaHieuSuat { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Tên bảng
            modelBuilder.Entity<NhanVien>().ToTable("NhanVien");
            modelBuilder.Entity<ChucVu>().ToTable("ChucVu");
            modelBuilder.Entity<PhongBan>().ToTable("PhongBan");
            modelBuilder.Entity<Luong>().ToTable("Luong");
            modelBuilder.Entity<ChamCong>().ToTable("ChamCong");
            modelBuilder.Entity<HopDong>().ToTable("HopDong");
            modelBuilder.Entity<TaiLieuHopDong>().ToTable("TaiLieuHopDong");
            modelBuilder.Entity<LoaiHopDong>().ToTable("LoaiHopDong");
            modelBuilder.Entity<NghiPhep>().ToTable("NghiPhep");
            modelBuilder.Entity<NghiPhepMinhChung>().ToTable("NghiPhepMinhChung");
            modelBuilder.Entity<PhuCap>().ToTable("PhuCap");
            modelBuilder.Entity<ChinhSachPhuCap>().ToTable("ChinhSachPhuCap");
            modelBuilder.Entity<ChinhSachBhxh>().ToTable("ChinhSachBHXH");
            modelBuilder.Entity<TaiKhoan>().ToTable("TaiKhoan");
            modelBuilder.Entity<IdentityRole<int>>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole<int>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("AspNetRoleClaims");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("AspNetUserTokens");

            // Tái sử dụng khóa và mật khẩu của bảng TaiKhoan hiện có.
            modelBuilder.Entity<TaiKhoan>().Property(x => x.Id).HasColumnName("MaTaiKhoan");
            modelBuilder.Entity<TaiKhoan>().Property(x => x.PasswordHash).HasColumnName("Pass");
            modelBuilder.Entity<LoaiBaoHiem>().ToTable("LoaiBaoHiem");
            modelBuilder.Entity<BaoHiem>().ToTable("BaoHiem");
            modelBuilder.Entity<ThuongPhat>().ToTable("ThuongPhat");
            modelBuilder.Entity<NhatKyHeThong>().ToTable("NhatKyHeThong");
            modelBuilder.Entity<TrinhDoChungChi>().ToTable("TrinhDoChungChi");
            modelBuilder.Entity<QuaTrinhCongTac>().ToTable("QuaTrinhCongTac");
            modelBuilder.Entity<TaiLieuNhanVien>().ToTable("TaiLieuNhanVien");
            modelBuilder.Entity<LoaiNghiPhep>().ToTable("LoaiNghiPhep");
            modelBuilder.Entity<SoDuNghiPhep>().ToTable("SoDuNghiPhep");
            modelBuilder.Entity<DieuChinhChamCong>().ToTable("DieuChinhChamCong");
            modelBuilder.Entity<DieuChinhChamCongMinhChung>().ToTable("DieuChinhChamCongMinhChung");
            modelBuilder.Entity<YeuCauCapNhatThongTinCaNhan>().ToTable("YeuCauCapNhatThongTinCaNhan");
            modelBuilder.Entity<ThongBaoCaNhan>().ToTable("ThongBaoCaNhan");
            modelBuilder.Entity<CaLamViec>().ToTable("CaLamViec");
            modelBuilder.Entity<PhanCaNhanVien>().ToTable("PhanCaNhanVien");
            modelBuilder.Entity<DangKyTangCa>().ToTable("DangKyTangCa");
            modelBuilder.Entity<DangKyTangCaMinhChung>().ToTable("DangKyTangCaMinhChung");
            modelBuilder.Entity<CongTac>().ToTable("CongTac");
            modelBuilder.Entity<PhanCongCongTac>().ToTable("PhanCongCongTac");
            modelBuilder.Entity<TaiLieuNoiBo>().ToTable("TaiLieuNoiBo");modelBuilder.Entity<XacNhanTaiLieu>().ToTable("XacNhanTaiLieu");
            modelBuilder.Entity<TepTaiLieuNoiBo>().ToTable("TepTaiLieuNoiBo");
            modelBuilder.Entity<ThongBaoNoiBo>().ToTable("ThongBaoNoiBo");modelBuilder.Entity<BinhLuanThongBao>().ToTable("BinhLuanThongBao");
            modelBuilder.Entity<KhaoSatNoiBo>().ToTable("KhaoSatNoiBo");modelBuilder.Entity<LuaChonKhaoSat>().ToTable("LuaChonKhaoSat");modelBuilder.Entity<PhieuKhaoSat>().ToTable("PhieuKhaoSat");
            modelBuilder.Entity<QuyTrinhPheDuyet>().ToTable("QuyTrinhPheDuyet");modelBuilder.Entity<BuocPheDuyet>().ToTable("BuocPheDuyet");
            modelBuilder.Entity<YeuCauNhanLuc>().ToTable("YeuCauNhanLuc");
            modelBuilder.Entity<DanhGiaHieuSuat>().ToTable("DanhGiaHieuSuat");

            // Computed property không lưu DB
            modelBuilder.Entity<ChamCong>().Ignore(c => c.SoGioLam);

            // TaiKhoan: mỗi nhân viên chỉ có 1 tài khoản
            modelBuilder.Entity<NhanVien>()
                .HasIndex(n => n.MaNhanVien).IsUnique();
            modelBuilder.Entity<NhanVien>()
                .HasIndex(n => n.Email).IsUnique();
            modelBuilder.Entity<NhanVien>()
                .HasIndex(n => n.CanCuocCongDan).IsUnique();

            modelBuilder.Entity<PhongBan>()
                .HasOne(x => x.PhongBanCha)
                .WithMany(x => x.PhongBanCons)
                .HasForeignKey(x => x.PhongBanChaID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChamCong>()
                .HasIndex(c => new { c.NhanVienID, c.Ngay }).IsUnique();
            modelBuilder.Entity<Luong>()
                .HasIndex(l => new { l.NhanVienID, l.Thang, l.Nam }).IsUnique();
            modelBuilder.Entity<HopDong>()
                .HasIndex(h => h.SoHopDong).IsUnique();
            modelBuilder.Entity<LoaiHopDong>()
                .HasIndex(x => x.TenLoai).IsUnique();
            modelBuilder.Entity<LoaiNghiPhep>().HasIndex(x => x.MaLoai).IsUnique();
            modelBuilder.Entity<ChinhSachPhuCap>().HasIndex(x => new { x.PhamVi, x.HoatDong });
            modelBuilder.Entity<ChinhSachPhuCap>().HasOne(x => x.PhongBan).WithMany().HasForeignKey(x => x.PhongBanID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChinhSachPhuCap>().HasOne(x => x.ChucVu).WithMany().HasForeignKey(x => x.ChucVuID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChinhSachPhuCap>().HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChinhSachBhxh>().HasIndex(x => new { x.PhamVi, x.HoatDong });
            modelBuilder.Entity<ChinhSachBhxh>().HasOne(x => x.PhongBan).WithMany().HasForeignKey(x => x.PhongBanID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChinhSachBhxh>().HasOne(x => x.ChucVu).WithMany().HasForeignKey(x => x.ChucVuID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChinhSachBhxh>().HasOne(x => x.NhanVien).WithMany().HasForeignKey(x => x.NhanVienID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SoDuNghiPhep>()
                .HasIndex(x => new { x.NhanVienID, x.LoaiNghiPhepID, x.Nam }).IsUnique();
            modelBuilder.Entity<DieuChinhChamCong>()
                .HasIndex(x => new { x.NhanVienID, x.Ngay, x.TrangThai });
            modelBuilder.Entity<DieuChinhChamCongMinhChung>()
                .HasOne(x=>x.YeuCau).WithMany(x=>x.MinhChungs).HasForeignKey(x=>x.DieuChinhChamCongID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<DieuChinhChamCong>()
                .HasOne(x=>x.ChamCong).WithMany().HasForeignKey(x=>x.ChamCongID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<YeuCauCapNhatThongTinCaNhan>()
                .HasIndex(x=>new{x.NhanVienID,x.TrangThai});
            modelBuilder.Entity<ThongBaoCaNhan>().HasIndex(x=>new{x.NhanVienID,x.MaThamChieu}).IsUnique();
            modelBuilder.Entity<CaLamViec>().HasIndex(x=>x.MaCa).IsUnique();
            modelBuilder.Entity<PhanCaNhanVien>().HasIndex(x=>new{x.NhanVienID,x.TuNgay});
            modelBuilder.Entity<DangKyTangCa>().HasIndex(x=>new{x.NhanVienID,x.Ngay,x.TrangThai});
            modelBuilder.Entity<DangKyTangCaMinhChung>().HasOne(x=>x.DangKyTangCa).WithMany(x=>x.MinhChungs).HasForeignKey(x=>x.DangKyTangCaID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CongTac>().HasIndex(x=>new{x.NhanVienID,x.TuNgay,x.DenNgay});
            modelBuilder.Entity<PhanCongCongTac>().HasIndex(x=>new{x.CongTacID,x.NhanVienID}).IsUnique();
            modelBuilder.Entity<CongTac>()
                .HasOne(x=>x.NhanVien).WithMany(x=>x.CongTacs).HasForeignKey(x=>x.NhanVienID)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<PhanCongCongTac>()
                .HasOne(x=>x.CongTac).WithMany(x=>x.PhanCongs).HasForeignKey(x=>x.CongTacID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PhanCongCongTac>()
                .HasOne(x=>x.NhanVien).WithMany(x=>x.PhanCongCongTacs).HasForeignKey(x=>x.NhanVienID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<XacNhanTaiLieu>().HasIndex(x=>new{x.TaiLieuNoiBoID,x.TaiKhoanID}).IsUnique();
            modelBuilder.Entity<TepTaiLieuNoiBo>().HasOne(x=>x.TaiLieuNoiBo).WithMany(x=>x.TepDinhKems).HasForeignKey(x=>x.TaiLieuNoiBoID).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<TaiLieuNoiBo>().HasOne(x=>x.ChucVu).WithMany().HasForeignKey(x=>x.ChucVuID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ThongBaoNoiBo>().HasOne(x=>x.ChucVu).WithMany().HasForeignKey(x=>x.ChucVuID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PhieuKhaoSat>().HasIndex(x=>new{x.KhaoSatNoiBoID,x.TaiKhoanID}).IsUnique();
            modelBuilder.Entity<PhieuKhaoSat>()
                .HasOne(x => x.KhaoSat).WithMany().HasForeignKey(x => x.KhaoSatNoiBoID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<QuyTrinhPheDuyet>().HasIndex(x=>x.LoaiYeuCau).IsUnique();
            modelBuilder.Entity<BuocPheDuyet>().HasIndex(x=>new{x.QuyTrinhPheDuyetID,x.ThuTu}).IsUnique();
            modelBuilder.Entity<YeuCauNhanLuc>().HasIndex(x=>x.MaYeuCau).IsUnique();
            modelBuilder.Entity<YeuCauNhanLuc>()
                .HasOne(x=>x.NhanVienNoiBo).WithMany(x=>x.YeuCauDieuChuyens)
                .HasForeignKey(x=>x.NhanVienNoiBoID).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<DanhGiaHieuSuat>()
                .HasOne(x=>x.NhanVien).WithMany(x=>x.DanhGiaHieuSuats)
                .HasForeignKey(x=>x.NhanVienID).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<DanhGiaHieuSuat>().Ignore(x=>x.DiemTrungBinh);
            modelBuilder.Entity<HopDong>()
                .HasOne(x => x.HopDongTruoc).WithMany().HasForeignKey(x => x.HopDongTruocID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<TaiLieuHopDong>()
                .HasOne(x => x.HopDong).WithMany(x => x.TaiLieuHopDongs).HasForeignKey(x => x.HopDongID)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<NghiPhepMinhChung>()
                .HasOne(x => x.NghiPhep).WithMany(x => x.MinhChungs).HasForeignKey(x => x.NghiPhepID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TaiKhoan>()
                .HasOne(t => t.NhanVien)
                .WithOne(n => n.TaiKhoan)
                .HasForeignKey<TaiKhoan>(t => t.NhanVienID);

            modelBuilder.Entity<ChucVu>().Property(c => c.Hesoluong).HasPrecision(8, 2);
            modelBuilder.Entity<ChucVu>().Property(c => c.SoNgayPhepNam).HasPrecision(6, 2).HasDefaultValue(16m);
            modelBuilder.Entity<Luong>().Property(l => l.SoNgayCong).HasPrecision(8, 2);
            modelBuilder.Entity<Luong>().Property(l => l.HeSoLuong).HasPrecision(8, 2);
        }
    }
}
