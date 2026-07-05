using IntelliCampus.Domain.Entities;

namespace IntelliCampus.Service.Specifications;

internal sealed class RoomByNameSpec(string roomName) : BaseSpecifications<Room>(r => r.RoomName == roomName);
