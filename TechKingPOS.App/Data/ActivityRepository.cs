using System;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;

namespace TechKingPOS.App.Data
{
    public static class ActivityRepository
    {
       public static void Log(Activity activity)
{
    using var connection = DbService.GetConnection();
    connection.Open();

    using var cmd = connection.CreateCommand();
    cmd.CommandText = @"
        INSERT INTO Activity (
            EntityType,
            EntityId,
            EntityName,
            Action,
            QuantityChange,
            UnitType,
            UnitValue,
            Price,
            BeforeValue,
            AfterValue,
            Reason,
            PerformedBy,
            BranchId,
            CreatedAt
        )
        VALUES (
            $EntityType,
            $EntityId,
            $EntityName,
            $Action,
            $QuantityChange,
            $UnitType,
            $UnitValue,
            $Price,
            $BeforeValue,
            $AfterValue,
            $Reason,
            $PerformedBy,
            $BranchId,
            $CreatedAt
        );
    ";

    cmd.Parameters.AddWithValue("$EntityType", activity.EntityType);
    cmd.Parameters.AddWithValue("$EntityId", (object?)activity.EntityId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$EntityName",(object?)activity.EntityName ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Action", activity.Action);
    cmd.Parameters.AddWithValue("$QuantityChange", activity.QuantityChange);
    cmd.Parameters.AddWithValue("$UnitType", (object?)activity.UnitType ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$UnitValue", (object?)activity.UnitValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Price", (object?)activity.Price ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$BeforeValue", (object?)activity.BeforeValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$AfterValue", (object?)activity.AfterValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Reason", (object?)activity.Reason ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$PerformedBy", activity.PerformedBy);
    cmd.Parameters.AddWithValue("$BranchId", activity.BranchId);
    cmd.Parameters.AddWithValue("$CreatedAt",
    activity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

    cmd.ExecuteNonQuery();
}
public static void Log(
    SqliteConnection connection,
    SqliteTransaction transaction,
    Activity activity)
{
    using var cmd = connection.CreateCommand();
    cmd.Transaction = transaction;

    cmd.CommandText = @"
        INSERT INTO Activity (
            EntityType,
            EntityId,
            EntityName,
            Action,
            QuantityChange,
            UnitType,
            UnitValue,
            Price,
            BeforeValue,
            AfterValue,
            Reason,
            PerformedBy,
            BranchId,
            CreatedAt
        )
        VALUES (
            $EntityType,
            $EntityId,
            $EntityName,
            $Action,
            $QuantityChange,
            $UnitType,
            $UnitValue,
            $Price,
            $BeforeValue,
            $AfterValue,
            $Reason,
            $PerformedBy,
            $BranchId,
            $CreatedAt
        );
    ";

    cmd.Parameters.AddWithValue("$EntityType", activity.EntityType);
    cmd.Parameters.AddWithValue("$EntityId", (object?)activity.EntityId ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$EntityName",(object?)activity.EntityName ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Action", activity.Action);
    cmd.Parameters.AddWithValue("$QuantityChange", activity.QuantityChange);
    cmd.Parameters.AddWithValue("$UnitType", (object?)activity.UnitType ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$UnitValue", (object?)activity.UnitValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Price", (object?)activity.Price ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$BeforeValue", (object?)activity.BeforeValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$AfterValue", (object?)activity.AfterValue ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$Reason", (object?)activity.Reason ?? DBNull.Value);
    cmd.Parameters.AddWithValue("$PerformedBy", activity.PerformedBy);
    cmd.Parameters.AddWithValue("$BranchId", activity.BranchId);
    cmd.Parameters.AddWithValue("$CreatedAt",
        activity.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));

    cmd.ExecuteNonQuery();
}


    }
}
