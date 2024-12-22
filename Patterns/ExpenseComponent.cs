namespace ExpenseTracker.Patterns
{
    public abstract class ExpenseComponent
    {
        protected ExpenseComponent(string name)
        {
            Name = name;
        }

        public string Name { get; protected set; }
        
        public abstract double GetTotal();
        public abstract void Display(int depth = 0);
    }
}
