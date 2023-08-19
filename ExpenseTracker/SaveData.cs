using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace ExpenseTracker
{
    public class SaveData
    {
        public HashSet<string> ParsedFiles { get; set; } = new HashSet<string>();
        public List<Expense> Expenses { get; set; } = new List<Expense>();
        public List<Expense> ParsedExpenses { get; set; } = new List<Expense>();

        public HashSet<string> Categories { get; set; } = new HashSet<string>();
        public Dictionary<string, string> ExpenseToCategoryRegexes { get; set; } = new Dictionary<string, string>();

        private static string thisFolderPath([CallerFilePath] string callerFilePath = null)
            => Path.GetDirectoryName(callerFilePath);

        internal void Save()
        {
            string saveFilePath = Path.Combine(thisFolderPath(), "parsedFiles.json");
            File.WriteAllText(saveFilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static SaveData LoadFromSave()
        {
            string saveFilePath = Path.Combine(thisFolderPath(), "parsedFiles.json");
            if (File.Exists(saveFilePath))
            {
                SaveData saveData = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText(saveFilePath));
                Console.WriteLine($"Loaded save data with:");
                Console.WriteLine($"  {saveData.ParsedFiles.Count} parsed files.");
                Console.WriteLine($"  {saveData.Expenses.Count} expenses.");
                Console.WriteLine($"  {saveData.ParsedExpenses.Count} parsed expenses.");
                Console.WriteLine($"  {saveData.Categories.Count} categories.");
                Console.WriteLine($"  {saveData.ExpenseToCategoryRegexes.Count} expenseToCategoryRegexes.");
                return saveData;
            }

            return new SaveData();
        }
    }
}
