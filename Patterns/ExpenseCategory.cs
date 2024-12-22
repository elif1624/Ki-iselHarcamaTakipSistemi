namespace ExpenseTracker.Patterns
{
    public class ExpenseCategory : ExpenseComponent
    {
        private readonly List<ExpenseComponent> _expenses = new();

        public ExpenseCategory(string name) : base(name)
        {
        }

        public void Add(ExpenseComponent component)
        {
            _expenses.Add(component);
        }

        public void Remove(ExpenseComponent component)
        {
            _expenses.Remove(component);
        }

        public override double GetTotal()
        {
            return _expenses.Sum(x => x.GetTotal());
        }

        public override void Display(int depth = 0)
        {
            Console.WriteLine($"{new string('-', depth)} {Name} (Toplam: {GetTotal():C2})");
            foreach (var expense in _expenses)
            {
                expense.Display(depth + 2);
            }
        }
    }
}
