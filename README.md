# PEDINHIOS FPS BOOST PRO

Ứng dụng WPF cho Windows x64, hướng tới BlueStacks.

## Build file EXE bằng GitHub Actions
1. Upload toàn bộ thư mục dự án lên repository GitHub.
2. Mở tab Actions và chạy workflow **Build PEDINHIOS FPS BOOST PRO**.
3. Khi chạy thành công, tải artifact `PEDINHIOS-FPS-BOOST-PRO-Windows-x64`.
4. Giải nén và chạy `PEDINHIOS.FPSBoost.exe`.

## Chức năng
- Đọc mức sử dụng CPU và RAM hệ thống.
- Nhận diện tiến trình HD-Player.exe.
- Đặt process priority thành AboveNormal hoặc khôi phục Normal.
- Hướng dẫn cấu hình FPS/CPU/RAM trong BlueStacks.

## Giới hạn
Ứng dụng không thể đảm bảo tăng FPS hoặc tự mở khóa FPS. FPS, CPU và RAM cần được cấu hình trong BlueStacks vì cơ chế lưu cấu hình thay đổi theo phiên bản. Thay đổi priority chỉ tác động đến lịch CPU của Windows và không cải thiện FPS trên mọi máy.

Yêu cầu build: .NET 8 SDK trên Windows. File publish self-contained không yêu cầu cài .NET runtime.
