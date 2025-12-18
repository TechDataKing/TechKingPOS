using System;

namespace TechKingPOS.App.Services
{
    public static class ReceiptNumberService
    {
        public static string Generate()
        {
            return $"RCPT-{DateTime.Now:yyyyMMddHHmmss}";
        }
    }
}
