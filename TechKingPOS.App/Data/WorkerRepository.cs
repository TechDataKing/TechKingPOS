using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using BCrypt.Net;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;

namespace TechKingPOS.App.Data
{
    public static class WorkerRepository
    {
        // ================= LOAD =================
        public static List<WorkerView> GetAll()
        {
            var list = new List<WorkerView>();

            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, NationalId, Phone, Email, IsActive, Role, BranchId
                    FROM Workers
                    WHERE BranchId = @branch
                    ORDER BY Name;

            ";
            cmd.Parameters.AddWithValue("@branch", SessionContext.CurrentBranchId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new WorkerView
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    NationalId = reader.GetString(2),
                    Phone = reader.GetString(3),
                    Email = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    Role = reader.GetString(6),
                    BranchId = reader.GetInt32(7),
                    Status = reader.GetInt32(5) == 1 ? "Active" : "Inactive"
                });
            }

            return list;
        }

        // ================= CREATE =================
public static void Insert(
    string name,
    string nationalId,
    string phone,
    string email,
    UserRole role,
    int branchId)
{
    using var conn = new SqliteConnection(DbService.ConnectionString);
    conn.Open();

    string defaultPasswordHash = PasswordService.Hash("Pass.123");

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Workers
        (Name, NationalId, Phone, Email, PasswordHash, Role, BranchId, IsActive, MustChangePassword, CreatedAt)
        VALUES
        ($name, $nid, $phone, $email, $password, $role, $branchId, 0, 1, $created);
    ";

    cmd.Parameters.AddWithValue("$name", name);
    cmd.Parameters.AddWithValue("$nid", nationalId);
    cmd.Parameters.AddWithValue("$phone", phone);
    cmd.Parameters.AddWithValue("$email", email);
    cmd.Parameters.AddWithValue("$password", defaultPasswordHash);
    cmd.Parameters.AddWithValue("$role", (int)role);
    cmd.Parameters.AddWithValue("$branchId", branchId);
    cmd.Parameters.AddWithValue("$created",
    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

    cmd.ExecuteNonQuery();

    var idCmd = conn.CreateCommand();
    idCmd.CommandText = "SELECT last_insert_rowid();";
    long workerId = (long)idCmd.ExecuteScalar();

        ActivityRepository.Log(new Activity
        {
            EntityType = "Worker",
            EntityId = (int)workerId,
            EntityName = name,
            Action = "WORKER_CREATED",
            AfterValue = $"Role={role}, DefaultPassword=Pass.123",
            PerformedBy = SessionContext.CurrentUserName,
            BranchId = SessionContext.CurrentBranchId,
            CreatedAt = DateTime.Now
        });
}
        // ================= STATUS =================
     
public static Worker FindByEmailOrId(string value)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
    SELECT Id, Name, NationalId, Phone, Email,
        PasswordHash, IsActive, Role, MustChangePassword, BranchId
    FROM Workers
    WHERE IsActive = 1
    AND (
            TRIM(Email) = TRIM(@v) COLLATE NOCASE
        OR TRIM(NationalId) = TRIM(@v)
    )
    LIMIT 1;
    ";
    cmd.Parameters.AddWithValue("@v", value.Trim());


    using var r = cmd.ExecuteReader();

    if (!r.Read())
    {
        // MessageBox.Show(
        //     $"DB DEBUG:\n" +
        //     $"NO ROW RETURNED\n\n" +
        //     $"Search value: [{value}]\n" +
        //     $"Length: {value.Length}"
        // );
        return null;
    }

    // // 👇 THIS IS THE MOST IMPORTANT DEBUG
    // MessageBox.Show(
    //     $"DB DEBUG: ROW FOUND\n\n" +
    //     $"Id: {r.GetInt32(0)}\n" +
    //     $"Name: {r.GetString(1)}\n" +
    //     $"NationalId: {r.GetString(2)}\n" +
    //     $"Phone: {r.GetString(3)}\n" +
    //     $"Email: {(r.IsDBNull(4) ? "NULL" : r.GetString(4))}\n" +
    //     $"IsActive: {r.GetInt32(6)}\n" +
    //     $"Role(raw): {r.GetInt32(7)}\n" +
    //     $"MustChangePassword: {r.GetInt32(8)}\n\n" +
    //     $"PasswordHash:\n{r.GetString(5)}"
    // );

    return new Worker
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        NationalId = r.GetString(2),
        Phone = r.GetString(3),
        Email = r.IsDBNull(4) ? null : r.GetString(4),
        PasswordHash = r.GetString(5),
        IsActive = r.GetInt32(6),
        Role = (UserRole)r.GetInt32(7),
        MustChangePassword = r.GetInt32(8),
        BranchId = r.GetInt32(9)
    };
}



public static void ResetAllPasswords()
{
    using var conn = DbService.GetConnection();
    conn.Open();

    string hash = PasswordService.Hash("Pass.123");

    var cmd = conn.CreateCommand();
    cmd.CommandText = "UPDATE Workers SET PasswordHash = @h";
    cmd.Parameters.AddWithValue("@h", hash);
    cmd.ExecuteNonQuery();
}
// ================= UPDATE PROFILE =================
public static void UpdateProfile(int id, string name, string phone, string email)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET Name = @name,
            Phone = @phone,
            Email = @email
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@name", name);
    cmd.Parameters.AddWithValue("@phone", phone);
    cmd.Parameters.AddWithValue("@email", email);
    cmd.Parameters.AddWithValue("@id", id);

    cmd.ExecuteNonQuery();
     ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "PROFILE_UPDATED",
        AfterValue = $"Name={name}, Phone={phone}, Email={email}",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now
    });
}

// ================= CHANGE PASSWORD =================
public static void ChangePassword(int id, string passwordHash)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET PasswordHash = @hash,
            MustChangePassword = 0
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@hash", passwordHash);
    cmd.Parameters.AddWithValue("@id", id);

    cmd.ExecuteNonQuery();
       ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "PASSWORD_CHANGED",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now
    });
}
public static void SetMustChangePassword(int id, bool mustChange)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET MustChangePassword = @m
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@m", mustChange ? 1 : 0);
    cmd.Parameters.AddWithValue("@id", id);

    cmd.ExecuteNonQuery();
}

// ================= ACTIVATE/DEACTIVATE =================
public static void ActivateWorker(int id)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET IsActive = 1
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@id", id);
    cmd.ExecuteNonQuery();
    ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "WORKER_ACTIVATED",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now
    });
}
public static void ResetPassword(int id)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET PasswordHash = @hash,
            MustChangePassword = 1,
            IsActive = 1
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@hash",
        PasswordService.Hash("Pass.123"));
    cmd.Parameters.AddWithValue("@id", id);

    cmd.ExecuteNonQuery();
    ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "PASSWORD_RESET",
        AfterValue = "DefaultPassword=Pass.123",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now
    });
}
public static void DeactivateWorker(int id)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        UPDATE Workers
        SET IsActive = 0
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@id", id);
    cmd.ExecuteNonQuery();

    ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "WORKER_DEACTIVATED",
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now

    });
}



public static void UpdateProfileByAdmin(
    int id,
    Dictionary<string, object> changes)
{
    if (changes.Count == 0)
        return;

    using var conn = DbService.GetConnection();
    conn.Open();

    var setParts = new List<string>();
    var cmd = conn.CreateCommand();

    foreach (var c in changes)
    {
        setParts.Add($"{c.Key} = @{c.Key}");
        cmd.Parameters.AddWithValue($"@{c.Key}", c.Value);
    }

    // always deactivate on admin edit
    setParts.Add("IsActive = 0");

    cmd.CommandText = $@"
        UPDATE Workers
        SET {string.Join(", ", setParts)}
        WHERE Id = @id;
    ";

    cmd.Parameters.AddWithValue("@id", id);
    cmd.ExecuteNonQuery();

    ActivityRepository.Log(new Activity
    {
        EntityType = "Worker",
        EntityId = id,
        Action = "WORKER_UPDATED_BY_ADMIN",
        AfterValue = string.Join(", ", changes.Select(c => $"{c.Key}={c.Value}")),
        PerformedBy = SessionContext.CurrentUserName,
        BranchId = SessionContext.CurrentBranchId,
        CreatedAt = DateTime.Now
    });
}

    }
}
