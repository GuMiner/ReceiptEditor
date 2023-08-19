using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExpenseTracker
{
    public class AssignedCategoryItem
    {
        public string Category { get; set; }

        public decimal Amount { get; set; }

        public override string ToString()
            => $"{Amount}: {Category}";
    }

    /// <summary>
    /// Interaction logic for CategoryAssigner.xaml
    /// </summary>
    public partial class CategoryAssigner : Window
    {
        private readonly HashSet<string> categories;
        private decimal expenseAmountRemaining;

        public CategoryAssigner(Expense expense, HashSet<string> categories)
        {
            InitializeComponent();

            this.categories = categories;
            this.expenseNameTextBox.Text = expense.FullName;
            this.expenseAmountTextBox.Text = expense.Amount.ToString();
            this.Title = $"{this.Title} ({expense.Date.ToShortDateString()})";

            UpdateCurrentCategories();

            expenseAmountRemaining = expense.Amount;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.customSplitCheckBox.IsChecked.Value)
            {
                if (expenseAmountRemaining != 0)
                {
                    ShowErrorBox("Must assign the whole expense amount for split expenses");
                }
                else
                {
                    this.DialogResult = true;
                    this.Hide();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(this.expenseRegex.Text) ||
                    !Regex.IsMatch(this.expenseNameTextBox.Text, this.expenseRegex.Text))
                {
                    ShowErrorBox("There must be an expense regex set!");
                }
                else if (
                    this.assignedCategoriesListBox.Items.Count == 0 ||
                    this.assignedCategoriesListBox.Items.Count > 1)
                {
                    ShowErrorBox("There must be only one assigned category");
                }
                else
                {
                    this.DialogResult = true;
                    this.Hide();
                }
            }
        }

        public static void ShowErrorBox(string text)
            => MessageBox.Show(text, "Category Assignment Error", MessageBoxButton.OK, MessageBoxImage.Error);

        private void assignCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.currentCategories.SelectedItem == null)
            {
                ShowErrorBox("No category selected");
            }
            else if (this.customSplitCheckBox.IsChecked.Value)
            {
                if (!string.IsNullOrWhiteSpace(this.categoryAmountTextBox.Text) &&
                    decimal.TryParse(this.categoryAmountTextBox.Text, out decimal splitAmount))
                {
                    if (splitAmount > expenseAmountRemaining || splitAmount == 0)
                    {
                        ShowErrorBox($"Must specify an expense amount less than the amount remaining ({expenseAmountRemaining}) and more than 0");
                    }
                    else
                    {
                        this.assignedCategoriesListBox.Items.Add(new AssignedCategoryItem()
                        {
                            Category = currentCategories.SelectedItem as string,
                            Amount = splitAmount
                        });

                        expenseAmountRemaining -= splitAmount;
                        this.expenseAmountTextBox.Text = expenseAmountRemaining.ToString(); // TODO observables?
                    }
                }
                else
                {
                    ShowErrorBox("Must specify a value for the split amount");
                }
            }
            else if (this.assignedCategoriesListBox.Items.Count == 1)
            {
                ShowErrorBox("Cannot add another category unless this is a Custom Split item");
            }
            else
            {
                this.assignedCategoriesListBox.Items.Add(new AssignedCategoryItem()
                {
                    Category = currentCategories.SelectedItem as string,
                    Amount = expenseAmountRemaining
                });
            }
        }

        private void addCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.categorySearch.Text))
            {
                ShowErrorBox("Must specify a category in the search box to add it!");
            }
            else
            {
                categories.Add(categorySearch.Text);
                UpdateCurrentCategories();
            }
        }

        internal string GetSingleAssignedCategory()
            => (this.assignedCategoriesListBox.Items[0] as AssignedCategoryItem).Category;

        private void categorySearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCurrentCategories();
        }

        private void UpdateCurrentCategories()
        {
            this.currentCategories.Items.Clear();
            List<string> categorySearchResults = string.IsNullOrWhiteSpace(this.categorySearch.Text) ?
                this.categories.ToList() :
                categories.Where(category => category.Contains(this.categorySearch.Text)).ToList();

            foreach (string category in categorySearchResults.OrderBy(c => c))
            {
                this.currentCategories.Items.Add(category);
            }
        }

        private void maxButton_Click(object sender, RoutedEventArgs e)
        {
            this.categoryAmountTextBox.Text = expenseAmountRemaining.ToString();
        }
    }
}
