using Microsoft.AspNetCore.Mvc;

namespace TaskBridge_API.Notifications;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    // In-memory store; replace with a persistent data store later.
    private static readonly List<Notification> Notifications = new();
    private static int _nextId = 1;

    [HttpGet]
    public ActionResult<IEnumerable<Notification>> GetAll() => Ok(Notifications);

    [HttpGet("{id}")]
    public ActionResult<Notification> GetById(int id)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == id);
        return notification is null ? NotFound() : Ok(notification);
    }

    [HttpPost]
    public ActionResult<Notification> Create(Notification notification)
    {
        notification.Id = _nextId++;
        notification.CreatedAt = DateTime.UtcNow;
        Notifications.Add(notification);
        return CreatedAtAction(nameof(GetById), new { id = notification.Id }, notification);
    }

    [HttpPut("{id}/read")]
    public IActionResult MarkAsRead(int id)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == id);
        if (notification is null) return NotFound();

        notification.IsRead = true;
        return NoContent();
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var notification = Notifications.FirstOrDefault(n => n.Id == id);
        if (notification is null) return NotFound();

        Notifications.Remove(notification);
        return NoContent();
    }
}
