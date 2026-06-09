namespace wallet.Models.Responses
{
    public class BankStatementResponse
    {
        public BankStatementAccount Account { get; set; } = new();
        public BankStatementPeriod Period { get; set; } = new();
        public BankStatementSummary Summary { get; set; } = new();
        public IEnumerable<BankStatementEntry> Entries { get; set; } = [];
    }

    public class BankStatementAccount
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
    }

    public class BankStatementPeriod
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public DateTime GeneratedAt { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalEntries { get; set; }
    }

    public class BankStatementSummary
    {
        public decimal OpeningBalance { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal ClosingBalance { get; set; }      
    }

    public class BankStatementEntry
    {
        public DateTime CreatedAt { get; set; }
        public string TransactionType { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public decimal BeforeBalance { get; set; }
        public decimal AfterBalance { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
    }
}
