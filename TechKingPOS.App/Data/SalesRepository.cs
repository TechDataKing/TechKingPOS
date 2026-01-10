using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.Sqlite;
using TechKingPOS.App.Models;
using TechKingPOS.App.Services;
using TechKingPOS.App.Security;


namespace TechKingPOS.App.Data
{
        public static class SalesRepository
                {
        public static string SaveSale(
            List<SaleItem> items,
            string cashier,
            decimal subtotal,
            decimal discount,
            decimal vat,
            decimal total,
            decimal amountPaid,
            decimal totalCost = 0m,
            decimal totalProfit = 0m
            )
        {
            using var connection = DbService.GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                LoggerService.Info("💾", "SALE", "Saving sale started");

                string receiptNumber = ReceiptNumberService.Generate();
                string createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // ================= TRANSACTION HEADER =================
                var headerCmd = connection.CreateCommand();
                headerCmd.Transaction = transaction;
                headerCmd.CommandText = @"
                    INSERT INTO Transactions (
                        ReceiptNumber,
                        Cashier,
                        Total,
                        Tax,
                        Discount,
                        AmountPaid,
                        CreatedAt,
                        BranchId
                    )
                    VALUES (
                        @receipt,
                        @cashier,
                        @total,
                        @tax,
                        @discount,
                        @paid,
                        @created,
                        @branchId
                    );
                ";

                headerCmd.Parameters.AddWithValue("@receipt", receiptNumber);
                headerCmd.Parameters.AddWithValue("@cashier", cashier);
                headerCmd.Parameters.AddWithValue("@total", total);
                headerCmd.Parameters.AddWithValue("@tax", vat);
                headerCmd.Parameters.AddWithValue("@discount", discount);
                headerCmd.Parameters.AddWithValue("@paid", amountPaid);
                headerCmd.Parameters.AddWithValue("@created", createdAt);
                headerCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);
                headerCmd.ExecuteNonQuery();

                var idCmd = connection.CreateCommand();
                idCmd.Transaction = transaction;
                idCmd.CommandText = "SELECT last_insert_rowid();";

                long saleId = (long)idCmd.ExecuteScalar();
               ActivityRepository.Log(connection, transaction, new Activity
                {
                    EntityType = "Transaction",
                    EntityId = (int)saleId,
                    EntityName = receiptNumber,
                    Action = "TRANSACTION_CREATED",
                    QuantityChange = 0,
                    Reason = $"Sale started. Items={items.Count}",
                    PerformedBy = SessionContext.CurrentUserName,
                    BranchId = SessionContext.CurrentBranchId,
                    CreatedAt = DateTime.UtcNow
                });






                LoggerService.Info(
                    "🧾",
                    "SALE",
                    "Transaction header saved",
                    receiptNumber
                );

                // ================= LINE ITEMS =================
                foreach (var item in items)
                {    if (!UnitConverter.TryToBase(
                                item.UnitType,
                                item.Quantity,
                                item.UnitValue, // selling is per selected unit
                                out var baseUnit,
                                out var baseQty,
                                out var error))
                        {
                            throw new InvalidOperationException(error);
                        }

                        decimal qtyBefore = ItemRepository.GetBaseQuantity(
                                connection,
                                transaction,
                                item.ItemId
                            );
                        decimal costPrice = ItemRepository.GetMarkedPrice(
                            connection,
                            transaction,
                            item.ItemId
                        );
                            decimal costTotal = costPrice * baseQty;
                            decimal lineProfit = item.Total - costTotal;

                            totalCost += costTotal;
                            totalProfit += lineProfit;


                
                        // Deduct stock (transaction-safe)
                        ItemRepository.DeductStock(
                            connection,
                            transaction,
                            item.ItemId,
                            baseQty,
                            $"Sale {receiptNumber}",
                            cashier
                        );
                        


                    var itemCmd = connection.CreateCommand();
                    itemCmd.Transaction = transaction;
                   itemCmd.CommandText = @"
                            INSERT INTO Sales (
                                ReceiptNumber,
                                ItemId,
                                ItemName,
                                UnitType,
                                UnitValue,
                                Quantity,
                                Price,
                                Total,
                                CostPrice,
                                Profit,
                                BranchId,
                                CreatedAt
                            )
                            VALUES (
                                @receipt,
                                @itemId,
                                @name,
                                @unitType,
                                @unitValue,
                                @qty,
                                @price,
                                @total,
                                @costPrice,
                                @profit,
                                @branchId,
                                @created
                            );
                        ";

                        itemCmd.Parameters.AddWithValue("@receipt", receiptNumber);
                        itemCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                        itemCmd.Parameters.AddWithValue("@name", item.Name);
                        itemCmd.Parameters.AddWithValue("@unitType", item.UnitType);
                        itemCmd.Parameters.AddWithValue("@unitValue", item.UnitValue);
                        itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                        itemCmd.Parameters.AddWithValue("@price", item.Price);
                        itemCmd.Parameters.AddWithValue("@total", item.Total);
                        itemCmd.Parameters.AddWithValue("@costPrice", costPrice);
                        itemCmd.Parameters.AddWithValue("@profit", lineProfit);
                        itemCmd.Parameters.AddWithValue("@branchId", SessionContext.CurrentBranchId);   
                        itemCmd.Parameters.AddWithValue("@created", createdAt);

                    itemCmd.ExecuteNonQuery();
                    // ================= STOCK DEDUCTION =================

                        // Convert sold quantity to BASE units
                       

                    ActivityRepository.Log(connection, transaction, new Activity
                    {
                        EntityType = "Item",
                        EntityId = item.ItemId,
                        EntityName = item.Name,
                        Action = "ITEM_SOLD",
                        QuantityChange = -baseQty,
                        UnitType = item.UnitType,
                        UnitValue = item.UnitValue,
                        Price = item.Price,
                        AfterValue = item.Total.ToString("0.00"),
                        PerformedBy = SessionContext.CurrentUserName,
                        BranchId = SessionContext.CurrentBranchId,
                        CreatedAt = DateTime.Now
                    });



                }

                LoggerService.Info(
                    "📦",
                    "SALE",
                    "Sale items saved",
                    $"Count={items.Count}"
                );

                transaction.Commit();

                LoggerService.Info(
                    "✅",
                    "SALE",
                    "Sale committed successfully",
                    receiptNumber
                );

                return receiptNumber;
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                LoggerService.Error(
                    "❌",
                    "SALE",
                    "Sale failed — rollback executed",
                    ex
                );

                throw;
            }
        }
    }
}
