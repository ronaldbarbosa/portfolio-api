namespace Portfolio.Api.Dtos;

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalItemCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
