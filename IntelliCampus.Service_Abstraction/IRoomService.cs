using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service_Abstraction;

public interface IRoomService
{
    Task<RoomDto> GetByIdAsync(int roomId);
    Task<PaginatedResult<RoomDto>> GetAllAsync(RoomQueryParams queryParams);
    Task<RoomDto> CreateAsync(CreateRoomDto dto);
    Task<RoomDto> UpdateAsync(int roomId, UpdateRoomDto dto);
    Task DeleteAsync(int roomId);
}
