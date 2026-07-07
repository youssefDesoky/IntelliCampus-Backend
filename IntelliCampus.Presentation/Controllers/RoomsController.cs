using IntelliCampus.shared.Pagination;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Params;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntelliCampus.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoomsController : ControllerBase
{
    private readonly IRoomService _roomService;

    public RoomsController(IRoomService roomService)
    {
        _roomService = roomService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<RoomDto>>> GetAll([FromQuery] RoomQueryParams queryParams)
    {
        var rooms = await _roomService.GetAllAsync(queryParams);
        return Ok(rooms);
    }

    [HttpGet("types")]
    public ActionResult<IEnumerable<object>> GetTypes()
    {
        var types = new[]
        {
            new { value = "Hall", label = "Hall", labelAr = "قاعة" },
            new { value = "Lab", label = "Lab", labelAr = "معمل" },
            new { value = "Classroom", label = "Classroom", labelAr = "فصل دراسي" },
            new { value = "Office", label = "Office", labelAr = "مكتب" },
            new { value = "Conference", label = "Conference Room", labelAr = "قاعة مؤتمرات" },
            new { value = "CommonRooms", label = "Common Rooms", labelAr = "غرف مشتركة" },
        };
        return Ok(types);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto)
    {
        var room = await _roomService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, room);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        var room = await _roomService.UpdateAsync(id, dto);
        return Ok(room);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return NoContent();
    }
}
