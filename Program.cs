using System;
using System.IO;
using RustEditProCrack.Core;

namespace RustEditProCrack
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🚀 RustEdit Pro Crack - Автономный DLL Патчер");
            Console.WriteLine("================================================");

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string dllPath = args[0];
            
            if (string.IsNullOrEmpty(dllPath))
            {
                Console.WriteLine("❌ Не указан путь к DLL файлу");
                ShowUsage();
                return;
            }

            RunPatching(dllPath);
        }

        static void RunPatching(string dllPath)
        {
            Console.WriteLine($"📂 Использую указанный путь: {dllPath}");

            if (!File.Exists(dllPath))
            {
                Console.WriteLine($"❌ Файл не найден: {dllPath}");
                return;
            }

            using (var manager = new DllManager())
            {
                Console.WriteLine($"📂 Загружаю сборку: {dllPath}");
                if (!manager.LoadAssembly(dllPath))
                {
                    Console.WriteLine("❌ Не удалось загрузить сборку");
                    return;
                }

                if (manager.ApplyAllPatches())
                {
                    Console.WriteLine("\n💾 Сохраняю пропатченную сборку...");
                    
                    var directory = Path.GetDirectoryName(dllPath);
                    var filename = Path.GetFileNameWithoutExtension(dllPath);
                    var extension = Path.GetExtension(dllPath);
                    var outputPath = Path.Combine(directory, $"{filename}-Patched{extension}");
                    
                    if (manager.SavePatched(outputPath))
                    {
                        Console.WriteLine("✅ Патчинг завершен успешно!");
                        Console.WriteLine($"📁 Результат: {outputPath}");
                        Console.WriteLine("\n🎉 Готово! Замените оригинальный файл на пропатченный.");
                    }
                    else
                    {
                        Console.WriteLine("❌ Ошибка сохранения пропатченного файла");
                    }
                }
                else
                {
                    Console.WriteLine("❌ Патчинг завершился с ошибками");
                }
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("=== RustEdit Pro Crack Tool ===");
            Console.WriteLine("Использование:");
            Console.WriteLine("  dotnet run <dll-path>");
            Console.WriteLine();
            Console.WriteLine("Пример:");
            Console.WriteLine("  dotnet run Assembly-CSharp.dll");
        }
    }
} 