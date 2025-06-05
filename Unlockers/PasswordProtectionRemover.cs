using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RustEditProCrack.Unlockers
{
    /// <summary>
    /// Класс для удаления защиты паролем в RustEdit Pro
    /// </summary>
    public class PasswordProtectionRemover
    {
        private readonly AssemblyDefinition assembly;

        public PasswordProtectionRemover(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
        }

        /// <summary>
        /// Основной метод для удаления защиты паролем
        /// </summary>
        public bool RemovePasswordProtection()
        {
            Console.WriteLine("🔧 === УДАЛЕНИЕ PASSWORD PROTECTION ===");
            
            try
            {
                // ОБНОВЛЕНО: Ищем новое имя класса BFJKBOOACKO вместо старого PLOFBHPMKFD
                var cryptoClass = assembly.MainModule.Types
                    .FirstOrDefault(t => t.Name == "BFJKBOOACKO");
                    
                if (cryptoClass == null)
                {
                    // Пытаемся найти с помощью характеристик, если имя снова изменилось
                    cryptoClass = FindCryptoClassBySignature();
                    
                    if (cryptoClass == null)
                    {
                        Console.WriteLine("❌ Класс шифрования не найден");
                        return false;
                    }
                }

                Console.WriteLine($"✅ Найден класс {cryptoClass.Name} с {cryptoClass.Methods.Count} методами");

                // Найти все методы шифрования/дешифрования (по характерным признакам)
                var encryptionMethods = cryptoClass.Methods
                    .Where(m => m.HasBody && m.ReturnType.FullName == "System.String" && 
                           m.Parameters.Count == 2 && 
                           m.Parameters[0].ParameterType.FullName == "System.String" && 
                           m.Parameters[1].ParameterType.FullName == "System.Int32")
                    .ToList();

                if (!encryptionMethods.Any())
                {
                    Console.WriteLine("❌ Методы шифрования не найдены");
                    return false;
                }

                Console.WriteLine($"✅ Найдено {encryptionMethods.Count} методов шифрования/дешифрования");

                int patchedMethods = 0;

                // Патчим каждый метод для игнорирования пароля
                foreach (var method in encryptionMethods)
                {
                    if (PatchEncryptionMethod(method))
                    {
                        patchedMethods++;
                        Console.WriteLine($"✅ Пропатчен метод: {method.Name}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Не удалось пропатчить метод: {method.Name}");
                    }
                }

                Console.WriteLine($"\n🎉 Успешно пропатчено {patchedMethods} из {encryptionMethods.Count} методов шифрования");
                return patchedMethods > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка удаления password protection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Находит класс шифрования по сигнатурам методов
        /// </summary>
        private TypeDefinition FindCryptoClassBySignature()
        {
            Console.WriteLine("🔍 Ищем класс шифрования по сигнатурам...");
            
            foreach (var type in assembly.MainModule.Types)
            {
                // Проверяем, есть ли у класса методы с характерными признаками шифрования
                var encryptionMethods = type.Methods
                    .Where(m => m.HasBody && m.ReturnType.FullName == "System.String" && 
                           m.Parameters.Count == 2 && 
                           m.Parameters[0].ParameterType.FullName == "System.String" && 
                           m.Parameters[1].ParameterType.FullName == "System.Int32")
                    .ToList();
                
                if (encryptionMethods.Count >= 3)
                {
                    // Проверяем, есть ли в методах использование классов шифрования
                    foreach (var method in encryptionMethods)
                    {
                        bool containsCrypto = method.Body.Instructions
                            .Any(i => i.OpCode == OpCodes.Newobj && 
                                  i.Operand is MethodReference methodRef && 
                                  (methodRef.DeclaringType.Name.Contains("Aes") || 
                                   methodRef.DeclaringType.Name.Contains("Rfc2898DeriveBytes") ||
                                   methodRef.DeclaringType.Name.Contains("CryptoStream")));
                        
                        if (containsCrypto)
                        {
                            Console.WriteLine($"🔍 Найден вероятный класс шифрования: {type.Name}");
                            return type;
                        }
                    }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Патчит метод шифрования, чтобы он просто возвращал исходную строку без шифрования
        /// </summary>
        private bool PatchEncryptionMethod(MethodDefinition method)
        {
            Console.WriteLine($"🔧 Патчинг метода {method.Name}...");
            
            try
            {
                var ilProcessor = method.Body.GetILProcessor();
                
                // Очищаем тело метода
                method.Body.Instructions.Clear();
                method.Body.ExceptionHandlers.Clear();
                
                // Добавляем новые инструкции: просто возвращаем исходную строку (первый параметр)
                ilProcessor.Append(Instruction.Create(OpCodes.Ldarg_0)); // Загрузить первый параметр (строка) на стек
                ilProcessor.Append(Instruction.Create(OpCodes.Ret));     // Вернуть значение со стека
                
                Console.WriteLine($"✅ Метод {method.Name} успешно пропатчен для возврата исходной строки без шифрования");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка патчинга метода {method.Name}: {ex.Message}");
                return false;
            }
        }
    }
} 