using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service.Exceptions;
using IntelliCampus.Shared.Dtos.Room;
using IntelliCampus.Shared.Params;
using IntelliCampus.UnitTests.TestHelpers;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class RoomServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IGenericRepository<Room, int>> _roomRepoMock;
    private readonly RoomService _sut;

    public RoomServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _roomRepoMock = new Mock<IGenericRepository<Room, int>>();
        _unitOfWorkMock.Setup(u => u.GetRepository<Room, int>()).Returns(_roomRepoMock.Object);
        _sut = new RoomService(_unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRoom_ReturnsRoomDtoWithAllProperties()
    {
        var room = TestDataFactory.RoomFaker.Generate();

        _roomRepoMock.Setup(r => r.GetByIdAsync(room.RoomId)).ReturnsAsync(room);

        var result = await _sut.GetByIdAsync(room.RoomId);

        result.RoomId.Should().Be(room.RoomId);
        result.RoomName.Should().Be(room.RoomName);
        result.RoomNameAr.Should().Be(room.RoomNameAr);
        result.Capacity.Should().Be(room.Capacity);
        result.Type.Should().Be(room.Type);
        result.Location.Should().Be(room.Location);
        result.LocationAr.Should().Be(room.LocationAr);
        _roomRepoMock.Verify(r => r.GetByIdAsync(room.RoomId), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingRoom_ThrowsRoomNotFoundException()
    {
        _roomRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Room?)null);

        await _sut.Invoking(s => s.GetByIdAsync(999)).Should().ThrowAsync<RoomNotFoundException>();

        _roomRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithRooms_ReturnsPaginatedResultWithCorrectData()
    {
        var rooms = TestDataFactory.RoomFaker.Generate(5);
        var queryParams = new RoomQueryParams { PageIndex = 1, PageSize = 10 };

        _roomRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Room>>())).ReturnsAsync(rooms);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Room>>())).ReturnsAsync(5);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().HaveCount(5);
        result.TotalCount.Should().Be(5);
        result.Data.All(r => r.RoomId > 0).Should().BeTrue();
        _roomRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Room>>()), Times.Once);
        _roomRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Room>>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoRooms_ReturnsEmptyPaginatedResult()
    {
        var queryParams = new RoomQueryParams { PageIndex = 1, PageSize = 10 };

        _roomRepoMock.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<Room>>())).ReturnsAsync([]);
        _roomRepoMock.Setup(r => r.CountAsync(It.IsAny<ISpecifications<Room>>())).ReturnsAsync(0);

        var result = await _sut.GetAllAsync(queryParams);

        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _roomRepoMock.Verify(r => r.GetAllAsync(It.IsAny<ISpecifications<Room>>()), Times.Once);
        _roomRepoMock.Verify(r => r.CountAsync(It.IsAny<ISpecifications<Room>>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithAllFields_CreatesAndReturnsDtoWithExpectedValues()
    {
        var dto = new CreateRoomDto
        {
            RoomName = "Room 101",
            RoomNameAr = "قاعة 101",
            Capacity = 30,
            Type = "Lecture",
            Location = "Building A",
            LocationAr = "المبنى أ"
        };
        Room? capturedRoom = null;
        _roomRepoMock.Setup(r => r.Add(It.IsAny<Room>())).Callback<Room>(r => capturedRoom = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.RoomId.Should().Be(0);
        result.RoomName.Should().Be("Room 101");
        result.RoomNameAr.Should().Be("قاعة 101");
        result.Capacity.Should().Be(30);
        result.Type.Should().Be("Lecture");
        result.Location.Should().Be("Building A");
        result.LocationAr.Should().Be("المبنى أ");
        capturedRoom.Should().NotBeNull();
        capturedRoom!.RoomName.Should().Be("Room 101");
        capturedRoom.RoomNameAr.Should().Be("قاعة 101");
        capturedRoom.Capacity.Should().Be(30);
        capturedRoom.Type.Should().Be("Lecture");
        capturedRoom.Location.Should().Be("Building A");
        capturedRoom.LocationAr.Should().Be("المبنى أ");
        _roomRepoMock.Verify(r => r.Add(It.IsAny<Room>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalFields_SetsDefaultValues()
    {
        var dto = new CreateRoomDto { RoomName = "Minimal" };

        Room? capturedRoom = null;
        _roomRepoMock.Setup(r => r.Add(It.IsAny<Room>())).Callback<Room>(r => capturedRoom = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.CreateAsync(dto);

        result.RoomName.Should().Be("Minimal");
        result.RoomNameAr.Should().BeNull();
        result.Capacity.Should().Be(0);
        result.Type.Should().BeNull();
        result.Location.Should().BeNull();
        result.LocationAr.Should().BeNull();
        capturedRoom!.RoomName.Should().Be("Minimal");
        capturedRoom.Capacity.Should().Be(0);
        _roomRepoMock.Verify(r => r.Add(It.Is<Room>(room => room.RoomName == "Minimal")), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_AllFieldsProvided_UpdatesAndReturnsDtoWithNewValues()
    {
        var room = TestDataFactory.RoomFaker.Generate();
        var dto = new UpdateRoomDto
        {
            RoomName = "Updated Name",
            RoomNameAr = "الاسم المحدث",
            Capacity = 50,
            Type = "Lab",
            Location = "Building B",
            LocationAr = "المبنى ب"
        };

        _roomRepoMock.Setup(r => r.GetByIdAsync(room.RoomId)).ReturnsAsync(room);
        _roomRepoMock.Setup(r => r.Update(room));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(room.RoomId, dto);

        result.RoomName.Should().Be("Updated Name");
        result.RoomNameAr.Should().Be("الاسم المحدث");
        result.Capacity.Should().Be(50);
        result.Type.Should().Be("Lab");
        result.Location.Should().Be("Building B");
        result.LocationAr.Should().Be("المبنى ب");
        room.RoomName.Should().Be("Updated Name");
        room.RoomNameAr.Should().Be("الاسم المحدث");
        room.Capacity.Should().Be(50);
        room.Type.Should().Be("Lab");
        room.Location.Should().Be("Building B");
        room.LocationAr.Should().Be("المبنى ب");
        _roomRepoMock.Verify(r => r.GetByIdAsync(room.RoomId), Times.Once);
        _roomRepoMock.Verify(r => r.Update(room), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_OnlyChangesProvidedFields()
    {
        var room = TestDataFactory.RoomFaker.Generate();
        var originalCapacity = room.Capacity;
        var originalType = room.Type;
        var originalLocation = room.Location;
        var originalLocationAr = room.LocationAr;
        var originalRoomNameAr = room.RoomNameAr;
        var dto = new UpdateRoomDto { RoomName = "Only Name Changed" };

        _roomRepoMock.Setup(r => r.GetByIdAsync(room.RoomId)).ReturnsAsync(room);
        _roomRepoMock.Setup(r => r.Update(room));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _sut.UpdateAsync(room.RoomId, dto);

        result.RoomName.Should().Be("Only Name Changed");
        room.Capacity.Should().Be(originalCapacity);
        room.Type.Should().Be(originalType);
        room.Location.Should().Be(originalLocation);
        room.LocationAr.Should().Be(originalLocationAr);
        room.RoomNameAr.Should().Be(originalRoomNameAr);
        _roomRepoMock.Verify(r => r.GetByIdAsync(room.RoomId), Times.Once);
        _roomRepoMock.Verify(r => r.Update(room), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingRoom_ThrowsRoomNotFoundException()
    {
        _roomRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Room?)null);

        await _sut.Invoking(s => s.UpdateAsync(999, new UpdateRoomDto())).Should().ThrowAsync<RoomNotFoundException>();

        _roomRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _roomRepoMock.Verify(r => r.Update(It.IsAny<Room>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingRoom_DeletesAndSaves()
    {
        var room = TestDataFactory.RoomFaker.Generate();

        _roomRepoMock.Setup(r => r.GetByIdAsync(room.RoomId)).ReturnsAsync(room);
        Room? capturedDeleted = null;
        _roomRepoMock.Setup(r => r.Delete(It.IsAny<Room>())).Callback<Room>(r => capturedDeleted = r);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        await _sut.Invoking(s => s.DeleteAsync(room.RoomId)).Should().NotThrowAsync();

        capturedDeleted.Should().BeSameAs(room);
        _roomRepoMock.Verify(r => r.GetByIdAsync(room.RoomId), Times.Once);
        _roomRepoMock.Verify(r => r.Delete(room), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingRoom_ThrowsRoomNotFoundException()
    {
        _roomRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Room?)null);

        await _sut.Invoking(s => s.DeleteAsync(999)).Should().ThrowAsync<RoomNotFoundException>();

        _roomRepoMock.Verify(r => r.GetByIdAsync(999), Times.Once);
        _roomRepoMock.Verify(r => r.Delete(It.IsAny<Room>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
