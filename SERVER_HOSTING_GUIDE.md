# 🌐 Hướng Dẫn Thiết Lập & Vận Hành Server Cho Japanese Mastery

> **Tài liệu hướng dẫn & phân tích kiến trúc kết nối Server**  
> Dành cho hai dự án: **`JapaneseMasteryBackendDotNet`** (Backend API) và **`JapaneseMastery`** (Godot Game Client).  
> Tích hợp giải pháp đường hầm Internet miễn phí: **`PyTunnel Studio`**.

---

## 📑 Mục Lục
1. [Tổng Quan Kiến Trúc Hệ Thống](#1-tổng-quan-kiến-trúc-hệ-thống)
2. [Phân Tích Chi Tiết Công Cụ `PyTunnel Studio`](#2-phân-tích-chi-tiết-công-cụ-pytunnel-studio)
3. [Hướng Dẫn Từng Bước: Chạy Server Bằng PyTunnel (Khuyên Dùng)](#3-hướng-dẫn-từng-bước-chạy-server-bằng-pytunnel-khuyên-dùng)
4. [Lưu Ý Sống Còn Cho Game Client (Godot)](#4-lưu-ý-sống-còn-cho-game-client-godot)
5. [Phương Án Thay Thế: Thuê Cloud VPS Chạy 24/7](#5-phương-án-thay-thế-thuê-cloud-vps-chạy-247)
6. [Bảng So Sánh Các Phương Án](#6-bảng-so-sánh-các-phương-án)
7. [Gợi Ý Cải Tiến Tương Lai Cho Client](#7-gợi-ý-cải-tiến-tương-lai-cho-client)

---

## 1. Tổng Quan Kiến Trúc Hệ Thống

Dự án game của bạn hoạt động theo mô hình Client - Server tiêu chuẩn:

```
┌───────────────────────────────┐
│     Godot Game Client         │
│     (JapaneseMastery)         │
│  Người chơi / Bạn bè ở xa     │
└──────────────┬────────────────┘
               │ Gửi HTTP REST (JSON) / WebSocket
               ▼
┌───────────────────────────────┐
│      Cloudflare Network       │
│  (Đường hầm bảo vệ qua mạng)  │
└──────────────┬────────────────┘
               │ Tunnel an toàn (Không cần mở Port Modem)
               ▼
┌───────────────────────────────┐
│     PyTunnel Studio           │
│   (Chạy trên máy tính bạn)    │
│    Reverse Proxy Shield       │
└──────────────┬────────────────┘
               │ Localhost:8080
               ▼
┌───────────────────────────────┐
│   JapaneseMasteryBackend      │
│         (.NET 8 API)          │
│   Cổng: 8080 | Auth, PvP, Shop│
└──────────────┬────────────────┘
               │ Localhost:1433
               ▼
┌───────────────────────────────┐
│      Microsoft SQL Server     │
│   Cơ sở dữ liệu người dùng    │
└───────────────────────────────┘
```

* **Godot Client (`JapaneseMastery`)**: Gọi API thông qua `Network.BASE_URL` (ví dụ: `https://xxxx.trycloudflare.com/api`).
* **Backend (`JapaneseMasteryBackendDotNet`)**: ASP.NET Core 8 Web API, lắng nghe nội bộ tại `http://0.0.0.0:8080`.
* **Database**: Microsoft SQL Server chạy trên cổng `1433`.
* **PyTunnel Studio**: Cầu nối từ cổng nội bộ `8080` trên máy bạn ra Internet với tên miền HTTPS công khai.

---

## 2. Phân Tích Chi Tiết Công Cụ `PyTunnel Studio`

Thư mục `d:\github\pytunnel` mà bạn của bạn gửi là một công cụ quản lý đường hầm (**Cloudflare Tunnel Hub**) được xây dựng bằng Python (FastAPI + Three.js Neon HUD).

### Các đặc điểm nổi bật của PyTunnel:
1. **Zero-Cost & Zero-Config (100% Miễn phí & Không cần cấu hình phức tạp)**:
   - Tự động tải và xác thực nhị phân chính thức `cloudflared.exe` từ GitHub.
   - Không cần mua tên miền, không cần mua VPS, không cần tài khoản Cloudflare trả phí.
   - **Không cần mở port Modem (Port Forwarding / NAT)**: Tránh rủi ro bị lộ IP mạng gia đình.
2. **Giao diện Dashboard trực quan**:
   - Khởi chạy nhanh bằng file `start.bat`.
   - Bảng điều khiển tại `http://127.0.0.1:8787` giúp bạn theo dõi: URL công khai, số lượng request, lưu lượng mạng, trạng thái online.
3. **Bảo mật và tự dọn dẹp (Win32 Job Object)**:
   - Tự động diệt các tiến trình ngầm `cloudflared.exe` khi bạn tắt ứng dụng (không lo bị đơ máy hay tốn tài nguyên rác).
   - Danh sách chặn cổng nhạy cảm (Blacklist): Cổng `1433` (SQL Server) được bảo vệ không cho mở bừa bãi ra Internet, chỉ cho phép cổng ứng dụng như `8080`.

---

## 3. Hướng Dẫn Từng Bước: Chạy Server Bằng PyTunnel (Khuyên Dùng)

Đây là cách nhanh nhất và hoàn toàn miễn phí để bạn bè ở nơi khác có thể kết nối vào game của bạn:

### 🟢 Bước 1: Khởi động Cơ sở dữ liệu (SQL Server)
* Đảm bảo dịch vụ SQL Server (MSSQLSERVER hoặc SQL Express) đang chạy trên máy tính của bạn.
* Cổng mặc định: `1433`.

### 🟢 Bước 2: Khởi động Backend .NET 8
1. Mở PowerShell hoặc Command Prompt tại thư mục:  
   `d:\github\JapaneseMastery\JapaneseMasteryBackendDotNet`
2. Chạy lệnh:
   ```powershell
   dotnet run
   ```
3. Đảm bảo màn hình hiển thị:
   ```
   Now listening on: http://[::]:8080
   Application started. Press Ctrl+C to shut down.
   ```
4. Kiểm tra nhanh bằng trình duyệt: Truy cập `http://localhost:8080/swagger` để xem tài liệu API.

### 🟢 Bước 3: Mở đường hầm với PyTunnel
1. Vào thư mục `d:\github\pytunnel`.
2. Nhấp đúp chuột vào file **`start.bat`**.
3. Cửa sổ CMD sẽ tự động kiểm tra thư viện và mở trình duyệt tại: `http://127.0.0.1:8787`.
4. Trên giao diện web của PyTunnel:
   - **Nhập cổng (Port)**: Điền `8080`.
   - **Chế độ (Mode)**: Chọn **CÔNG KHAI (Public)**.  
     *(⚠️ Lưu ý: Tuyệt đối không chọn Bảo vệ/Protected, xem giải thích ở Mục 4)*.
   - Nhấn nút **"Bắt đầu đường hầm" (Start Tunnel)**.
5. Sau vài giây, hệ thống sẽ cấp cho bạn một đường link HTTPS công khai dạng:
   ```
   https://random-words-here.trycloudflare.com
   ```
   *(Sao chép đường link này lại).*

### 🟢 Bước 4: Cấu hình Game Client Godot
1. Mở project Godot `JapaneseMastery`.
2. Mở file [`scripts/autoloads/Network.gd`](file:///d:/github/JapaneseMastery/JapaneseMastery/scripts/autoloads/Network.gd).
3. Đổi hằng số `BASE_URL` trỏ về link Tunnel vừa tạo:
   ```gdscript
   # Cũ (khi chơi trên máy cá nhân):
   # const BASE_URL = "http://localhost:8080/api"

   # Mới (khi chơi qua mạng Internet):
   const BASE_URL = "https://random-words-here.trycloudflare.com/api"
   ```
4. Bây giờ bạn có thể Export bản build game Godot (`.exe` hoặc `.pck`) gửi cho bạn bè, hoặc cả hai cùng mở game để test:
   - Đăng ký / Đăng nhập tài khoản.
   - Học Solo Learning (đồng bộ cấp độ).
   - Mua đồ trong Shop (ảnh video từ Cloudinary).
   - Thách đấu PvP trực tuyến qua Internet!

---

## 4. Lưu Ý Sống Còn Cho Game Client (Godot)

> [!CAUTION]
> **BẮT BUỘC CHỌN CHẾ ĐỘ "PUBLIC MODE" (CÔNG KHAI) TRÊN PYTUNNEL!**

* **Tại sao không được chọn Protected Mode?**
  - Trong PyTunnel, chế độ **Protected** được thiết kế cho người duyệt web: khi ai đó truy cập, PyTunnel sẽ chặn lại và hiện trang web khóa màu đen (Cyber Gate) yêu cầu nhập mã Access Code 8 chữ số.
  - Tuy nhiên, Game Godot gửi các gói tin HTTP REST dạng thô (`application/json`). Godot không phải trình duyệt nên không thể hiển thị trang web nhập mã PIN.
  - Nếu chọn **Protected**, game sẽ nhận về chuỗi HTML của trang khóa thay vì dữ liệu JSON, dẫn đến lỗi: `JSON parse error` hoặc `Failed to fetch from server`.
* **Khi chọn Public Mode**: Mọi request từ game Godot sẽ đi thẳng vào Backend .NET 8 mà không bị chặn, bảo đảm kết nối mượt mà và không có độ trễ.
* **Bảo mật**: Bạn hoàn toàn yên tâm vì Backend .NET 8 của chúng ta đã có sẵn cơ chế bảo mật bằng **JWT Token** (JSON Web Token), mã hóa mật khẩu và phân quyền role (Player, Sensei, Designer, Admin).

---

## 5. Phương Án Thay Thế: Thuê Cloud VPS Chạy 24/7

Nếu bạn muốn server luôn luôn hoạt động mà **không cần bật máy tính ở nhà**, bạn có thể đưa toàn bộ hệ thống lên Cloud VPS:

### Các bước triển khai lên VPS (Linux Ubuntu 22.04):
1. **Thuê VPS**:
   - Cấu hình đề xuất: 2 CPU Core, 4GB RAM (giá dao động 100k - 150k/tháng ở các nhà cung cấp như Vietnix, TinoHost, Hetzner, DigitalOcean).
2. **Cài đặt môi trường trên VPS**:
   - Cài đặt **.NET 8 Runtime**:
     ```bash
     sudo apt-get update
     sudo apt-get install -y dotnet-sdk-8.0
     ```
   - Chạy **SQL Server 2022** qua Docker:
     ```bash
     sudo docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourPassword123!" \
        -p 1433:1433 --name sqlserver --restart always -d \
        mcr.microsoft.com/mssql/server:2022-latest
     ```
3. **Chạy Backend dưới dạng Background Service (Systemd)**:
   - Dùng `systemctl` để service tự động khởi động lại nếu máy chủ bị restart.
4. **Cài đặt Nginx hoặc Cloudflare Tunnel chính thức**:
   - Tạo tên miền riêng (ví dụ: `api.japanesemastery.com`).
   - Cấp chứng chỉ SSL HTTPS miễn phí bằng Certbot Let's Encrypt.

---

## 6. Bảng So Sánh Các Phương Án

| Tiêu Chí | Phương Án 1: Dùng PyTunnel (Khuyên Dùng Hiện Tại) | Phương Án 2: Thuê Cloud VPS (24/7) | Phương Án 3: Mở Port Modem (Port Forwarding) |
| :--- | :--- | :--- | :--- |
| **Chi phí** | **0đ (Hoàn toàn miễn phí)** | Tốn phí thuê hàng tháng (~100-200k/tháng) | 0đ |
| **Độ khó cài đặt** | **Rất dễ (1 click chạy `start.bat`)** | Trung bình (Cần biết cơ bản về Linux/Docker) | Khó (Cần vào cài đặt modem router gia đình) |
| **Bảo mật IP** | **Rất an toàn** (Ẩn IP nhà thật qua Cloudflare) | An toàn (Dùng IP của nhà mạng VPS) | **Rủi ro cao** (Lộ địa chỉ IP nhà thật) |
| **Thời gian hoạt động** | Phụ thuộc vào máy tính của bạn (Tắt máy = Tắt server) | **24/7 liên tục không ngừng nghỉ** | Phụ thuộc vào máy tính của bạn |
| **Phù hợp với** | **Test game, chơi chung với nhóm bạn, demo đồ án** | **Phát hành game chính thức ra công chúng** | Không khuyến khích |

---

## 7. Gợi Ý Cải Tiến Tương Lai Cho Client (Godot)

Hiện tại, `BASE_URL` trong Godot được gán tĩnh trong mã nguồn. Mỗi khi bạn đổi link Tunnel mới, bạn sẽ phải chỉnh sửa code.

**Ý tưởng nâng cấp thông minh:**
* Thêm một ô nhập **"Địa chỉ Server"** nhỏ ở góc màn hình Login của Godot (mặc định lưu vào file `user://server_config.json`).
* Khi bạn gửi link tunnel mới cho bạn bè, bạn bè chỉ cần dán link vào ô đó một lần mà không cần bạn phải xuất file build game mới!
*(Tính năng này có thể được triển khai khi bạn yêu cầu ở các bước tiếp theo).*

