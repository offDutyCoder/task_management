namespace TaskManagement.Application.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "このリソースへのアクセス権がありません")
        : base(message) { }
}
