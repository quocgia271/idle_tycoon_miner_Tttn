Dạ thưa thầy, 

Vì đây là một game Offline/Single-player chạy trên máy client (Unity), nên dự án của em không sử dụng một Hệ quản trị cơ sở dữ liệu (DBMS) truyền thống hay server riêng. Thay vào đó, em áp dụng kỹ thuật Data Persistence (Lưu trữ cục bộ) bằng JSON Serialization để lưu trữ tiến trình game.

Trong thư mục này, em đã đính kèm:
1. File idle_tycoon_save.json: Đây là ví dụ về cấu trúc dữ liệu đã được lưu (hoạt động như Data Schema).
2. File SaveData.cs: Định nghĩa các class mô hình dữ liệu (tương đương với các Table trong Database).
3. File SaveManager.cs: Chứa logic Đọc/Ghi dữ liệu ra file JSON.

Mong thầy xem qua ạ. Em cảm ơn thầy!
