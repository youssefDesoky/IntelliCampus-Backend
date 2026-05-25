using Microsoft.AspNetCore.SignalR;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Presentation.Hubs;

public class ChatHub : Hub
{
    private readonly IChatService _chatService;

    public ChatHub(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task SendPrivateMessage(string recipientId, string content)
    {
        var senderId = Context.UserIdentifier!;
        await _chatService.SendMessageAsync(senderId, recipientId, content);

        await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", senderId, content);
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", senderId, content);
    }

    public async Task SendGroupMessage(string groupName, string content)
    {
        var senderId = Context.UserIdentifier!;
        await _chatService.SendMessageAsync(senderId, string.Empty, content);

        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", senderId, content);
    }

    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task MarkMessageAsRead(string messageId)
    {
        var userId = Context.UserIdentifier!;
        await _chatService.MarkMessageAsReadAsync(userId, messageId);
    }

    public async Task DeleteMessage(string messageId)
    {
        var userId = Context.UserIdentifier!;
        await _chatService.DeleteMessageAsync(userId, messageId);
    }

    public async Task EditMessage(string messageId, string newContent)
    {
        var userId = Context.UserIdentifier!;
        await _chatService.EditMessageAsync(userId, messageId, newContent);
    }

    public async Task BroadcastTypingStatus(string receiverId, bool isTyping)
    {
        var senderId = Context.UserIdentifier!;
        await Clients.User(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
    }
}