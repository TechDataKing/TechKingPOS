using System;
using Microsoft.Data.Sqlite;
using System.Windows;

using TechKingPOS.App.Data;

namespace TechKingPOS.App.Services
{
    public static class BranchService
    {
        public static void Load()
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
            
            if (!r.Read())
                {
                    MessageBox.Show(
                        "No branch configured. Please restart the application.",
                        "Initialization Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    Application.Current.Shutdown();
                    return;
                }
            BranchContext.Id   = r.GetInt32(0);
            BranchContext.Name = r.GetString(1);
            BranchContext.Code = r.GetString(2);
            BranchContext.IsActive = r.GetInt32(3) == 1;
        }
    }
}
