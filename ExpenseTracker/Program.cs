using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ExpenseTracker.Parsing;

namespace ExpenseTracker {
    public class Program {
        private readonly SaveData data;
        public Program()
        {
            data = SaveData.LoadFromSave();
            
            LoadExpenseFiles();
            data.Save();
        }

        private void SaveCsv()
        {
            foreach (Expense expense in data.ParsedExpenses)
            {
                // Fix misaligned dates
                if (expense.Date < DateTime.Parse("01-01-2023"))
                {
                    expense.Date -= TimeSpan.FromDays(365);
                }

                if (expense.ParsedFrom.Equals("UWExpenseParser") || expense.ParsedFrom.Equals("FTExpenseParser"))
                {
                    // Invert amounts
                    expense.Amount = -expense.Amount;
                    foreach (string key in expense.Categories.Keys)
                    {
                        expense.Categories[key] = -expense.Categories[key];
                    }
                }
            }

            List<string> lines = new List<string>();

            Dictionary<int, Dictionary<string, decimal>> monthlySummary = new Dictionary<int, Dictionary<string, decimal>>();
            foreach (Expense expense in data.ParsedExpenses.OrderBy(e => e.Date))
            {
                foreach (var category in expense.Categories)
                {
                    lines.Add($"{expense.Date.ToShortDateString()},{expense.FullName.Replace(",", "")}," +
                        $"{category.Key},{category.Value}");

                    int month = expense.Date.Month;
                    if (month == 12)
                    {
                        month = 0;
                    }
                    if (!monthlySummary.ContainsKey(month))
                    {
                        monthlySummary.Add(month, new Dictionary<string, decimal>());
                    }

                    if (!monthlySummary[month].ContainsKey(category.Key))
                    {
                        monthlySummary[month].Add(category.Key, 0);
                    }

                    monthlySummary[month][category.Key] += category.Value;
                }
            }

            List<string> summaryLines = new List<string>();
            bool first = true;
            foreach (var month in monthlySummary.OrderBy(m => m.Key))
            {
                if (first)
                {
                    summaryLines.Add($"Month,{string.Join(",", data.Categories.OrderBy(c => c))}");
                    first = false;
                }

                string summaryLine = $"{month.Key},";
                foreach (string category in data.Categories.OrderBy(c => c))
                {
                    if (month.Value.ContainsKey(category))
                    {
                        summaryLine += $"{month.Value[category]},";
                    }
                    else
                    {
                        summaryLine += "0,";
                    }
                }
                summaryLines.Add(summaryLine);
            }

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            File.WriteAllText($@"{desktopPath}\exp.csv", string.Join(Environment.NewLine, lines));
            File.WriteAllText($@"{desktopPath}\monthly.csv", string.Join(Environment.NewLine, summaryLines));
        }

        private void LoadExpenseFiles()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            List<IExpenseParser> baParsers = new List<IExpenseParser>()
            {
                new BAExpenseParser(),
            };
            ParseWithParsers("BankOfAmerica", $@"{desktopPath}\spending-analyze\ba", data.Expenses, baParsers);

            List<IExpenseParser> ftParsers = new List<IExpenseParser>()
            {
                new FTCCExpenseParser(),
                new FTExpenseParser(),
            };
            ParseWithParsers("FirstTech", $@"{desktopPath}\spending-analyze\firsttech", data.Expenses, ftParsers);
            
            List<IExpenseParser> uwParsers = new List<IExpenseParser>()
            {
                new UWExpenseParser(),
                new UWCCExpenseParser(),
            };
            ParseWithParsers("UWCU", $@"{desktopPath}\spending-analyze\uwcu", data.Expenses, uwParsers);

            List<IExpenseParser> chaseParsers = new List<IExpenseParser>()
            {
                new ChaseExpenseParser(),
            };
            ParseWithParsers("Chase", $@"{desktopPath}\spending-analyze\chase", data.Expenses, chaseParsers);
        }

        private void Parse()
        {
            while (data.Expenses.Any())
            {
                Console.WriteLine($"{data.Expenses.Count} remaining, {data.ParsedExpenses.Count} parsed.");

                // Process an unprocessed, uncategorized expense via the dialog
                Expense lastExpense = data.Expenses[data.Expenses.Count - 1];
                CategoryAssigner assigner = new CategoryAssigner(lastExpense, data.Categories);
                bool? dialogResult = assigner.ShowDialog();
                if (!dialogResult.HasValue || !dialogResult.Value)
                {
                    // Exit if the dialog closed prematurely.
                    return;
                }

                if (assigner.customSplitCheckBox.IsChecked.Value)
                {
                    // Custom assignment of the expense into multiple categories
                    foreach (AssignedCategoryItem categoryAssignment in assigner.assignedCategoriesListBox.Items)
                    {
                        Console.WriteLine($"    Assigning {categoryAssignment.Category} to {lastExpense.FullName} ({categoryAssignment.Amount})");
                        lastExpense.Categories.Add(categoryAssignment.Category, categoryAssignment.Amount);
                    }

                    data.ParsedExpenses.Add(lastExpense);
                    data.Expenses.Remove(lastExpense);
                }
                else
                {
                    // Has an expense regex, scan all the expenses for it.
                    // This will effectively process the current *lastExpense* item too.
                    string assignedCategory = assigner.GetSingleAssignedCategory();
                    string expenseRegex = assigner.expenseRegex.Text;

                    data.ExpenseToCategoryRegexes.Add(expenseRegex, assignedCategory);

                    List<Expense> matchingExpenses = data.Expenses.Where(
                        expense => Regex.IsMatch(expense.FullName, expenseRegex)).ToList();
                    Console.WriteLine($"Found {matchingExpenses.Count} entries for {expenseRegex}");
                    foreach (Expense expense in matchingExpenses)
                    {
                        Console.WriteLine($"  Assigning {assignedCategory} to {expense.FullName} ({expense.Amount})");
                        expense.Categories.Add(assignedCategory, expense.Amount);

                        data.ParsedExpenses.Add(expense);
                        data.Expenses.Remove(expense);
                    }
                }

                data.Save();
            }
        }

        void ParseWithParsers(string folderType, string folder, List<Expense> expenses, List<IExpenseParser> parsers)
        {
            Console.WriteLine($"Parsing {folderType} statements...");
            foreach (string file in Directory.GetFiles(folder))
            {
                string shortFileName = Path.GetFileNameWithoutExtension(file);
                if (data.ParsedFiles.Contains(shortFileName))
                {
                    Console.WriteLine($"  Ignoring {shortFileName} (already parsed)");
                    continue;
                }

                List<List<string>> lines = PdfParser.GetDocumentLines(file);

                bool parsed = false;
                foreach (IExpenseParser parser in parsers)
                {
                    if (parser.CanParse(lines) && !parsed)
                    {
                        expenses.AddRange(parser.ParseExpenses(lines));
                        data.ParsedFiles.Add(shortFileName);

                        Console.WriteLine($"  Parsed {shortFileName} ({parser.GetType().ToString().Split(".").Last()})");
                        parsed = true;
                    }
                }

                if (!parsed)
                {
                    Console.WriteLine($"Could not parse {shortFileName}!");
                }
            }
        }

        [STAThread]
        static void Main(string[] args) {
            var program = new Program();
            program.Parse();
            program.SaveCsv();
            Console.WriteLine("Done");
        }
    }
}
