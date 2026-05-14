namespace RDO.App.Services;

internal static class UserSession
{
    public static string Email    { get; private set; } = string.Empty;
    public static string Password { get; private set; } = string.Empty;
    public static string Name     { get; private set; } = string.Empty;

    public static void Set(string email, string password, string name = "")
    {
        Email    = email;
        Password = password;
        Name     = name;
    }

    public static void Clear()
    {
        Email    = string.Empty;
        Password = string.Empty;
        Name     = string.Empty;
    }
}
