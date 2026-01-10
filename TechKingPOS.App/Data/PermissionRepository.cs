using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class PermissionRepository
    {
        // ================= SEED =================
        public static void SeedPermissions(IEnumerable<string> keys)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            foreach (var key in keys)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT OR IGNORE INTO permissions (key)
                    VALUES (@key);
                ";
                cmd.Parameters.AddWithValue("@key", key);
                cmd.ExecuteNonQuery();
            }
        }

        // ================= CHECK =================
        public static bool HasPermission(int userId, string permissionKey)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT granted
                FROM user_permissions
                WHERE user_id = @uid
                  AND permission_key = @key
                LIMIT 1;
            ";

            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@key", permissionKey);

            var result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) == 1;
        }

        // ================= SET / UPDATE =================
        public static void SetUserPermission(
            int userId,
            string permissionKey,
            bool granted
        )
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            using var transaction = conn.BeginTransaction();

            try
            {
                // 🔎 Check previous state
                bool? previous = null;

                var checkCmd = conn.CreateCommand();
                checkCmd.Transaction = transaction;
                checkCmd.CommandText = @"
                    SELECT granted
                    FROM user_permissions
                    WHERE user_id = @uid
                      AND permission_key = @key;
                ";
                checkCmd.Parameters.AddWithValue("@uid", userId);
                checkCmd.Parameters.AddWithValue("@key", permissionKey);

                var prevResult = checkCmd.ExecuteScalar();
                if (prevResult != null)
                    previous = Convert.ToInt32(prevResult) == 1;

                // 💾 UPSERT permission
                var cmd = conn.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO user_permissions (
                        user_id,
                        permission_key,
                        granted,
                        granted_at
                    )
                    VALUES (
                        @uid,
                        @key,
                        @granted,
                        @at
                    )
                    ON CONFLICT(user_id, permission_key)
                    DO UPDATE SET
                        granted = @granted,
                        granted_at = @at;
                ";

                cmd.Parameters.AddWithValue("@uid", userId);
                cmd.Parameters.AddWithValue("@key", permissionKey);
                cmd.Parameters.AddWithValue("@granted", granted ? 1 : 0);
                cmd.Parameters.AddWithValue("@at", DateTime.UtcNow.ToString("s"));
                cmd.ExecuteNonQuery();

                // 📝 ACTIVITY LOG
                ActivityRepository.Log(conn, transaction, new Activity
                {
                    EntityType = "USER_PERMISSION",
                    EntityId = userId,
                    EntityName = permissionKey,
                    Action = granted ? "PERMISSION_GRANTED" : "PERMISSION_REVOKED",
                    BeforeValue = previous.HasValue
                        ? (previous.Value ? "Granted" : "Denied")
                        : null,
                    AfterValue = granted ? "Granted" : "Denied",
                    Reason = $"Permission {(granted ? "granted" : "revoked")}",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = SessionContext.CurrentBranchId,
                    CreatedAt = DateTime.UtcNow
                });

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
