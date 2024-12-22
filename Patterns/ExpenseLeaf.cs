using ExpenseTracker.Models;

namespace ExpenseTracker.Patterns
{
    public class ExpenseLeaf : ExpenseComponent
    {
        private readonly Expense _expense;

        public ExpenseLeaf(Expense expense, string name) : base(name)
        {
            _expense = expense;
        }

        public override double GetTotal()
        {
            return _expense.Amount;
        }

        public override void Display(int depth = 0)
        {
            Console.WriteLine($"{new string('-', depth)} {Name}: {_expense.Amount:C2}");
        }
    }
}
