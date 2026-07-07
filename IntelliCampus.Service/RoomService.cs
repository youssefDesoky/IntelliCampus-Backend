using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Service.Specifications;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Params;

namespace IntelliCampus.Service;

public class RoomService(IUnitOfWork unitOfWork, ICurrentAdminContext adminContext) : IRoomService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly ICurrentAdminContext _adminContext = adminContext;

    private IGenericRepository<Room, int> Rooms
        => _unitOfWork.GetRepository<Room, int>();

    public async Task<RoomDto> GetByIdAsync(int roomId)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            throw new RoomNotFoundException(roomId);

        if (_adminContext.IsAdmin)
            await _adminContext.EnsureCanAccessFacultyAsync(room.FacultyId);

        return MapToDto(room);
    }

    public async Task<PaginatedResult<RoomDto>> GetAllAsync(RoomQueryParams queryParams)
    {
        if (_adminContext.IsAdmin)
            queryParams.FacultyId = await _adminContext.GetFacultyIdAsync();

        var spec = new RoomSpec(queryParams);
        var rooms = await Rooms.GetAllAsync(spec, asNoTracking: true);
        var dataToReturn = rooms.Select(MapToDto).ToList();

        var countSpec = new RoomCountSpec(queryParams);
        var totalCount = await Rooms.CountAsync(countSpec);

        return new PaginatedResult<RoomDto>(queryParams.PageIndex, dataToReturn.Count, totalCount, dataToReturn);
    }

    public async Task<RoomDto> CreateAsync(CreateRoomDto dto)
    {
        await _adminContext.EnsureAdminHasFacultyAsync();

        var room = new Room
        {
            RoomName = dto.RoomName,
            RoomNameAr = dto.RoomNameAr,
            Capacity = dto.Capacity,
            Type = dto.Type,
            IsExamHall = dto.IsExamHall,
            Location = dto.Location,
            LocationAr = dto.LocationAr,
            FacultyId = dto.FacultyId
        };

        Rooms.Add(room);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(room);
    }

    public async Task<RoomDto> UpdateAsync(int roomId, UpdateRoomDto dto)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            throw new RoomNotFoundException(roomId);

        await _adminContext.EnsureCanAccessFacultyAsync(room.FacultyId);

        if (dto.RoomName is not null)
            room.RoomName = dto.RoomName;

        if (dto.RoomNameAr is not null)
            room.RoomNameAr = dto.RoomNameAr;

        if (dto.Capacity.HasValue)
            room.Capacity = dto.Capacity.Value;
        if (dto.Type is not null)
            room.Type = dto.Type;
        if (dto.IsExamHall.HasValue)
            room.IsExamHall = dto.IsExamHall.Value;
        if (dto.Location is not null)
            room.Location = dto.Location;
        if (dto.LocationAr is not null)
            room.LocationAr = dto.LocationAr;

        room.FacultyId = dto.FacultyId;

        Rooms.Update(room);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(room);
    }

    public async Task DeleteAsync(int roomId)
    {
        var room = await Rooms.GetByIdAsync(roomId);

        if (room is null)
            throw new RoomNotFoundException(roomId);

        await _adminContext.EnsureCanAccessFacultyAsync(room.FacultyId);

        Rooms.Delete(room);
        await _unitOfWork.SaveChangesAsync();
    }

    private static RoomDto MapToDto(Room room)
    {
        return new RoomDto
        {
            RoomId = room.RoomId,
            RoomName = room.RoomName,
            RoomNameAr = room.RoomNameAr,
            Capacity = room.Capacity,
            Type = room.Type,
            Location = room.Location,
            LocationAr = room.LocationAr,
            IsExamHall = room.IsExamHall,
            FacultyId = room.FacultyId,
            FacultyName = room.Faculty?.FacultyName
        };
    }
}
