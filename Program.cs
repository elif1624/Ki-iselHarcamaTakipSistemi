﻿using ExpenseTracker.Commands;
using ExpenseTracker.Database;
using ExpenseTracker.Models;
using ExpenseTracker.Patterns;
using Microsoft.Data.Sqlite;

namespace ExpenseTracker
{
    class Program
    {
        private static Stack<ICommand> _commandHistory = new Stack<ICommand>();

        static void Main(string[] args)
        {
            InitializeCategories();

            while (true)
            {
                Console.WriteLine("\nKişisel Harcama Takip Sistemi");
                Console.WriteLine("1. Yeni Harcama Ekle");
                Console.WriteLine("2. Harcama Sil");
                Console.WriteLine("3. Harcamaları Listele");
                Console.WriteLine("4. Son İşlemi Geri Al");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        AddExpense();
                        break;
                    case "2":
                        DeleteExpense();
                        break;
                    case "3":
                        ListExpenses();
                        break;
                    case "4":
                        UndoLastCommand();
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçim!");
                        break;
                }
            }
        }

        private static void InitializeCategories()
        {
            var connection = DatabaseConnection.Instance.GetConnection();
            using (var command = connection.CreateCommand())
            {
                // Örnek kategorileri ekle
                string[] categories = { "Yemek", "Ulaşım", "Eğlence", "Alışveriş", "Diğer" };
                
                foreach (var category in categories)
                {
                    command.CommandText = "INSERT OR IGNORE INTO kategoriler (ad) VALUES (@name)";
                    command.Parameters.AddWithValue("@name", category);
                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        private static void AddExpense()
        {
            Console.WriteLine("\nKategoriler:");
            var categories = GetCategories();
            foreach (var category in categories)
            {
                Console.WriteLine($"{category.Id}. {category.Name}");
            }

            Console.Write("Kategori ID: ");
            if (!int.TryParse(Console.ReadLine(), out int categoryId))
            {
                Console.WriteLine("Geçersiz kategori ID!");
                return;
            }

            Console.Write("Açıklama: ");
            string description = Console.ReadLine();

            Console.Write("Tutar: ");
            if (!double.TryParse(Console.ReadLine(), out double amount))
            {
                Console.WriteLine("Geçersiz tutar!");
                return;
            }

            var expense = new Expense(0, categoryId, description, amount, DateTime.Now.ToString("yyyy-MM-dd"));
            var command = new AddExpenseCommand(expense);
            
            try
            {
                command.Execute();
                _commandHistory.Push(command);
                Console.WriteLine("Harcama başarıyla eklendi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        private static void DeleteExpense()
        {
            Console.Write("Silinecek harcama ID: ");
            if (!int.TryParse(Console.ReadLine(), out int expenseId))
            {
                Console.WriteLine("Geçersiz ID!");
                return;
            }

            var command = new DeleteExpenseCommand(expenseId);
            
            try
            {
                command.Execute();
                _commandHistory.Push(command);
                Console.WriteLine("Harcama başarıyla silindi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }

        private static void ListExpenses()
        {
            var root = new ExpenseCategory("Tüm Harcamalar");
            var categoryExpenses = new Dictionary<int, ExpenseCategory>();

            var categories = GetCategories();
            foreach (var category in categories)
            {
                categoryExpenses[category.Id] = new ExpenseCategory(category.Name);
                root.Add(categoryExpenses[category.Id]);
            }

            var connection = DatabaseConnection.Instance.GetConnection();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT h.*, k.ad as kategori_adi 
                    FROM harcamalar h
                    JOIN kategoriler k ON h.kategori_id = k.id
                    ORDER BY h.tarih DESC";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var expense = new Expense(
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetString(2),
                            reader.GetDouble(3),
                            reader.GetString(4)
                        );

                        var leaf = new ExpenseLeaf(expense, $"{expense.Description} ({expense.Date})");
                        categoryExpenses[expense.CategoryId].Add(leaf);
                    }
                }
            }

            Console.WriteLine("\nHarcama Raporu:");
            root.Display();
        }

        private static void UndoLastCommand()
        {
            if (_commandHistory.Count > 0)
            {
                var command = _commandHistory.Pop();
                try
                {
                    command.Undo();
                    Console.WriteLine("Son işlem geri alındı.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hata: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Geri alınacak işlem yok!");
            }
        }

        private static List<Category> GetCategories()
        {
            var categories = new List<Category>();
            var connection = DatabaseConnection.Instance.GetConnection();
            
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT * FROM kategoriler";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category(
                            reader.GetInt32(0),
                            reader.GetString(1)
                        ));
                    }
                }
            }

            return categories;
        }
    }
}
