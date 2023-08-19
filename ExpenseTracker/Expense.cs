using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpenseTracker
{
    public class Expense
    {
        public DateTime Date { get; set; }
        public string FullName { get; set; }
        public decimal Amount { get; set; }
        public Dictionary<string, decimal> Categories { get; set; }

        public string ParsedFrom { get; set; }

        public Expense(string date, string fullName, decimal amount, string parsedFrom)
        {
            this.Date = FormatDate(date);
            this.FullName = fullName;
            this.Amount = amount;
            this.ParsedFrom = parsedFrom;
            this.Categories = new Dictionary<string, decimal>();
        }

        private DateTime FormatDate(string date)
        {
            if (date.Count(c => c == '/') == 1)
            {
                date += "/23";
            }

            return DateTime.Parse(date);
        }
    }
}
