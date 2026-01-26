using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class BranchRepository
    {
        // ================= GET ALL =================
        public static List<Branch> GetAll()
        {
            var list = new List<Branch>();

            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            // Admins can see all branches, non-admins see only their own branch
            if (SessionContext.IsAdmin)
            {
                cmd.CommandText = @"
                    SELECT Id, Name, Code, IsActive
                    FROM Branch
                    ORDER BY Name;
                ";
            }
            else
            {
                cmd.CommandText = @"
                    SELECT Id, Name, Code, IsActive
                    FROM Branch
                    WHERE Id = @branchId
                    ORDER BY Name;
                ";
                cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            }

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Branch
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Code = r.IsDBNull(2) ? "" : r.GetString(2),
                    IsActive = r.GetInt32(3) == 1
                });
            }

            return list;
        }

        // ================= INSERT =================
        public static void Insert(string name, string code, bool isActive)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                var createdAt = DateTime.Now;

                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    INSERT INTO Branch (Name, Code, IsActive, CreatedAt)
                    VALUES (@name, @code, @active, @created);
                ";

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code",
                    string.IsNullOrWhiteSpace(code) ? DBNull.Value : code);
                cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@created",
                    createdAt.ToString("yyyy-MM-dd HH:mm:ss"));

                cmd.ExecuteNonQuery();

                var idCmd = conn.CreateCommand();
                idCmd.Transaction = tx;
                idCmd.CommandText = "SELECT last_insert_rowid();";
                long branchId = (long)idCmd.ExecuteScalar();

                // 🔥 ACTIVITY LOG
                ActivityRepository.Log(conn, tx, new Activity
                {
                    EntityType = "Branch",
                    EntityId = (int)branchId,
                    EntityName = name,
                    Action = "BRANCH_CREATED",
                    AfterValue = $"Name={name}, Code={code}, Active={(isActive ? 1 : 0)}",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = (int)branchId,
                    CreatedAt = createdAt
                });

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ================= UPDATE =================
        public static void Update(int id, string name, string code, bool isActive)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();

            try
            {
                // read BEFORE values (for audit)
                var beforeCmd = conn.CreateCommand();
                beforeCmd.Transaction = tx;
                beforeCmd.CommandText = @"
                    SELECT Name, Code, IsActive
                    FROM Branch
                    WHERE Id = @id;
                ";
                beforeCmd.Parameters.AddWithValue("@id", id);

                string before = "";
                using (var r = beforeCmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        before =
                            $"Name={r.GetString(0)}, Code={r.GetString(1)}, Active={r.GetInt32(2)}";
                    }
                }

                var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
                    UPDATE Branch
                    SET Name = @name,
                        Code = @code,
                        IsActive = @active
                    WHERE Id = @id;
                ";

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@code",
                    string.IsNullOrWhiteSpace(code) ? DBNull.Value : code);
                cmd.Parameters.AddWithValue("@active", isActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@id", id);

                cmd.ExecuteNonQuery();

                // 🔥 ACTIVITY LOG
                ActivityRepository.Log(conn, tx, new Activity
                {
                    EntityType = "Branch",
                    EntityId = id,
                    EntityName = name,
                    Action = "BRANCH_UPDATED",
                    BeforeValue = before,
                    AfterValue = $"Name={name}, Code={code}, Active={(isActive ? 1 : 0)}",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = id,
                    CreatedAt = DateTime.Now
                });

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        // ================= GET DEFAULT =================
        public static Branch GetDefault()
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, Code, IsActive
                FROM Branch
                WHERE IsActive = 1
                ORDER BY Id
                LIMIT 1;
            ";

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Branch
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Code = r.IsDBNull(2) ? "" : r.GetString(2),
                IsActive = r.GetInt32(3) == 1
            };
        }

        // ================= GET ACTIVE =================
        public static List<Branch> GetActive()
        {
            var list = new List<Branch>();

            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            // Admins can see all active branches, non-admins see only their branch
            if (SessionContext.IsAdmin)
            {
                cmd.CommandText = @"
                    SELECT Id, Name, Code, IsActive
                    FROM Branch
                    WHERE IsActive = 1
                    ORDER BY Name;
                ";
            }
            else
            {
                cmd.CommandText = @"
                    SELECT Id, Name, Code, IsActive
                    FROM Branch
                    WHERE IsActive = 1 AND Id = @branchId
                    ORDER BY Name;
                ";
                cmd.Parameters.AddWithValue("@branchId", SessionContext.EffectiveBranchId);
            }

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new Branch
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Code = r.GetString(2),
                    IsActive = r.GetInt32(3) == 1
                });
            }

            return list;
        }

        // ================= GET BY ID =================
        public static Branch GetById(int id)
        {
            using var conn = DbService.GetConnection();
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, Code, IsActive
                FROM Branch
                WHERE Id = @id
                LIMIT 1;
            ";
            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return null;

            return new Branch
            {
                Id = r.GetInt32(0),
                Name = r.GetString(1),
                Code = r.IsDBNull(2) ? "" : r.GetString(2),
                IsActive = r.GetInt32(3) == 1
            };
        }
    }
}
