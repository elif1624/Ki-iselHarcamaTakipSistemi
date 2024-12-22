using ExpenseTracker.Models;
using ExpenseTracker.Database;
using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Commands
{
    /*
     * Command tasarım kalıbının ConcreteCommand rolünü üstlenen sınıf:
     * 1. Var olan bir harcamayı silme işlemini gerçekleştirir
     * 2. Execute() metodunda:
     *    - Silinecek harcamanın bilgileri saklanır (geri alma için)
     *    - İşlem kaydı tutulur
     *    - Harcama veritabanından silinir
     * 3. Undo() metodunda:
     *    - Silinen harcama geri yüklenir
     *    - Silme işleminin kaydı silinir
     * 
     * Bu sınıf sayesinde:
     * - Harcama silme işlemi kapsüllenir
     * - Silinen harcamalar geri getirilebilir
     * - Veritabanı tutarlılığı korunur
     */
    public class DeleteExpenseCommand : ICommand
    {
        private readonly int _expenseId;
        private Expense? _deletedExpense;

        public DeleteExpenseCommand(int expenseId)
        {
            _expenseId = expenseId;
        }

        public void Execute()
        {
            var connection = DatabaseConnection.Instance.GetConnection();
            using (var command = connection.CreateCommand())
            {
                // Silinecek harcamayı kaydet
                command.CommandText = "SELECT * FROM harcamalar WHERE id = @expenseId";
                command.Parameters.AddWithValue("@expenseId", _expenseId);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        _deletedExpense = new Expense(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetString(2),
                            reader.GetDouble(3),
                            reader.GetString(4)
                        );
                    }
                }

                if (_deletedExpense == null)
                {
                    throw new Exception("Silinecek harcama bulunamadı.");
                }

                // İşlem kaydı ekleme
                command.CommandText = @"
                    INSERT INTO islemler (islem_tipi, harcama_id, tarih)
                    VALUES ('Sil', @expenseId, @transactionDate)";

                command.Parameters.AddWithValue("@transactionDate", DateTime.Now.ToString("yyyy-MM-dd"));
                command.ExecuteNonQuery();

                // Harcamayı silme
                command.CommandText = "DELETE FROM harcamalar WHERE id = @expenseId";
                command.ExecuteNonQuery();
            }
        }

        public void Undo()
        {
            if (_deletedExpense != null)
            {
                var connection = DatabaseConnection.Instance.GetConnection();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT INTO harcamalar (id, kategori_id, aciklama, tutar, tarih)
                        VALUES (@id, @categoryId, @description, @amount, @date)";

                    command.Parameters.AddWithValue("@id", _deletedExpense.Id);
                    command.Parameters.AddWithValue("@categoryId", _deletedExpense.CategoryId);
                    command.Parameters.AddWithValue("@description", _deletedExpense.Description);
                    command.Parameters.AddWithValue("@amount", _deletedExpense.Amount);
                    command.Parameters.AddWithValue("@date", _deletedExpense.Date);

                    command.ExecuteNonQuery();

                    // İşlem kaydını silme
                    command.CommandText = "DELETE FROM islemler WHERE harcama_id = @expenseId AND islem_tipi = 'Sil'";
                    command.Parameters.AddWithValue("@expenseId", _deletedExpense.Id);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
