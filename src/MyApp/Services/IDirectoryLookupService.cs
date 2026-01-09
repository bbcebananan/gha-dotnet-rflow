namespace MyApp.Services;

public interface IDirectoryLookupService
{
    DirectoryUserInfo? GetUserByName(string userName);

    DirectoryUserInfo? GetUserBySid(string sid);

    IEnumerable<string> GetUserGroups(string userName);

    bool IsUserInGroup(string userName, string groupName);

    bool ValidateCredentials(string userName, string password);
}

public class DirectoryUserInfo
{
    public string? UserName { get; set; }
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? Sid { get; set; }
    public string? DistinguishedName { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? LastLogon { get; set; }
    public IEnumerable<string>? Groups { get; set; }
}
