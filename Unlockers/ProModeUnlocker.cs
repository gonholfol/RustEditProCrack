using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RustEditProCrack.Unlockers
{
    /// <summary>
    /// Класс для разблокировки PRO режима в RustEdit Pro
    /// </summary>
    public class ProModeUnlocker
    {
        private readonly AssemblyDefinition assembly;
        
        // Динамически извлеченные обфусцированные имена
        private string proModeClassName = null;
        private string proModeMethodName = null;

        public ProModeUnlocker(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
        }

        /// <summary>
        /// Основной метод для разблокировки PRO режима
        /// </summary>
        public bool UnlockProMode()
        {
            Console.WriteLine("🔧 === РАЗБЛОКИРОВКА PRO РЕЖИМА ===");
            
            try
            {
                // Шаг 1: Извлечь обфусцированные имена
                if (!ExtractObfuscatedNames())
                {
                    Console.WriteLine("❌ Не удалось извлечь обфусцированные имена");
                    return false;
                }

                // Шаг 2: Применить патч PRO режима
                if (!ApplyProModePatch())
                {
                    Console.WriteLine("❌ Не удалось применить патч PRO режима");
                    return false;
                }

                // Шаг 3: Удалить блокирующие методы
                if (!RemoveProModeBlocks())
                {
                    Console.WriteLine("❌ Не удалось удалить блокирующие методы");
                    return false;
                }

                Console.WriteLine("✅ PRO РЕЖИМ УСПЕШНО РАЗБЛОКИРОВАН!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка разблокировки PRO режима: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Извлечение обфусцированных имен через анализ DiscordPresence
        /// </summary>
        private bool ExtractObfuscatedNames()
        {
            Console.WriteLine("🔍 Извлечение обфусцированных имен...");
            
            var discordType = assembly.MainModule.Types
                .FirstOrDefault(t => t.Name.Contains("DiscordPresence"));

            if (discordType == null)
            {
                Console.WriteLine("❌ DiscordPresence класс не найден");
                return false;
            }

            var updateMethod = discordType.Methods
                .FirstOrDefault(m => m.Name == "UpdateActivity");

            if (updateMethod?.HasBody != true)
            {
                Console.WriteLine("❌ UpdateActivity метод не найден");
                return false;
            }

            var instructions = updateMethod.Body.Instructions;

            for (int i = 0; i < instructions.Count; i++)
            {
                var inst = instructions[i];

                if (inst.OpCode == OpCodes.Ldstr && inst.Operand?.ToString() == "512_2")
                {
                    int start = Math.Max(0, i - 20);
                    int end = Math.Min(instructions.Count - 1, i + 10);

                    for (int k = start; k <= end; k++)
                    {
                        var contextInst = instructions[k];

                        if ((contextInst.OpCode == OpCodes.Call || contextInst.OpCode == OpCodes.Callvirt) &&
                            contextInst.Operand is MethodReference methodRef)
                        {
                            if (methodRef.ReturnType?.FullName == "System.Boolean" &&
                                IsObfuscatedName(methodRef.DeclaringType.Name) &&
                                IsObfuscatedName(methodRef.Name) &&
                                methodRef.Parameters.Count == 0)
                            {
                                proModeClassName = methodRef.DeclaringType.Name;
                                proModeMethodName = methodRef.Name;
                                
                                Console.WriteLine($"✅ Найдены обфусцированные имена:");
                                Console.WriteLine($"   Класс: {proModeClassName}");
                                Console.WriteLine($"   Метод: {proModeMethodName}");
                                return true;
                            }
                        }
                    }
                    break;
                }
            }

            return false;
        }

        /// <summary>
        /// Применение патча PRO режима
        /// </summary>
        private bool ApplyProModePatch()
        {
            Console.WriteLine("🔧 Применение патча PRO режима...");
            
            var proModeClass = assembly.MainModule.Types
                .FirstOrDefault(t => t.Name == proModeClassName);

            if (proModeClass == null)
            {
                Console.WriteLine($"❌ PRO класс {proModeClassName} не найден");
                return false;
            }

            var proModeMethod = proModeClass.Methods
                .FirstOrDefault(m => m.Name == proModeMethodName);

            if (proModeMethod?.HasBody != true)
            {
                Console.WriteLine($"❌ PRO метод {proModeMethodName} не найден");
                return false;
            }

            Console.WriteLine($"✅ Найден PRO метод: {proModeClassName}.{proModeMethodName}");

            return PatchMethodToReturnTrue(proModeMethod);
        }

        /// <summary>
        /// Удаление блокирующих методов PRO режима
        /// </summary>
        private bool RemoveProModeBlocks()
        {
            Console.WriteLine("🔧 Удаление блокирующих методов PRO режима...");
            
            var njfClass = assembly.MainModule.Types
                .FirstOrDefault(t => t.Name == "NJFSINOIPNMDA");

            if (njfClass == null)
            {
                Console.WriteLine("❌ NJFSINOIPNMDA класс не найден");
                return false;
            }

            Console.WriteLine($"✅ Найден NJFSINOIPNMDA класс с {njfClass.Methods.Count} методами");

            int modifiedMethods = 0;

            foreach (var method in njfClass.Methods)
            {
                if (!method.HasBody || method.Name == ".ctor" || method.Name == "Awake")
                    continue;

                if (ContainsProModeCheck(method))
                {
                    Console.WriteLine($"🎯 Блокирующий метод: {method.Name}()");
                    
                    if (ClearBlockingMethod(method))
                    {
                        modifiedMethods++;
                        Console.WriteLine($"   ✅ Метод очищен");
                    }
                }
            }

            Console.WriteLine($"✅ Очищено {modifiedMethods} блокирующих методов");
            return modifiedMethods > 0;
        }

        /// <summary>
        /// Проверка содержит ли метод PRO проверку
        /// </summary>
        private bool ContainsProModeCheck(MethodDefinition method)
        {
            if (!method.HasBody || string.IsNullOrEmpty(proModeClassName)) 
                return false;

            var instructions = method.Body.Instructions;
            for (int i = 0; i < instructions.Count - 1; i++)
            {
                var inst = instructions[i];
                var nextInst = instructions[i + 1];

                if (inst.OpCode == OpCodes.Call &&
                    inst.Operand is MethodReference methodRef &&
                    methodRef.DeclaringType?.Name == proModeClassName &&
                    methodRef.ReturnType?.FullName == "System.Boolean")
                {
                    if (nextInst.OpCode == OpCodes.Brfalse_S || nextInst.OpCode == OpCodes.Brfalse)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Очистка блокирующего метода
        /// </summary>
        private bool ClearBlockingMethod(MethodDefinition method)
        {
            try
            {
                method.Body.Instructions.Clear();
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка очистки метода: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Патч метода для возврата true
        /// </summary>
        private bool PatchMethodToReturnTrue(MethodDefinition method)
        {
            try
            {
                method.Body.Instructions.Clear();
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1)); // load 1 (true)
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));      // return
                method.Body.Variables.Clear();
                method.Body.ExceptionHandlers.Clear();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка патча метода: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Проверка является ли имя обфусцированным
        /// </summary>
        private bool IsObfuscatedName(string name)
        {
            return name.Length >= 8 && name.Length <= 15 &&
                   name.All(c => char.IsUpper(c) || char.IsDigit(c)) &&
                   name.Any(char.IsLetter);
        }
    }
} 