namespace Order.Api.Contracts;

public sealed record MenuItemDto(
    int Id,
    string Name,
    decimal Price,
    bool IsAvailable);

public sealed record CreateOrderRequest(
    string CustomerName,
    int MenuItemId,
    int Quantity);

public sealed record OrderResponse(
    Guid Id,
    string CustomerName,
    int MenuItemId,
    string MenuItemName,
    int Quantity,
    decimal TotalPrice,
    DateTimeOffset OrderedAt,
    string Status);

public sealed record ApiError(
    string Code,
    string Message);