using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IntelliCampus.Service_Abstraction;

namespace IntelliCampus.Presentation.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IFriendService _friendService;
    private readonly IGroupService _groupService;

    public ChatHub(IChatService chatService, IFriendService friendService, IGroupService groupService)
    {
        _chatService = chatService;
        _friendService = friendService;
        _groupService = groupService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier!;
        await Clients.All.SendAsync("UserOnline", userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier!;
        await Clients.All.SendAsync("UserOffline", userId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendPrivateMessage(string recipientId, string content)
    {
        var senderId = Context.UserIdentifier!;
        var msg = await _chatService.SendMessageAsync(senderId, recipientId, content);

        await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", msg);
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", msg);
    }

    public async Task SendGroupMessage(string groupName, string content)
    {
        var senderId = Context.UserIdentifier!;
        var msg = await _chatService.SendMessageAsync(senderId, string.Empty, content, groupName);

        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", msg);
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
        var msg = await _chatService.DeleteMessageAsync(userId, messageId);
        if (msg == null) return;

        if (!string.IsNullOrEmpty(msg.GroupName))
            await Clients.Group(msg.GroupName).SendAsync("MessageDeleted", msg.MessageId);
        else
        {
            await Clients.User(msg.SenderId).SendAsync("MessageDeleted", msg.MessageId);
            await Clients.User(msg.RecipientId).SendAsync("MessageDeleted", msg.MessageId);
        }
    }

    public async Task EditMessage(string messageId, string newContent)
    {
        var userId = Context.UserIdentifier!;
        var msg = await _chatService.EditMessageAsync(userId, messageId, newContent);
        if (msg == null) return;

        if (!string.IsNullOrEmpty(msg.GroupName))
            await Clients.Group(msg.GroupName).SendAsync("MessageEdited", msg.MessageId, msg.Content, msg.IsEdited);
        else
        {
            await Clients.User(msg.SenderId).SendAsync("MessageEdited", msg.MessageId, msg.Content, msg.IsEdited);
            await Clients.User(msg.RecipientId).SendAsync("MessageEdited", msg.MessageId, msg.Content, msg.IsEdited);
        }
    }

    public async Task PinMessage(string messageId)
    {
        var userId = Context.UserIdentifier!;
        var msg = await _chatService.PinMessageAsync(userId, messageId);
        if (msg == null) return;

        if (!string.IsNullOrEmpty(msg.GroupName))
            await Clients.Group(msg.GroupName).SendAsync("MessagePinned", msg.MessageId, msg.Content);
        else
        {
            await Clients.User(msg.SenderId).SendAsync("MessagePinned", msg.MessageId, msg.Content);
            await Clients.User(msg.RecipientId).SendAsync("MessagePinned", msg.MessageId, msg.Content);
        }
    }

    public async Task UnpinMessage(string messageId)
    {
        var userId = Context.UserIdentifier!;
        var msg = await _chatService.UnpinMessageAsync(userId, messageId);
        if (msg == null) return;

        if (!string.IsNullOrEmpty(msg.GroupName))
            await Clients.Group(msg.GroupName).SendAsync("MessageUnpinned", msg.MessageId);
        else
        {
            await Clients.User(msg.SenderId).SendAsync("MessageUnpinned", msg.MessageId);
            await Clients.User(msg.RecipientId).SendAsync("MessageUnpinned", msg.MessageId);
        }
    }

    public async Task BroadcastTypingStatus(string receiverId, bool isTyping)
    {
        var senderId = Context.UserIdentifier!;
        await Clients.User(receiverId).SendAsync("ReceiveTypingStatus", senderId, isTyping);
    }
}
