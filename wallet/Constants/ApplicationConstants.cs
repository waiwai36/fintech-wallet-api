namespace wallet.Constants
{
    public static class TransactionConstants
    {
        // Transaction Statuses
        public static class Status
        {
            public const string Pending = "Pending";
            public const string Success = "Success";
            public const string Failed = "Failed";
            public const string Expired = "Expired";
        }

        // Transaction Types
        public static class Type
        {
            public const string Deposit = "Deposit";
            public const string Withdraw = "Withdraw";        
            public const string TransferIn = "TransferIn";
            public const string TransferOut = "TransferOut";
            public const string Adjustment = "Adjustment";
        }

        // Payment Methods
        public static class PaymentMethod
        {
            public const string ManualBankTransfer = "ManualBankTransfer";
            public const string AdminAdjustment = "AdminAdjustment";
            public const string KPay = "KPay";
            public const string WaveMoney = "WaveMoney";
            public const string CBPay = "CBPay";
            public const string Card = "Card";
        }
    }

    public static class WalletConstants
    {
        // Wallet Statuses
        public static class Status
        {
            public const string Active = "Active";
            public const string Blocked = "Blocked";
            public const string Suspended = "Suspended";
        }
    }

    public enum UserRole
    {
        Admin = 1,       
        User = 2,        
        Merchant = 3   
    }
}

