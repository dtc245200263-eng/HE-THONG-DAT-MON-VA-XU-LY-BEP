using Order.Api.Contracts;
using Order.Api.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemoryOrderStore>();

var app = builder.Build();

// 1. Kiểm tra trạng thái API
app.MapGet("/", () => Results.Ok(new
{
    service = "Order.Api",
    status = "running"
}));

// 2. Lấy danh sách món ăn
app.MapGet("/menu", (InMemoryOrderStore store) =>
    Results.Ok(store.GetMenu()));

// 3. Tạo đơn đặt món mới
app.MapPost("/orders", (CreateOrderRequest request, InMemoryOrderStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.CustomerName))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_CUSTOMER",
            "Ten khach hang khong duoc de trong."));
    }

    if (request.Quantity <= 0)
    {
        return Results.BadRequest(new ApiError(
            "INVALID_QUANTITY",
            "So luong mon phai lon hon 0."));
    }

    var menuItem = store.GetMenuItem(request.MenuItemId);
    if (menuItem is null)
    {
        return Results.BadRequest(new ApiError(
            "MENU_ITEM_NOT_FOUND",
            $"Khong tim thay mon an co ma {request.MenuItemId}."));
    }

    if (!menuItem.IsAvailable)
    {
        return Results.BadRequest(new ApiError(
            "MENU_ITEM_UNAVAILABLE",
            $"Mon '{menuItem.Name}' hien tam het."));
    }

    var createdOrder = store.CreateOrder(request.CustomerName, menuItem, request.Quantity);
    return Results.Created($"/orders/{createdOrder.Id}", createdOrder);
});

// 4. Lấy danh sách đơn (lọc tùy chọn qua query string ?status=...)
app.MapGet("/orders", (string? status, InMemoryOrderStore store) =>
{
    if (!string.IsNullOrWhiteSpace(status) &&
        !InMemoryOrderStore.ValidStatuses.Contains(status.Trim(), StringComparer.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new ApiError(
            "INVALID_STATUS",
            $"Trang thai khong hop le. Cac gia tri ho tro: {string.Join(", ", InMemoryOrderStore.ValidStatuses)}."));
    }

    return Results.Ok(store.GetOrders(status));
});

// 5. Tra cứu chi tiết một đơn món theo Id
app.MapGet("/orders/{id:guid}", (Guid id, InMemoryOrderStore store) =>
{
    var order = store.GetOrderById(id);
    if (order is null)
    {
        return Results.NotFound(new ApiError(
            "ORDER_NOT_FOUND",
            $"Khong tim thay don hang voi ma {id}."));
    }

    return Results.Ok(order);
});

app.Run();