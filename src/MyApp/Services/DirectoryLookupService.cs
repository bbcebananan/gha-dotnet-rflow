using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Logging;

namespace MyApp.Services;

public class DirectoryLookupService : IDirectoryLookupService
{
    private readonly ILogger<DirectoryLookupService> _logger;

    public DirectoryLookupService(ILogger<DirectoryLookupService> logger)
    {
        _logger = logger;
    }

    public DirectoryUserInfo? GetUserByName(string userName)
    {
        _logger.LogDebug("Looking up user by name: {UserName}", userName);

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserName}", userName);
                return null;
            }

            return MapUserPrincipal(user);
        }
        catch (PrincipalServerDownException ex)
        {
            _logger.LogError(ex, "Directory server unavailable");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup user {UserName}", userName);
            return null;
        }
    }

    public DirectoryUserInfo? GetUserBySid(string sid)
    {
        _logger.LogDebug("Looking up user by SID: {Sid}", sid);

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, IdentityType.Sid, sid);

            if (user == null)
            {
                _logger.LogWarning("User not found with SID: {Sid}", sid);
                return null;
            }

            return MapUserPrincipal(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lookup user by SID {Sid}", sid);
            return null;
        }
    }

    public IEnumerable<string> GetUserGroups(string userName)
    {
        _logger.LogDebug("Getting groups for user: {UserName}", userName);

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserName}", userName);
                return Enumerable.Empty<string>();
            }

            var groups = user.GetGroups()
              .Select(g => g.Name)
              .Where(name => name != null)
              .Cast<string>()
              .ToList();

            _logger.LogDebug("Found {Count} groups for user {UserName}", groups.Count, userName);

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get groups for user {UserName}", userName);
            return Enumerable.Empty<string>();
        }
    }

    public bool IsUserInGroup(string userName, string groupName)
    {
        _logger.LogDebug("Checking if user {UserName} is in group {GroupName}", userName, groupName);

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);
            using var group = GroupPrincipal.FindByIdentity(context, groupName);

            if (user == null || group == null)
            {
                return false;
            }

            return user.IsMemberOf(group);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check group membership for {UserName} in {GroupName}",
              userName, groupName);
            return false;
        }
    }

    public bool ValidateCredentials(string userName, string password)
    {
        _logger.LogDebug("Validating credentials for user: {UserName}", userName);

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            return context.ValidateCredentials(userName, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate credentials for {UserName}", userName);
            return false;
        }
    }

    private static DirectoryUserInfo MapUserPrincipal(UserPrincipal user)
    {
        return new DirectoryUserInfo
        {
            UserName = user.SamAccountName,
            DisplayName = user.DisplayName,
            Email = user.EmailAddress,
            Sid = user.Sid?.ToString(),
            DistinguishedName = user.DistinguishedName,
            IsEnabled = user.Enabled ?? false,
            LastLogon = user.LastLogon,
            Groups = user.GetGroups()
            .Select(g => g.Name)
            .Where(name => name != null)
            .Cast<string>()
            .ToList()
        };
    }
}
