---
trigger: manual
---

1. Kiến trúc & Pattern (Architecture First)
MVVM Strict Enforcement: Luôn sử dụng CommunityToolkit.Mvvm. Tuyệt đối không viết logic nghiệp vụ trong code-behind (.xaml.cs).

Dependency Injection (DI): Giả định project sử dụng Microsoft.Extensions.DependencyInjection. Code mẫu phải bao gồm cách đăng ký Service và ViewModel.

Clean Code: Sử dụng StrongReferenceMessenger để truyền tin giữa các ViewModel thay vì dùng sự kiện trực tiếp để tránh memory leak.

2. Tối ưu Hiệu năng (Performance Mastery)
Compiled Bindings: Luôn sử dụng {x:Bind}. Phải chỉ định rõ x:DataType trong DataTemplate.

UI Thread Management: Sử dụng DispatcherQueue.TryEnqueue khi cần cập nhật UI từ background thread. Ưu tiên Task.Run cho các tác vụ nặng để tránh đóng băng UI.

Virtualization: Khi xử lý danh sách lớn, hãy cấu hình ItemsRepeater hoặc ListView với ItemsPanel là ItemsStackPanel để đảm bảo UI Virtualization.

3. XAML & Modern UI (Fluent Design)
Theme Resources: Không bao giờ hard-code màu sắc. Luôn dùng {ThemeResource ...} (ví dụ: SystemControlForegroundBaseHighBrush).

Spacing & Alignment: Tuân thủ hệ thống 4px/8px của Fluent Design. Sử dụng Standard Tile/Card layout.

Visual States: Sử dụng VisualStateManager để xử lý các trạng thái Adaptive UI (thay đổi layout khi resize cửa sổ).

4. Code Standards (Senior Level)
Error Handling: Sử dụng Try-Catch kết hợp với ContentDialog hoặc InfoBar để thông báo lỗi cho người dùng một cách tinh tế.

Documentation: Code phải có chú thích XML (///) cho các phương thức phức tạp.

Modern C#: Sử dụng C# 10/12 features (file-scoped namespaces, primary constructors, target-typed new).

5. Yêu cầu Phản hồi
Cung cấp cấu trúc file rõ ràng (Ví dụ: Views/MainPage.xaml, ViewModels/MainViewModel.cs).

Nếu giải pháp có thư viện bên ngoài (như WinUI Community Toolkit), hãy nêu rõ tên NuGet package cần cài đặt.