using IntelliCampus.Shared.Dtos.Room;

namespace IntelliCampus.Service_Abstraction;

public interface IRoomService
{
    Task<RoomDto> GetByIdAsync(int roomId);
    Task<IEnumerable<RoomDto>> GetAllAsync();
    Task<RoomDto> CreateAsync(CreateRoomDto dto);
    Task<RoomDto> UpdateAsync(int roomId, UpdateRoomDto dto);
    Task DeleteAsync(int roomId);
}
