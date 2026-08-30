using Microsoft.AspNetCore.Mvc;
using TaskBridge_API.Notifications;
using Xunit;

namespace TaskBridge_API.Tests;

public class NotificationsControllerTests
{
    [Fact]
    public void Create_ThenMarkAsRead_SetsIsReadTrue()
    {
        var controller = new NotificationsController();
        var notification = new Notification { Message = "Task completed" };

        var createResult = controller.Create(notification);
        var created = Assert.IsType<CreatedAtActionResult>(createResult.Result);
        var createdNotification = Assert.IsType<Notification>(created.Value);

        var markResult = controller.MarkAsRead(createdNotification.Id);
        Assert.IsType<NoContentResult>(markResult);

        var getResult = controller.GetById(createdNotification.Id);
        var ok = Assert.IsType<OkObjectResult>(getResult.Result);
        var fetched = Assert.IsType<Notification>(ok.Value);

        Assert.True(fetched.IsRead);
    }

    [Fact]
    public void MarkAsRead_UnknownId_ReturnsNotFound()
    {
        var controller = new NotificationsController();

        var result = controller.MarkAsRead(-1);

        Assert.IsType<NotFoundResult>(result);
    }
}
