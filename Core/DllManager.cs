using Mono.Cecil;
using System;
using System.IO;
using RustEditProCrack.Unlockers;

namespace RustEditProCrack.Core
{
    /// <summary>
    /// Основной менеджер для координации всех разблокировщиков
    /// </summary>
    public class DllManager : IDisposable
    {
        private AssemblyDefinition assembly;
        private string assemblyPath;
        
        // Разблокировщики
        private ProModeUnlocker proModeUnlocker;
        private SmartPrefabUnlocker smartPrefabUnlocker;
        private PasswordProtectionRemover passwordRemover;

        public bool LoadAssembly(string path)
        {
            try
            {
                assemblyPath = path;
                
                // Создаем резолвер для Unity зависимостей
                var resolver = new DefaultAssemblyResolver();
                var managedPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(managedPath))
                {
                    resolver.AddSearchDirectory(managedPath);
                }
                
                var readerParameters = new ReaderParameters()
                {
                    AssemblyResolver = resolver,
                    ReadingMode = ReadingMode.Immediate
                };
                
                assembly = AssemblyDefinition.ReadAssembly(path, readerParameters);
                
                // Инициализация разблокировщиков
                proModeUnlocker = new ProModeUnlocker(assembly);
                smartPrefabUnlocker = new SmartPrefabUnlocker(assembly);
                passwordRemover = new PasswordProtectionRemover(assembly);
                
                Console.WriteLine($"✅ Сборка загружена: {path}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка загрузки сборки: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Применяет все доступные патчи к загруженной сборке
        /// </summary>
        public bool ApplyAllPatches()
        {
            Console.WriteLine("🔧 Применяю патчи...");
            
            try
            {
                // STEP 1: Pro Mode Unlock
                Console.WriteLine("\n🔧 STEP 1: PRO MODE UNLOCK");
                var proUnlocker = new ProModeUnlocker(assembly);
                bool proResult = proUnlocker.UnlockProMode();
                
                // STEP 2: Prefab Unlock (УМНЫЙ ПАТЧЕР)
                Console.WriteLine("\n🔧 STEP 2: PREFAB UNLOCK (УМНЫЙ)");
                var smartPrefabUnlocker = new SmartPrefabUnlocker(assembly);
                bool prefabResult = smartPrefabUnlocker.UnlockPrefabsSmartly();
                
                // STEP 3: Password Protection Removal
                Console.WriteLine("\n🔧 STEP 3: PASSWORD PROTECTION REMOVAL");
                var passwordRemover = new PasswordProtectionRemover(assembly);
                bool passwordResult = passwordRemover.RemovePasswordProtection();
                
                // Результат
                if (proResult && prefabResult && passwordResult)
                {
                    Console.WriteLine("\n🎉 ВСЕ ПАТЧИ ПРИМЕНЕНЫ УСПЕШНО!");
                    return true;
                }
                else
                {
                    Console.WriteLine("\n⚠️ НЕКОТОРЫЕ ПАТЧИ НЕ ПРИМЕНИЛИСЬ");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА при применении патчей: {ex.Message}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Сохранение пропатченной сборки
        /// </summary>
        public bool SavePatched()
        {
            if (assembly == null)
            {
                Console.WriteLine("❌ Сборка не загружена");
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(assemblyPath);
                var filename = Path.GetFileNameWithoutExtension(assemblyPath);
                var extension = Path.GetExtension(assemblyPath);
                var outputPath = Path.Combine(directory, $"{filename}_Cracked{extension}");

                // Создаем WriterParameters для корректного сохранения Unity сборок
                var writerParameters = new WriterParameters()
                {
                    WriteSymbols = false // Отключаем символы для избежания проблем с зависимостями
                };

                assembly.Write(outputPath, writerParameters);
                
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"✅ Пропатченная сборка сохранена:");
                Console.WriteLine($"   Путь: {outputPath}");
                Console.WriteLine($"   Размер: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения: {ex.Message}");
                Console.WriteLine("💡 Пробуем альтернативный способ сохранения...");
                
                try
                {
                    // Альтернативный способ без WriterParameters
                    var directory = Path.GetDirectoryName(assemblyPath);
                    var filename = Path.GetFileNameWithoutExtension(assemblyPath);
                    var extension = Path.GetExtension(assemblyPath);
                    var outputPath = Path.Combine(directory, $"{filename}_Cracked{extension}");
                    
                    using (var stream = new FileStream(outputPath, FileMode.Create))
                    {
                        assembly.Write(stream);
                    }
                    
                    var fileInfo = new FileInfo(outputPath);
                    Console.WriteLine($"✅ Пропатченная сборка сохранена (альтернативный способ):");
                    Console.WriteLine($"   Путь: {outputPath}");
                    Console.WriteLine($"   Размер: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                    
                    return true;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"❌ Критическая ошибка сохранения: {ex2.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Сохранение пропатченной сборки по указанному пути
        /// </summary>
        public bool SavePatched(string outputPath)
        {
            if (assembly == null)
            {
                Console.WriteLine("❌ Сборка не загружена");
                return false;
            }

            try
            {
                // Создаем WriterParameters для корректного сохранения Unity сборок
                var writerParameters = new WriterParameters()
                {
                    WriteSymbols = false // Отключаем символы для избежания проблем с зависимостями
                };

                assembly.Write(outputPath, writerParameters);
                
                var fileInfo = new FileInfo(outputPath);
                Console.WriteLine($"✅ Пропатченная сборка сохранена:");
                Console.WriteLine($"   Путь: {outputPath}");
                Console.WriteLine($"   Размер: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка сохранения: {ex.Message}");
                Console.WriteLine("💡 Пробуем альтернативный способ сохранения...");
                
                try
                {
                    // Альтернативный способ без WriterParameters
                    using (var stream = new FileStream(outputPath, FileMode.Create))
                    {
                        assembly.Write(stream);
                    }
                    
                    var fileInfo = new FileInfo(outputPath);
                    Console.WriteLine($"✅ Пропатченная сборка сохранена (альтернативный способ):");
                    Console.WriteLine($"   Путь: {outputPath}");
                    Console.WriteLine($"   Размер: {fileInfo.Length / (1024.0 * 1024.0):F2} MB");
                    
                    return true;
                }
                catch (Exception ex2)
                {
                    Console.WriteLine($"❌ Критическая ошибка сохранения: {ex2.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            assembly?.Dispose();
        }

        /// <summary>
        /// Получить загруженную сборку для анализа
        /// </summary>
        public AssemblyDefinition GetAssembly()
        {
            return assembly;
        }
    }
} 