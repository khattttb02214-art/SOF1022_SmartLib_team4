namespace SmartLib.Web.Interfaces;

public interface INotificationService
{
    Task SendNotificationAsync(string message);
}