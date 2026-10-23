using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QLNS.Migrations
{
    /// <inheritdoc />
    public partial class SqlServerInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CaLamViec",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaCa = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenCa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    SoPhutNghi = table.Column<int>(type: "int", nullable: false),
                    ChoPhepDiMuonPhut = table.Column<int>(type: "int", nullable: false),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaLamViec", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "ChucVu",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Chucvu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Hesoluong = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChucVu", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "KhaoSatNoiBo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CauHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhaoSatNoiBo", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LoaiBaoHiem",
                columns: table => new
                {
                    MaLoaiBaoHiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenLoaiBaoHiem = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiBaoHiem", x => x.MaLoaiBaoHiem);
                });

            migrationBuilder.CreateTable(
                name: "LoaiNghiPhep",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaLoai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TenLoai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HuongLuong = table.Column<bool>(type: "bit", nullable: false),
                    TruQuyPhep = table.Column<bool>(type: "bit", nullable: false),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiNghiPhep", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHeThong",
                columns: table => new
                {
                    ID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HanhDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoiTuong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoiTuongID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChiTiet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHeThong", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PhongBan",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tenphongban = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Matruongphong = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhongBan", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "QuyTrinhPheDuyet",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoaiYeuCau = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenQuyTrinh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyTrinhPheDuyet", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LuaChonKhaoSat",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhaoSatNoiBoID = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LuaChonKhaoSat", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LuaChonKhaoSat_KhaoSatNoiBo_KhaoSatNoiBoID",
                        column: x => x.KhaoSatNoiBoID,
                        principalTable: "KhaoSatNoiBo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Matkhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SDT = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    SoTaiKhoan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Gioitinh = table.Column<bool>(type: "bit", nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanCuocCongDan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    AnhDaiDien = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MaSothue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Trangthai = table.Column<bool>(type: "bit", nullable: false),
                    Ngaytao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngayvaolam = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngayxoa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ngaycapnhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PhongBanID = table.Column<int>(type: "int", nullable: true),
                    ChucVuID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhanVien", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NhanVien_ChucVu_ChucVuID",
                        column: x => x.ChucVuID,
                        principalTable: "ChucVu",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_NhanVien_PhongBan_PhongBanID",
                        column: x => x.PhongBanID,
                        principalTable: "PhongBan",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TaiLieuNoiBo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DanhMuc = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PhamVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhongBanID = table.Column<int>(type: "int", nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BatBuocXacNhan = table.Column<bool>(type: "bit", nullable: false),
                    DaPhatHanh = table.Column<bool>(type: "bit", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    DuongDan = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayPhatHanh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuNoiBo", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TaiLieuNoiBo_PhongBan_PhongBanID",
                        column: x => x.PhongBanID,
                        principalTable: "PhongBan",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "ThongBaoNoiBo",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhongBanID = table.Column<int>(type: "int", nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Ghim = table.Column<bool>(type: "bit", nullable: false),
                    BatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiTao = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaoNoiBo", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ThongBaoNoiBo_PhongBan_PhongBanID",
                        column: x => x.PhongBanID,
                        principalTable: "PhongBan",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "BuocPheDuyet",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuyTrinhPheDuyetID = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    VaiTroDuyet = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenBuoc = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocPheDuyet", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BuocPheDuyet_QuyTrinhPheDuyet_QuyTrinhPheDuyetID",
                        column: x => x.QuyTrinhPheDuyetID,
                        principalTable: "QuyTrinhPheDuyet",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaoHiem",
                columns: table => new
                {
                    MaBaoHiem = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoTienDong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    LoaiBaoHiemID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoHiem", x => x.MaBaoHiem);
                    table.ForeignKey(
                        name: "FK_BaoHiem_LoaiBaoHiem_LoaiBaoHiemID",
                        column: x => x.LoaiBaoHiemID,
                        principalTable: "LoaiBaoHiem",
                        principalColumn: "MaLoaiBaoHiem",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaoHiem_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChamCong",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Giovaolam = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Giotanlam = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PhutDiMuon = table.Column<int>(type: "int", nullable: false),
                    PhutVeSom = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChamCong", x => x.ID);
                    table.ForeignKey(
                        name: "FK_ChamCong_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CongTac",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiaDiem = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MucDich = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TamUng = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CongTac", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CongTac_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DangKyTangCa",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioBatDau = table.Column<TimeSpan>(type: "time", nullable: false),
                    GioKetThuc = table.Column<TimeSpan>(type: "time", nullable: false),
                    HeSo = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    SoGioDuyet = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DangKyTangCa", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DangKyTangCa_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DieuChinhChamCong",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioVaoDeXuat = table.Column<TimeSpan>(type: "time", nullable: true),
                    GioRaDeXuat = table.Column<TimeSpan>(type: "time", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuChinhChamCong", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DieuChinhChamCong_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HopDong",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    SoHopDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Loaihopdong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ngayky = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngaybatdau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ngayketthuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Luongcoban = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Trangthai = table.Column<bool>(type: "bit", nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    DuongDanFile = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HopDongTruocID = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDong", x => x.ID);
                    table.ForeignKey(
                        name: "FK_HopDong_HopDong_HopDongTruocID",
                        column: x => x.HopDongTruocID,
                        principalTable: "HopDong",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HopDong_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Luong",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    Luongcoban = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoNgayCong = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    SoNgayChuan = table.Column<int>(type: "int", nullable: false),
                    Phucap = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Thuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SoGioTangCa = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    TienTangCa = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Khautru = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BaoHiem = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Tongluong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NgayTinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKhoa = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayDoiSoat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDoiSoat = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NguoiKhoa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Luong", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Luong_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NghiPhep",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    LoaiNghiPhepID = table.Column<int>(type: "int", nullable: true),
                    LoaiNghi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ngaybatdau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Lydo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Trangthai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PhanHoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NghiPhep", x => x.ID);
                    table.ForeignKey(
                        name: "FK_NghiPhep_LoaiNghiPhep_LoaiNghiPhepID",
                        column: x => x.LoaiNghiPhepID,
                        principalTable: "LoaiNghiPhep",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_NghiPhep_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhanCaNhanVien",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    CaLamViecID = table.Column<int>(type: "int", nullable: false),
                    TuNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhanCaNhanVien", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PhanCaNhanVien_CaLamViec_CaLamViecID",
                        column: x => x.CaLamViecID,
                        principalTable: "CaLamViec",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhanCaNhanVien_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhuCap",
                columns: table => new
                {
                    MaPhuCap = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    TenPhuCap = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NgayApDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuCap", x => x.MaPhuCap);
                    table.ForeignKey(
                        name: "FK_PhuCap_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuaTrinhCongTac",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ChucDanh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TuNgay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DenNgay = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuaTrinhCongTac", x => x.ID);
                    table.ForeignKey(
                        name: "FK_QuaTrinhCongTac_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SoDuNghiPhep",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    LoaiNghiPhepID = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    SoNgayDuocHuong = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    DieuChinh = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDuNghiPhep", x => x.ID);
                    table.ForeignKey(
                        name: "FK_SoDuNghiPhep_LoaiNghiPhep_LoaiNghiPhepID",
                        column: x => x.LoaiNghiPhepID,
                        principalTable: "LoaiNghiPhep",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SoDuNghiPhep_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NhanVienID = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    Pass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                    table.ForeignKey(
                        name: "FK_TaiKhoan_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateTable(
                name: "TaiLieuNhanVien",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    TenTaiLieu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    DuongDan = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NgayTaiLen = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTaiLen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuNhanVien", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TaiLieuNhanVien_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ThuongPhat",
                columns: table => new
                {
                    MaThuongPhat = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Loai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NgayApDung = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NhanVienID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuongPhat", x => x.MaThuongPhat);
                    table.ForeignKey(
                        name: "FK_ThuongPhat_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrinhDoChungChi",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhanVienID = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DonViCap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoHieu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrinhDoChungChi", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TrinhDoChungChi_NhanVien_NhanVienID",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_TaiKhoan_UserId",
                        column: x => x.UserId,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BinhLuanThongBao",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThongBaoNoiBoID = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanID = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinhLuanThongBao", x => x.ID);
                    table.ForeignKey(
                        name: "FK_BinhLuanThongBao_TaiKhoan_TaiKhoanID",
                        column: x => x.TaiKhoanID,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BinhLuanThongBao_ThongBaoNoiBo_ThongBaoNoiBoID",
                        column: x => x.ThongBaoNoiBoID,
                        principalTable: "ThongBaoNoiBo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuKhaoSat",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhaoSatNoiBoID = table.Column<int>(type: "int", nullable: false),
                    LuaChonKhaoSatID = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanID = table.Column<int>(type: "int", nullable: false),
                    NgayTraLoi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKhaoSat", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PhieuKhaoSat_KhaoSatNoiBo_KhaoSatNoiBoID",
                        column: x => x.KhaoSatNoiBoID,
                        principalTable: "KhaoSatNoiBo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhaoSat_LuaChonKhaoSat_LuaChonKhaoSatID",
                        column: x => x.LuaChonKhaoSatID,
                        principalTable: "LuaChonKhaoSat",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuKhaoSat_TaiKhoan_TaiKhoanID",
                        column: x => x.TaiKhoanID,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XacNhanTaiLieu",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiLieuNoiBoID = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanID = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XacNhanTaiLieu", x => x.ID);
                    table.ForeignKey(
                        name: "FK_XacNhanTaiLieu_TaiKhoan_TaiKhoanID",
                        column: x => x.TaiKhoanID,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_XacNhanTaiLieu_TaiLieuNoiBo_TaiLieuNoiBoID",
                        column: x => x.TaiLieuNoiBoID,
                        principalTable: "TaiLieuNoiBo",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoHiem_LoaiBaoHiemID",
                table: "BaoHiem",
                column: "LoaiBaoHiemID");

            migrationBuilder.CreateIndex(
                name: "IX_BaoHiem_NhanVienID",
                table: "BaoHiem",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_BinhLuanThongBao_TaiKhoanID",
                table: "BinhLuanThongBao",
                column: "TaiKhoanID");

            migrationBuilder.CreateIndex(
                name: "IX_BinhLuanThongBao_ThongBaoNoiBoID",
                table: "BinhLuanThongBao",
                column: "ThongBaoNoiBoID");

            migrationBuilder.CreateIndex(
                name: "IX_BuocPheDuyet_QuyTrinhPheDuyetID_ThuTu",
                table: "BuocPheDuyet",
                columns: new[] { "QuyTrinhPheDuyetID", "ThuTu" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CaLamViec_MaCa",
                table: "CaLamViec",
                column: "MaCa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChamCong_NhanVienID_Ngay",
                table: "ChamCong",
                columns: new[] { "NhanVienID", "Ngay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CongTac_NhanVienID_TuNgay_DenNgay",
                table: "CongTac",
                columns: new[] { "NhanVienID", "TuNgay", "DenNgay" });

            migrationBuilder.CreateIndex(
                name: "IX_DangKyTangCa_NhanVienID_Ngay_TrangThai",
                table: "DangKyTangCa",
                columns: new[] { "NhanVienID", "Ngay", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhChamCong_NhanVienID_Ngay_TrangThai",
                table: "DieuChinhChamCong",
                columns: new[] { "NhanVienID", "Ngay", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_HopDongTruocID",
                table: "HopDong",
                column: "HopDongTruocID");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_NhanVienID",
                table: "HopDong",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_HopDong_SoHopDong",
                table: "HopDong",
                column: "SoHopDong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiNghiPhep_MaLoai",
                table: "LoaiNghiPhep",
                column: "MaLoai",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LuaChonKhaoSat_KhaoSatNoiBoID",
                table: "LuaChonKhaoSat",
                column: "KhaoSatNoiBoID");

            migrationBuilder.CreateIndex(
                name: "IX_Luong_NhanVienID_Thang_Nam",
                table: "Luong",
                columns: new[] { "NhanVienID", "Thang", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NghiPhep_LoaiNghiPhepID",
                table: "NghiPhep",
                column: "LoaiNghiPhepID");

            migrationBuilder.CreateIndex(
                name: "IX_NghiPhep_NhanVienID",
                table: "NghiPhep",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_CanCuocCongDan",
                table: "NhanVien",
                column: "CanCuocCongDan",
                unique: true,
                filter: "[CanCuocCongDan] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_ChucVuID",
                table: "NhanVien",
                column: "ChucVuID");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_Email",
                table: "NhanVien",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_MaNhanVien",
                table: "NhanVien",
                column: "MaNhanVien",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhanVien_PhongBanID",
                table: "NhanVien",
                column: "PhongBanID");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCaNhanVien_CaLamViecID",
                table: "PhanCaNhanVien",
                column: "CaLamViecID");

            migrationBuilder.CreateIndex(
                name: "IX_PhanCaNhanVien_NhanVienID_TuNgay",
                table: "PhanCaNhanVien",
                columns: new[] { "NhanVienID", "TuNgay" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhaoSat_KhaoSatNoiBoID_TaiKhoanID",
                table: "PhieuKhaoSat",
                columns: new[] { "KhaoSatNoiBoID", "TaiKhoanID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhaoSat_LuaChonKhaoSatID",
                table: "PhieuKhaoSat",
                column: "LuaChonKhaoSatID");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhaoSat_TaiKhoanID",
                table: "PhieuKhaoSat",
                column: "TaiKhoanID");

            migrationBuilder.CreateIndex(
                name: "IX_PhuCap_NhanVienID",
                table: "PhuCap",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_QuaTrinhCongTac_NhanVienID",
                table: "QuaTrinhCongTac",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_QuyTrinhPheDuyet_LoaiYeuCau",
                table: "QuyTrinhPheDuyet",
                column: "LoaiYeuCau",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoDuNghiPhep_LoaiNghiPhepID",
                table: "SoDuNghiPhep",
                column: "LoaiNghiPhepID");

            migrationBuilder.CreateIndex(
                name: "IX_SoDuNghiPhep_NhanVienID_LoaiNghiPhepID_Nam",
                table: "SoDuNghiPhep",
                columns: new[] { "NhanVienID", "LoaiNghiPhepID", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "TaiKhoan",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_NhanVienID",
                table: "TaiKhoan",
                column: "NhanVienID",
                unique: true,
                filter: "[NhanVienID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "TaiKhoan",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuNhanVien_NhanVienID",
                table: "TaiLieuNhanVien",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuNoiBo_PhongBanID",
                table: "TaiLieuNoiBo",
                column: "PhongBanID");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoNoiBo_PhongBanID",
                table: "ThongBaoNoiBo",
                column: "PhongBanID");

            migrationBuilder.CreateIndex(
                name: "IX_ThuongPhat_NhanVienID",
                table: "ThuongPhat",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_TrinhDoChungChi_NhanVienID",
                table: "TrinhDoChungChi",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_XacNhanTaiLieu_TaiKhoanID",
                table: "XacNhanTaiLieu",
                column: "TaiKhoanID");

            migrationBuilder.CreateIndex(
                name: "IX_XacNhanTaiLieu_TaiLieuNoiBoID_TaiKhoanID",
                table: "XacNhanTaiLieu",
                columns: new[] { "TaiLieuNoiBoID", "TaiKhoanID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BaoHiem");

            migrationBuilder.DropTable(
                name: "BinhLuanThongBao");

            migrationBuilder.DropTable(
                name: "BuocPheDuyet");

            migrationBuilder.DropTable(
                name: "ChamCong");

            migrationBuilder.DropTable(
                name: "CongTac");

            migrationBuilder.DropTable(
                name: "DangKyTangCa");

            migrationBuilder.DropTable(
                name: "DieuChinhChamCong");

            migrationBuilder.DropTable(
                name: "HopDong");

            migrationBuilder.DropTable(
                name: "Luong");

            migrationBuilder.DropTable(
                name: "NghiPhep");

            migrationBuilder.DropTable(
                name: "NhatKyHeThong");

            migrationBuilder.DropTable(
                name: "PhanCaNhanVien");

            migrationBuilder.DropTable(
                name: "PhieuKhaoSat");

            migrationBuilder.DropTable(
                name: "PhuCap");

            migrationBuilder.DropTable(
                name: "QuaTrinhCongTac");

            migrationBuilder.DropTable(
                name: "SoDuNghiPhep");

            migrationBuilder.DropTable(
                name: "TaiLieuNhanVien");

            migrationBuilder.DropTable(
                name: "ThuongPhat");

            migrationBuilder.DropTable(
                name: "TrinhDoChungChi");

            migrationBuilder.DropTable(
                name: "XacNhanTaiLieu");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "LoaiBaoHiem");

            migrationBuilder.DropTable(
                name: "ThongBaoNoiBo");

            migrationBuilder.DropTable(
                name: "QuyTrinhPheDuyet");

            migrationBuilder.DropTable(
                name: "CaLamViec");

            migrationBuilder.DropTable(
                name: "LuaChonKhaoSat");

            migrationBuilder.DropTable(
                name: "LoaiNghiPhep");

            migrationBuilder.DropTable(
                name: "TaiKhoan");

            migrationBuilder.DropTable(
                name: "TaiLieuNoiBo");

            migrationBuilder.DropTable(
                name: "KhaoSatNoiBo");

            migrationBuilder.DropTable(
                name: "NhanVien");

            migrationBuilder.DropTable(
                name: "ChucVu");

            migrationBuilder.DropTable(
                name: "PhongBan");
        }
    }
}
