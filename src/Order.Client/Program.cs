using System.Net.Http.Json;
using Order.Client.Contracts;

var apiBaseUrl = args.Length > 0 ? args[0] : "http://localhost:5000";

using var http = new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
};

Console.WriteLine($"=== KET NOI TOI ORDER API ({apiBaseUrl}) ===\n");

// 1. Xem danh sách món ăn (GET /menu)
var menu = await http.GetFromJsonAsync<List<MenuItemDto>>("/menu") ?? [];
Console.WriteLine("1. Danh sach mon an:");
foreach (var item in menu)
{
    var statusText = item.IsAvailable ? "Con mon" : "Tam het";
    Console.WriteLine($"   - [{item.Id}] {item.Name}: {item.Price:N0} VND ({statusText})");
}

// 2. Đường thành công: Tạo đơn đặt món hợp lệ (POST /orders -> 201 Created)
Console.WriteLine("\n2. Gui yeu cau dat mon hop le:");
var validRequest = new CreateOrderRequest("Nguyen Van A", MenuItemId: 1, Quantity: 2);
var createdOrder = await SendAndHandleAsync<OrderResponse>(
    () => http.PostAsJsonAsync("/orders", validRequest));

if (createdOrder is not null)
{
    Console.WriteLine($"   -> Dat thanh cong! Ma don: {createdOrder.Id}");
    Console.WriteLine($"   -> Mon: {createdOrder.MenuItemName} x{createdOrder.Quantity} | Tong: {createdOrder.TotalPrice:N0} VND | Trang thai: {createdOrder.Status}");

    // 3. Tra cứu lại chi tiết đơn vừa tạo (GET /orders/{id} -> 200 OK)
    Console.WriteLine("\n3. Tra cuu chi tiet don vua dat:");
    var detail = await SendAndHandleAsync<OrderResponse>(
        () => http.GetAsync($"/orders/{createdOrder.Id}"));
    if (detail is not null)
    {
        Console.WriteLine($"   -> Khach hang: {detail.CustomerName} | Thoi diem dat: {detail.OrderedAt:HH:mm:ss dd/MM/yyyy} | Trang thai: {detail.Status}");
    }
}

// 4. Lọc danh sách đơn theo trạng thái Pending (GET /orders?status=Pending)
Console.WriteLine("\n4. Danh sach don dang o trang thai Pending:");
var pendingOrders = await SendAndHandleAsync<List<OrderResponse>>(
    () => http.GetAsync("/orders?status=Pending")) ?? [];
Console.WriteLine($"   -> Tim thay {pendingOrders.Count} don hang.");

// 5. Đường lỗi 400 Bad Request: Đặt món thiếu tên khách hàng
Console.WriteLine("\n5. Kiem thu duong loi 400 (thieu ten khach hang):");
var invalidRequest = new CreateOrderRequest("", MenuItemId: 1, Quantity: 1);
await SendAndHandleAsync<OrderResponse>(
    () => http.PostAsJsonAsync("/orders", invalidRequest));

// 6. Đường lỗi 404 Not Found: Tra cứu mã đơn không tồn tại
Console.WriteLine("\n6. Kiem thu duong loi 404 (ma don khong ton tai):");
await SendAndHandleAsync<OrderResponse>(
    () => http.GetAsync($"/orders/{Guid.NewGuid()}"));

static async Task<T?> SendAndHandleAsync<T>(Func<Task<HttpResponseMessage>> sendAsync)
{
    using var response = await sendAsync();
    Console.WriteLine($"   [HTTP {(int)response.StatusCode} {response.StatusCode}]");

    if (response.IsSuccessStatusCode)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }

    var error = await response.Content.ReadFromJsonAsync<ApiError>();
    Console.WriteLine($"   [API ERROR] {error?.Code}: {error?.Message}");
    return default;
}