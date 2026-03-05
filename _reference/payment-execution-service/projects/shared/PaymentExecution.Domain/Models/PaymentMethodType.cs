using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PaymentExecution.Domain.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethodType
{
    [EnumMember(Value = "card")]
    Card,
    [EnumMember(Value = "link")]
    Link,
    [EnumMember(Value = "customer_balance")]
    CustomerBalance,
    [EnumMember(Value = "afterpay_clearpay")]
    AfterPay,
    [EnumMember(Value = "au_becs_debit")]
    AuBecsDebit,
    [EnumMember(Value = "bacs_debit")]
    BacsDebit,
    [EnumMember(Value = "klarna")]
    Klarna,
    [EnumMember(Value = "us_bank_account")]
    UsBankAccount,
    [EnumMember(Value = "zip")]
    Zip,
    [EnumMember(Value = "payto")]
    PayTo,
    [EnumMember(Value = "pay_by_bank")]
    PayByBank,
    [EnumMember(Value = "apple_pay")]
    ApplePay,
    [EnumMember(Value = "google_pay")]
    GooglePay
}
