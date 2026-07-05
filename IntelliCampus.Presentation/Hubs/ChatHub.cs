using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Domain.Entities.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace IntelliCampus.Presentation.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IFriendService _friendService;
    private readonly IGroupService _groupService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChatHub> _logger;
    private readonly IFahimUserService _fahimUserService;

    public ChatHub(IChatService chatService, IFriendService friendService,
        IGroupService groupService, INotificationService notificationService,
        IHubContext<ChatHub> hubContext, IServiceScopeFactory scopeFactory,
        ILogger<ChatHub> logger, IFahimUserService fahimUserService)
    {
        _chatService = chatService;
        _friendService = friendService;
        _groupService = groupService;
        _notificationService = notificationService;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _fahimUserService = fahimUserService;
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

    public async Task SendCourseQuestion(string recipientId, string courseCode, string courseName, string content)
    {
        var senderId = Context.UserIdentifier!;

        var msg = await _chatService.SendMessageAsync(senderId, recipientId, content);
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", msg);
        await Clients.User(recipientId).SendAsync("ReceiveTypingStatus", recipientId, true);

        _ = HandleFahimCourseReplyAsync(senderId, courseCode, courseName, content);
    }

    private async Task HandleFahimCourseReplyAsync(string senderId, string courseCode, string courseName, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        const string fahimUserId = "-1";

        try
        {
            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceiveTypingStatus", fahimUserId, true);

            var reply = await chatService.GenerateFahimCourseReplyAsync(senderId, courseCode, courseName, content);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceiveTypingStatus", fahimUserId, false);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceivePrivateMessage", reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fahim AI course reply failed for user {SenderId} course {Course}", senderId, courseCode);

            var fallback = await chatService.GenerateFahimFallbackReplyAsync(senderId, ex);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceivePrivateMessage", fallback);
        }
    }

    public async Task SendPrivateMessage(string recipientId, string content)
    {
        var senderId = Context.UserIdentifier!;
        var msg = await _chatService.SendMessageAsync(senderId, recipientId, content);

        await Clients.User(recipientId).SendAsync("ReceivePrivateMessage", msg);
        await Clients.User(senderId).SendAsync("ReceivePrivateMessage", msg);

        // Fire-and-forget Fahim AI reply — uses new scope for long-running LLM call
        if (_fahimUserService.IsFahim(recipientId))
        {
            _ = HandleFahimReplyAsync(senderId, content);
        }
    }

    private async Task HandleFahimReplyAsync(string senderId, string content)
    {
        using var scope = _scopeFactory.CreateScope();
        var chatService = scope.ServiceProvider.GetRequiredService<IChatService>();
        const string fahimUserId = "-1";

        try
        {
            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceiveTypingStatus", fahimUserId, true);

            var reply = await chatService.GenerateFahimReplyAsync(senderId, content);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceiveTypingStatus", fahimUserId, false);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceivePrivateMessage", reply);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fahim AI reply failed for user {SenderId}", senderId);

            var fallback = await chatService.GenerateFahimFallbackReplyAsync(senderId, ex);

            await _hubContext.Clients.User(senderId)
                .SendAsync("ReceivePrivateMessage", fallback);
        }
    }

    public async Task SendGroupMessage(string groupName, string content)
    {
        var senderId = Context.UserIdentifier!;
        var msg = await _chatService.SendMessageAsync(senderId, string.Empty, content, groupName);

        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", msg);

        var groupIdStr = groupName.Replace("group_", "");
        if (int.TryParse(groupIdStr, out var groupId) && msg.SenderName is not null)
        {
            var group = await _groupService.GetGroupByIdAsync(groupId, int.Parse(senderId));
            if (group is not null)
            {
                var memberIds = group.Members
                    .Select(m => m.UserId)
                    .Where(id => id != int.Parse(senderId))
                    .ToList();

                if (memberIds.Count > 0)
                {
                    var titlePreview = msg.Content.Length > 80 ? msg.Content[..80] + "..." : msg.Content;
                    await _notificationService.SendToManyAsync(
                        memberIds,
                        NotificationType.NewMessage,
                        $"{msg.SenderName} in {group.Title}: {titlePreview}",
                        "Group Message",
                        $"/?openChat=group&userId={groupName}&userName={Uri.EscapeDataString(group.Title)}");
                }
            }
        }
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
