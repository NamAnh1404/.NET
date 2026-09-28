# HRM Desktop

Đồ án quản lý nhân sự dành cho hai vai trò Admin và Nhân viên, xây dựng bằng C# WPF.

## Môi trường

- Visual Studio Community 2017 (nickname: 2)
- .NET Framework 4.6.1
- WPF
- SQL Server LocalDB
- Entity Framework 6.4.4 Database First
- Solution: `HRMDesktop.sln`

## Tài khoản dùng thử

- Admin: `admin` / `123`
- Nhân viên: `employee` / `123`

## Chức năng hiện có

### Admin

- Tổng quan nhân sự
- Quản lý và tìm kiếm nhân viên
- Quản lý lương, thưởng và trạng thái thanh toán
- Duyệt hoặc từ chối đơn nghỉ phép
- Theo dõi chấm công theo ngày, phòng ban và duyệt yêu cầu điều chỉnh
- Báo cáo thống kê

### Nhân viên

- Tổng quan cá nhân
- Chấm công bắt đầu và kết thúc ngày làm việc
- Xem lịch sử chấm công
- Gửi yêu cầu điều chỉnh khi thiếu hoặc sai giờ chấm công
- Gửi và theo dõi đơn nghỉ phép
- Xem phiếu lương
- Cập nhật hồ sơ và ngày sinh

## Chạy dự án

1. Mở `HRMDesktop.sln` bằng Visual Studio 2017.
2. Chọn cấu hình `Debug` và `Any CPU`.
3. Nhấn `F5` để chạy.

Ứng dụng kết nối database `HRMDatabase` trên instance `(LocalDB)\MSSQLLocalDB`. Schema nguồn nằm tại `HRMDesktop/Database/HRMDatabase.sql`; mô hình Database First, Entity và `HRMDatabaseEntities` nằm trong `HRMDesktop/Data/Generated`.

Lần chạy đầu, ứng dụng tự tạo database theo schema. Nếu máy đang có dữ liệu XML của phiên bản cũ, dữ liệu đó được nhập một lần vào SQL Server; từ thời điểm này mọi thao tác thêm, sửa, chấm công, duyệt đơn, trả lương và đổi mật khẩu đều được lưu bằng Entity Framework.
