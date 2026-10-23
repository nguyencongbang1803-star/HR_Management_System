# TIC HRM MVP

Hệ thống quản lý nhân sự nội bộ xây dựng bằng ASP.NET Core MVC, Entity Framework Core và SQL Server.

## Chức năng

- Đăng nhập và phân quyền ASP.NET Core Identity với các role `Admin`, `HR`, `QuanLy`,
  `NhanVien`; hỗ trợ đổi/reset mật khẩu, lockout và security stamp.
- Dashboard, hồ sơ nhân viên, phòng ban/chức vụ, hợp đồng và cảnh báo hết hạn.
- Chấm công, nghỉ phép có phê duyệt, cổng tự phục vụ cho nhân viên.
- Phụ cấp, thưởng/phạt, bảo hiểm, tính/khóa/thanh toán kỳ lương.
- Tài liệu nội bộ có xác nhận đã đọc, thông báo có bình luận và khảo sát nhanh.
- Phát hành nội dung theo toàn công ty, vai trò hoặc phòng ban.
- Trợ lý tra cứu quy chế từ nội dung nội bộ mà người dùng được phép xem.
- Cấu hình luồng phê duyệt theo loại yêu cầu và thứ tự vai trò duyệt.
- Báo cáo tổng hợp, xuất bảng lương CSV và nhật ký thao tác.

## Yêu cầu

- .NET SDK 9 hoặc mới hơn.
- SQL Server đang chạy trên máy hoặc máy chủ có thể truy cập.
- Tài khoản Windows chạy ứng dụng có quyền truy cập database khi dùng Integrated Security.

## Cấu hình cơ sở dữ liệu

Mặc định dự án kết nối SQL Server tại `localhost`, database `QLNS_DB2`, bằng Windows
Integrated Security. Có thể ghi đè chuỗi kết nối bằng biến môi trường:

```powershell
$env:ConnectionStrings__DefaultConnection = "Data Source=localhost;Initial Catalog=QLNS_DB2;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
```

Với SQL Server Authentication, sử dụng `User ID` và `Password` thay cho
`Integrated Security=True`. Không lưu mật khẩu production trực tiếp trong `appsettings.json`.

Trong môi trường Development, ứng dụng tự chạy migration khi khởi động. Có thể chạy thủ công bằng EF CLI:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

## Tài khoản đầu tiên

Nếu database chưa có tài khoản, cấu hình trước khi chạy:

```powershell
$env:HRM_ADMIN_EMAIL = "[EMAIL_ADDRESS]"
$env:HRM_ADMIN_PASSWORD = "admin"
dotnet run
```

Nếu không cấu hình hai biến trên trong môi trường Development, hệ thống tạo tài khoản demo:

- Email: `admin@tic.local`
- Mật khẩu: `Admin@123456`

Hãy đổi mật khẩu ngay sau lần đăng nhập đầu tiên. Ở môi trường ngoài Development, hệ thống không tự tạo mật khẩu mặc định.

### Phân quyền Identity

- `Admin`: toàn quyền quản trị, tạo tài khoản, đổi vai trò, reset mật khẩu và xác nhận thanh toán lương.
- `HR`: quản lý hồ sơ, công, phép, chế độ, bảng lương và nội dung nội bộ.
- `QuanLy`: xem dashboard, nhân viên, hợp đồng, công, phép và báo cáo; không quản trị tài khoản/lương.
- `NhanVien`: cổng tự phục vụ và thông tin nội bộ thuộc phạm vi được cấp.

Identity tạm khóa tài khoản 15 phút sau 5 lần đăng nhập sai. Mật khẩu phải có tối
thiểu 10 ký tự, chữ hoa, chữ thường, chữ số và ký tự đặc biệt. Khi Admin đổi role,
security stamp làm mới quyền của phiên cũ trong tối đa một phút.

## Quy trình vận hành đề xuất

1. Tạo phòng ban và chức vụ.
2. Tạo hồ sơ nhân viên và hợp đồng có hiệu lực.
3. Tạo tài khoản, gắn đúng nhân viên và vai trò.
4. Ghi nhận chấm công, xử lý đơn nghỉ và các khoản phụ cấp/thưởng/phạt/bảo hiểm.
5. Tính bảng lương nháp, đối soát, khóa kỳ; Admin đánh dấu đã thanh toán.

### Chấm công, OT và công tác

- Tạo ca làm việc và phân ca theo khoảng ngày tại **Ca, OT & công tác**.
- Nhân viên gửi đăng ký OT; HR xác nhận số giờ và hệ số. Mặc định hệ số là `1.5`
  cho ngày thường và `2.0` cho thứ Bảy/Chủ nhật. Ngày lễ cần được HR điều chỉnh
  theo chính sách doanh nghiệp.
- Công tác được duyệt tự động tạo công cho các ngày làm việc trong chuyến đi.
- File CSV chấm công tối đa 2 MB, mã hóa UTF-8, có tiêu đề:

```csv
MaNhanVien,Ngay,GioVao,GioRa,TrangThai,GhiChu
NV001,2026-07-21,08:00,17:00,Present,Nhập từ máy chấm công
```

Các trạng thái CSV hợp lệ: `Present`, `Remote`, `BusinessTrip`. Nếu bất kỳ dòng
nào lỗi hoặc thuộc kỳ lương đã khóa, toàn bộ file sẽ được hoàn tác.

Khi kỳ lương đã khóa, dữ liệu chấm công của kỳ đó không thể sửa/xóa. Muốn điều chỉnh cần xây dựng quy trình mở khóa có phê duyệt thay vì sửa trực tiếp dữ liệu đã chốt.

### Nghỉ phép

- Đơn nghỉ thông thường phải gửi trước ít nhất 3 ngày. Chọn **Nghỉ khẩn cấp** để
  có thể xin nghỉ từ ngày hiện tại.
- Lý do và file minh chứng (PDF, Word, JPG hoặc PNG, tối đa 10 MB) là bắt buộc.
- Nhân viên thường: Trưởng phòng của nhân viên duyệt trước, sau đó HR duyệt.
- Nhân viên HR và người giữ vai trò Quản lý: Trưởng phòng HR duyệt trực tiếp.
- Đơn của chính Trưởng phòng HR được tự động duyệt.
- Phép năm còn quỹ được tính có lương; số ngày vượt quỹ tự chuyển sang không lương.
- Quản lý chỉ xem hồ sơ và danh sách nhân viên thuộc phòng ban của mình.

### Cổng thông tin nội bộ

- Admin/HR phát hành tài liệu, thông báo và khảo sát tại **Cổng nội bộ**.
- Với phạm vi `Theo vai trò` hoặc `Theo phòng ban`, phải chọn đúng đối tượng nhận.
- Tệp tài liệu tối đa 10 MB; các định dạng được giao diện hỗ trợ gồm PDF, Word và ảnh.
- Nhân viên chỉ tải, xác nhận và bình luận trên nội dung thuộc phạm vi được phép xem.
- Trợ lý quy chế hiện dùng tìm kiếm từ khóa trên tài liệu/thông báo nội bộ, không gửi dữ liệu
  ra dịch vụ AI bên ngoài. Đây là lựa chọn phù hợp cho MVP và bảo vệ dữ liệu nhân sự.

### Luồng phê duyệt

Admin/HR có thể cấu hình các bước tại **Luồng phê duyệt** cho từng loại yêu cầu, với
các vai trò hỗ trợ: `QuanLy`, `HR`, `Admin`. Ở giai đoạn 4, màn hình này quản lý cấu
hình luồng; các nghiệp vụ nghỉ phép, OT và công tác vẫn sử dụng bộ xử lý phê duyệt
hiện có. Việc chuyển toàn bộ nghiệp vụ sang state machine động nên thực hiện ở giai
đoạn tiếp theo để có lịch sử từng bước, ủy quyền duyệt và SLA.

Quản trị viên (Admin)
Email: admin@tic.local
Mật khẩu: Admin@123456

Quản lý (QuanLy)
Email: quanly@tic.local
Mật khẩu: Quanly@123456

Nhân viên (NhanVien)
Email: nhanvien@tic.local
Mật khẩu: Nhanvien@123456
