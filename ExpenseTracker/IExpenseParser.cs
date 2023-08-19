using System.Collections.Generic;

namespace ExpenseTracker.Parsing
{
    interface IExpenseParser
    {
        bool CanParse(List<List<string>> lines);
        List<Expense> ParseExpenses(List<List<string>> lines);
    }
}
