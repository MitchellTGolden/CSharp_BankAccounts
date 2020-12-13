using System;
using System.ComponentModel.DataAnnotations;

namespace BankAccounts.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }

        [Required]
        [Display(Name = "Deposit/Withdrawl Amount:")]
        public decimal Amount { get; set; }

        public int UserId {get; set;}
        public User AccountHolder {get; set;}
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}