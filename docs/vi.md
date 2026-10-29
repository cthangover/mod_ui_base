# UI — giao diện chung của mọi game

UI là lớp giao diện mà mọi game dùng chung: thanh công cụ ở góc trên bên phải, menu trong game, biểu tượng lưu, thông báo ở góc trên bên trái, khung xem nền và diện mạo thống nhất của bảng điều khiển, nút bấm, danh sách. UI không có cốt truyện hay luật chơi riêng. Gần như mọi mod khác đều được xây trên nó, vì thế nó hiện diện trong mọi game và rất nhiều mod liệt kê nó làm phụ thuộc.

![Một cảnh trong Cthangover lúc buổi trưa: đồng hồ trên bên trái, thanh công cụ trên bên phải, các lựa chọn của cảnh ở bên phải.](assets/scene-hud.jpg)

## Thanh công cụ

Mỗi biểu tượng mở một bảng điều khiển. Các nút không cố định: mod nào trong game cũng thêm nút của riêng mình, nên một tiểu thuyết tương tác không có túi đồ, còn game có Scenario Studio thì nhiều thêm một biểu tượng.

![Thanh công cụ của Cthangover: công cụ, bản đồ, túi đồ, nhân vật, người được chiêu mộ, kỹ năng, menu (bánh răng) và nhật ký.](assets/toolbar.png)

Trong Cthangover, từ trái sang phải:

1. **Công cụ** — menu công cụ dành cho nhà phát triển (chỉ khi game có trình soạn thảo).
2. **Bản đồ** — bản đồ thế giới (Map Engine).
3. **Túi đồ** — vật phẩm mang theo (Items).
4. **Nhân vật** — đội và các nhân vật (Characters).
5. **Người được chiêu mộ** — các nhân vật đã chiêu mộ (Recruiting).
6. **Kỹ năng** — kỹ năng (Skills).
7. **Menu** — menu trong game, nói ở dưới (mod này).
8. **Nhật ký** — nhiệm vụ (Quests).

Các game khác hiển thị bộ nút khác; chẳng hạn RPG Plus thêm **bảng nhân vật**.

## Menu trong game

Biểu tượng bánh răng mở menu: **Tiếp tục**, **Lưu**, **Tải**, **Cài đặt**, **Menu chính** và **Thoát**. Ở những cảnh không cho phép lưu, nút Lưu bị mờ đi như trong hình. Tải, Lưu và Cài đặt mở đúng những cửa sổ như ở menu chính.

![Menu trong game đè lên một cảnh: Menu, Resume, Save, Load, Settings, Main menu, Exit.](assets/game-menu.jpg)

## Thông báo

Góc trên bên trái xuất hiện những tin nhắn ngắn: một mục mới trong nhật ký, một gợi ý từ cốt truyện. Cùng lúc hiện tối đa bốn cái, mỗi cái giữ khoảng mười giây; cái khác xếp hàng chờ tới lượt.

![Thông báo dưới đồng hồ: "Hãy nhìn quanh. Có thứ gì đó nằm trong con hẻm." — một gợi ý từ phần mở đầu của Cthangover.](assets/notification.png)

## Cho tác giả mod

**Thêm nút thanh công cụ cùng bảng điều khiển của nó** bằng một mục `ui` trong manifest. Items thêm túi đồ theo cách này:

```json
"ui": [
  {
    "id": "inventory",
    "kind": "panel",
    "slot": "ModBeforeUIPanel",
    "scene_path": "scenes/ui/inventory_panel.tscn",
    "button": { "icon": "bag", "locale": "ui/inventory/title", "index": 20 },
    "priority": 10
  }
]
```

- `button.index` xếp thứ tự các biểu tượng (bản đồ 10, túi đồ 20, nhân vật 30, kỹ năng 40, menu 50, nhật ký 60, studio 90);
- `button.icon` là tên biểu tượng, `button.locale` là khóa dịch của chú thích;
- mục không có `button` là bảng điều khiển hoặc lớp phủ mà game tự mở — Crafting đặt bàn chế tạo của mình dưới dạng `"kind": "overlay"` ở khe `ModLastPanel` và mở nó từ kịch bản.

**Mở bảng điều khiển từ kịch bản** bằng hành động `ui.toggle_panel`, còn **hiện thông báo** bằng lệnh `notify`:

```scenario
notify "Mục nhật ký mới"
```

**Dùng diện mạo chung.** Bảng điều khiển viết bằng code lấy khung, nhãn, thẻ và nút từ bộ công cụ giao diện (`UiKit`) cùng các token màu và kích thước, nên bảng mới tự hòa vào tổng thể game và đi theo chủ đề của nó.

## Khi có trục trặc

1. **Thiếu nút trên thanh công cụ.** Mod thêm nút đó không nằm trong game này; hãy kiểm tra thành phần của game trong **Mod và bộ sưu tập**.
2. **Nút Lưu bị mờ.** Cảnh hiện tại không cho phép lưu; hãy đến nơi khác rồi thử lại. Tác giả cho phép lưu bằng dòng `save_allowed: true` trong phần đầu một trong các kịch bản của cảnh.
3. **Bảng điều khiển mở ra nhưng trống trơn.** Mod ấy chưa có gì để hiển thị trong game này — ví dụ, chưa có nhiệm vụ nào bắt đầu.
