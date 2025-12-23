using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using BCrypt.Net;
using TechKingPOS.App.Services;

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
                SELECT Id, Name, NationalId, Phone, Email, IsActive
                FROM Workers
                ORDER BY Name;
            ";

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
                    Status = reader.GetInt32(5) == 1 ? "Active" : "Inactive"
                });
            }

            return list;
        }

        // ================= CREATE =================
       public static void Insert(string name, string nationalId, string phone, string email)
{
    using var conn = new SqliteConnection(DbService.ConnectionString);
    conn.Open();

    // DEFAULT password hash (Pass.123)
string defaultPasswordHash = PasswordService.Hash("Pass.123");

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Workers 
        (Name, NationalId, Phone, Email, PasswordHash, CreatedAt)
        VALUES 
        ($name, $nid, $phone, $email, $password, $created);
    ";

    cmd.Parameters.AddWithValue("$name", name);
    cmd.Parameters.AddWithValue("$nid", nationalId);
    cmd.Parameters.AddWithValue("$phone", phone);
    cmd.Parameters.AddWithValue("$email", email);
    cmd.Parameters.AddWithValue("$password", defaultPasswordHash);
    cmd.Parameters.AddWithValue("$created", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

    cmd.ExecuteNonQuery();
}



        // ================= STATUS =================
        public static void SetActive(int id, bool active)
        {
            using var conn = new SqliteConnection(DbService.ConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE Workers
                SET IsActive = $active
                WHERE Id = $id;
            ";

            cmd.Parameters.AddWithValue("$active", active ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", id);

            cmd.ExecuteNonQuery();
        }
        public static Worker FindByEmailOrId(string value)
{
    using var conn = DbService.GetConnection();
    conn.Open();

    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT Id, Name, NationalId, Phone, Email, PasswordHash, IsActive
        FROM Workers
        WHERE Email = @v OR NationalId = @v
        LIMIT 1;
    ";

    cmd.Parameters.AddWithValue("@v", value);

    using var r = cmd.ExecuteReader();
    if (!r.Read()) return null;

    return new Worker
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        NationalId = r.GetString(2),
        Phone = r.GetString(3),
        Email = r.IsDBNull(4) ? null : r.GetString(4),
        PasswordHash = r.GetString(5),
        IsActive = r.GetInt32(6)
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


    }
}
