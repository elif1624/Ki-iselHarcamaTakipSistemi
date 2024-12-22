using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Database
{
    public sealed class DatabaseConnection
    {
        private static DatabaseConnection? instance;
        private static readonly object padlock = new object();
        private readonly SqliteConnection connection;
        private const string DbFile = "expenses.db";

        private DatabaseConnection()
        {
            connection = new SqliteConnection($"Data Source={DbFile}");
            connection.Open();
            CreateTables();
        }

        public static DatabaseConnection Instance
        {
            get
            {
                lock (padlock)
                {
                    instance ??= new DatabaseConnection();
                    return instance;
                }
            }
        }

        private void CreateTables()
        {
            string createCategoriesTable = @"
                CREATE TABLE IF NOT EXISTS kategoriler (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ad TEXT NOT NULL
                )";

            string createExpensesTable = @"
                CREATE TABLE IF NOT EXISTS harcamalar (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    kategori_id INTEGER NOT NULL,
                    aciklama TEXT,
                    tutar REAL NOT NULL,
                    tarih TEXT NOT NULL,
                    FOREIGN KEY (kategori_id) REFERENCES kategoriler (id)
                )";

            string createTransactionsTable = @"
                CREATE TABLE IF NOT EXISTS islemler (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    islem_tipi TEXT NOT NULL,
                    harcama_id INTEGER,
                    tarih TEXT NOT NULL,
                    FOREIGN KEY (harcama_id) REFERENCES harcamalar (id)
                )";

            using var command = connection.CreateCommand();
            command.CommandText = createCategoriesTable;
            command.ExecuteNonQuery();

            command.CommandText = createExpensesTable;
            command.ExecuteNonQuery();

            command.CommandText = createTransactionsTable;
            command.ExecuteNonQuery();
        }

        public SqliteConnection GetConnection()
        {
            return connection;
        }
    }
}