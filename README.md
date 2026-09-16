# HRM Desktop

Đồ án quản lý nhân sự dành cho hai vai trò Admin và Nhân viên, xây dựng bằng C# WPF.

## Môi trường

- Visual Studio Community 2017 (nickname: 2)
- .NET Framework 4.6.1
- WPF
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

Phiên bản hiện tại dùng `MockDataService` để hoàn thiện và kiểm thử luồng giao diện. Bước tiếp theo là thay service này bằng lớp gọi ASP.NET Core Web API và cơ sở dữ liệu MySQL/Railway.
