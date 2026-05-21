using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Room;

namespace IntelliCampus.Service;

public class RoomService(IUnitOfWork unitOfWork) : IRoomService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    private IGenericRepository<Room, int> Rooms
        => _unitOfWork.GetRepository<Room, int>();

    public async Task<RoomDto?> GetByIdAsync(int roomId)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            return null;

        return MapToDto(room);
    }

    public async Task<IEnumerable<RoomDto>> GetAllAsync()
    {
        var rooms = await Rooms.GetAllAsync();
        return rooms.Select(MapToDto);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomDto dto)
    {
        var room = new Room
        {
            RoomName = dto.RoomName,
            RoomNameAr = dto.RoomNameAr,
            Capacity = dto.Capacity
        };

        Rooms.Add(room);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(room);
    }

    public async Task<RoomDto?> UpdateAsync(int roomId, UpdateRoomDto dto)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            return null;

        if (dto.RoomName is not null)
            room.RoomName = dto.RoomName;

        if (dto.RoomNameAr is not null)
            room.RoomNameAr = dto.RoomNameAr;

        if (dto.Capacity.HasValue)
            room.Capacity = dto.Capacity.Value;

        Rooms.Update(room);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(room);
    }

    public async Task<bool> DeleteAsync(int roomId)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            return false;

        Rooms.Delete(room);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    private static RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            RoomId = room.RoomId,
            RoomName = room.RoomName,
            RoomNameAr = room.RoomNameAr,
            Capacity = room.Capacity
        };
    }
}
