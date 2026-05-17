using System;

namespace Skillforge.Service;

public interface IAuditService
{
    Task LogAsync(int? userid,string action, string resource);
}
