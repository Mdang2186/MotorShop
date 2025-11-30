namespace MotorShop.Utilities
{
    public class PaymentSettings
    {
        // Mã ngân hàng theo VietQR (ví dụ: "bidv", "vcb"…)
        public string BankCode { get; set; } = string.Empty;

        // Số tài khoản nhận tiền của MotorShop
        public string AccountNumber { get; set; } = string.Empty;

        public string AccountName { get; set; } = string.Empty;
    }
}
