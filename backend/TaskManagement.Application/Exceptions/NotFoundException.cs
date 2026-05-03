namespace TaskManagement.Application.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string resource, int id)
        : base($"{resource} が見つかりません (ID: {id})") { }
}
