using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Service_Abstraction;
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
    public async Task<ActionResult<IEnumerable<RoomDto>>> GetAll()
    {
        var rooms = await _roomService.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoomDto>> GetById(int id)
    {
        var room = await _roomService.GetByIdAsync(id);
        return Ok(room);
    }

    [HttpPost]
    [Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<ActionResult<RoomDto>> Create([FromBody] CreateRoomDto dto)
    {
        var room = await _roomService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = room.RoomId }, room);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<ActionResult<RoomDto>> Update(int id, [FromBody] UpdateRoomDto dto)
    {
        var room = await _roomService.UpdateAsync(id, dto);
        return Ok(room);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin_Bachelor,Admin_Masters,Admin_PhD,Admin_Diploma,SuperAdmin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _roomService.DeleteAsync(id);
        return NoContent();
    }
}
