namespace RDO.App.Services;

internal static class UserSession
{
    public static string Email    { get; private set; } = string.Empty;
    public static string Password { get; private set; } = string.Empty;

    public static void Set(string email, string password)
    {
        Email    = email;
        Password = password;
    }

    public static void Clear()
    {
        Email    = string.Empty;
        Password = string.Empty;
    }
}
