namespace TransactionAPI.Model
{
    public class Transaction
    {
        public string? CreditCardNumber { get; set; }
        public string? CreditCardType { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? TransactionId { get; set; }
    }
}
