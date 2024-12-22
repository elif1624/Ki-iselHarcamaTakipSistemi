using ExpenseTracker.Models;
using ExpenseTracker.Database;
using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Commands
{
    /*
     * Command tasarım kalıbının ConcreteCommand rolünü üstlenen sınıf:
     * 1. Yeni bir harcama ekleme işlemini gerçekleştirir
     * 2. Execute() metodunda:
     *    - Harcama veritabanına eklenir
     *    - İşlem kaydı tutulur
     *    - Eklenen harcamanın ID'si saklanır (geri alma için)
     * 3. Undo() metodunda:
     *    - Eklenen harcama silinir
     *    - İlgili işlem kaydı silinir
     * 
     * Bu sınıf sayesinde:
     * - Harcama ekleme işlemi kapsüllenir
     * - İşlem geri alınabilir
     * - Veritabanı işlemleri ve işlem kaydı bir arada yönetilir
     */
    public class AddExpenseCommand : ICommand
    {
        private readonly Expense _expense;
        private int _lastInsertedId;

        public AddExpenseCommand(Expense expense)
        {
            _expense = expense;
        }

        public void Execute()
        {
            var connection = DatabaseConnection.Instance.GetConnection();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT INTO harcamalar (kategori_id, aciklama, tutar, tarih)
                    VALUES (@categoryId, @description, @amount, @date);
                    SELECT last_insert_rowid();";

                command.Parameters.AddWithValue("@categoryId", _expense.CategoryId);
                command.Parameters.AddWithValue("@description", _expense.Description);
                command.Parameters.AddWithValue("@amount", _expense.Amount);
                command.Parameters.AddWithValue("@date", _expense.Date);

                _lastInsertedId = Convert.ToInt32(command.ExecuteScalar());

                // İşlem kaydı ekleme
                command.CommandText = @"
                    INSERT INTO islemler (islem_tipi, harcama_id, tarih)
                    VALUES ('Ekle', @expenseId, @transactionDate)";

                command.Parameters.AddWithValue("@expenseId", _lastInsertedId);
                command.Parameters.AddWithValue("@transactionDate", DateTime.Now.ToString("yyyy-MM-dd"));

                command.ExecuteNonQuery();
            }
        }

        public void Undo()
        {
            var connection = DatabaseConnection.Instance.GetConnection();
            using (var command = connection.CreateCommand())
            {
                // İşlem kaydını silme
                command.CommandText = "DELETE FROM islemler WHERE harcama_id = @expenseId";
                command.Parameters.AddWithValue("@expenseId", _lastInsertedId);
                command.ExecuteNonQuery();

                // Harcamayı silme
                command.CommandText = "DELETE FROM harcamalar WHERE id = @expenseId";
                command.ExecuteNonQuery();
            }
        }
    }
}
