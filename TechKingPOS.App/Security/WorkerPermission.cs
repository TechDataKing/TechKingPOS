using System.Collections.Generic;

namespace TechKingPOS.App.Security
{
    public static class WorkerPermissions
    {
        public static readonly HashSet<string> Allowed = new()
        {
            Permissions.Sales,
            Permissions.Items,
            Permissions.Stock,
            Permissions.Credit,
            Permissions.Reports,
            Permissions.Workers,
            Permissions.Settings
        };
    }
}
