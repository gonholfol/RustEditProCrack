using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;

namespace RustEditProCrack.Unlockers
{
    /// <summary>
    /// Умный патчер префабов - удаляет ТОЛЬКО сообщения об ошибках, сохраняя логику
    /// </summary>
    public class SmartPrefabUnlocker
    {
        private readonly AssemblyDefinition assembly;

        public SmartPrefabUnlocker(AssemblyDefinition assembly)
        {
            this.assembly = assembly;
        }

        /// <summary>
        /// Умная разблокировка - удаляем только ошибки, сохраняем логику
        /// </summary>
        public bool UnlockPrefabsSmartly()
        {
            Console.WriteLine("🧠 === УМНАЯ РАЗБЛОКИРОВКА ПРЕФАБОВ ===");
            
            try
            {
                var methods = FindAllPrefabMethods();
                
                if (!methods.Any())
                {
                    Console.WriteLine("❌ Методы с 'Prefab.FileDoesntExist' не найдены");
                    return false;
                }
                
                bool allSuccessful = true;
                foreach (var (method, className, methodName) in methods)
                {
                    Console.WriteLine($"\n🎯 Обрабатываю: {className}.{methodName}");
                    
                    if (!RemoveErrorMessagesOnly(method))
                    {
                        Console.WriteLine($"❌ Не удалось обработать {className}.{methodName}");
                        allSuccessful = false;
                    }
                    else
                    {
                        Console.WriteLine($"✅ Успешно обработан {className}.{methodName}");
                    }
                }
                
                if (allSuccessful)
                {
                    Console.WriteLine("\n🎉 ВСЕ ПРЕФАБЫ РАЗБЛОКИРОВАНЫ УМНО!");
                }
                
                return allSuccessful;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Находит ТОЛЬКО корутинные методы с "Prefab.FileDoesntExist" (как в изначальном требовании)
        /// </summary>
        private System.Collections.Generic.List<(MethodDefinition method, string className, string methodName)> FindAllPrefabMethods()
        {
            Console.WriteLine("🔍 Поиск ТОЛЬКО КОРУТИН с 'Prefab.FileDoesntExist'...");
            
            var foundMethods = new System.Collections.Generic.List<(MethodDefinition, string, string)>();
            
            foreach (var type in assembly.MainModule.Types)
            {
                // Обычные методы - ТОЛЬКО корутины
                foreach (var method in type.Methods)
                {
                    if (IsCoroutine(method) && ContainsString(method, "Prefab.FileDoesntExist"))
                    {
                        Console.WriteLine($"   ✅ Найдена корутина: {type.Name}.{method.Name}");
                        foundMethods.Add((method, type.Name, method.Name));
                    }
                }
                
                // Вложенные типы (state machines) - всегда корутины
                foreach (var nestedType in type.NestedTypes)
                {
                    foreach (var method in nestedType.Methods)
                    {
                        if (ContainsString(method, "Prefab.FileDoesntExist"))
                        {
                            Console.WriteLine($"   ✅ Найдена state machine: {type.Name}.{nestedType.Name}.{method.Name}");
                            foundMethods.Add((method, $"{type.Name}.{nestedType.Name}", method.Name));
                        }
                    }
                }
            }
            
            Console.WriteLine($"📊 Найдено корутин для обработки: {foundMethods.Count}");
            return foundMethods;
        }

        /// <summary>
        /// SURGICAL удаление - только конкретные инструкции связанные с ошибкой
        /// </summary>
        private bool RemoveErrorMessagesOnly(MethodDefinition method)
        {
            Console.WriteLine($"🔬 SURGICAL удаление ошибок в {method.Name}...");
            
            try
            {
                var instructions = method.Body.Instructions;
                var ilProcessor = method.Body.GetILProcessor();
                
                bool foundAny = false;
                
                // ЭТАП 1: ИСПРАВЛЯЕМ flag = true ПО УМОЛЧАНИЮ
                Console.WriteLine("🔧 === ЭТАП 1: ИСПРАВЛЕНИЕ flag = true ===");
                Console.WriteLine("🎯 Ищем инициализацию flag = false перед File.Exists проверкой...");
                
                for (int i = 0; i < instructions.Count - 5; i++)
                {
                    var inst = instructions[i];
                    
                    // Ищем ldc.i4.0 ПЕРЕД File.Exists вызовом
                    if (inst.OpCode == OpCodes.Ldc_I4_0)
                    {
                        // Проверяем что следующие инструкции соответствуют паттерну flag инициализации
                        bool isFlag = false;
                        
                        // Ищем в следующих 5 инструкциях File.Exists
                        for (int j = i + 1; j < Math.Min(instructions.Count, i + 10); j++)
                        {
                            var checkInst = instructions[j];
                            if (checkInst.OpCode == OpCodes.Call && 
                                checkInst.Operand?.ToString().Contains("File::Exists") == true)
                            {
                                Console.WriteLine($"🎯 Найдена инициализация flag = false на позиции {i} перед File.Exists");
                                isFlag = true;
                                break;
                            }
                        }
                        
                        if (isFlag)
                        {
                            Console.WriteLine($"🔄 ЗАМЕНА: {i}: ldc.i4.0 → ldc.i4.1 (flag = true)");
                            ilProcessor.Replace(inst, Instruction.Create(OpCodes.Ldc_I4_1));
                            foundAny = true;
                            break; // Нашли - этого достаточно
                        }
                    }
                }
                
                // ЭТАП 2: УБИРАЕМ ВСЮ ЛОГИКУ ПРОВЕРКИ ЧЕКСУММЫ
                Console.WriteLine("🔥 === ЭТАП 2: УБИРАЕМ ПРОВЕРКУ ЧЕКСУММЫ ===");
                Console.WriteLine("💡 Заменяем ВСЮ логику проверки чексуммы на flag = true");
                
                for (int i = 0; i < instructions.Count - 10; i++)
                {
                    var inst = instructions[i];
                    
                    // Ищем загрузку поля checksum (ldfld checksum)
                    if (inst.OpCode == OpCodes.Ldfld && 
                        inst.Operand?.ToString().Contains("checksum") == true)
                    {
                        Console.WriteLine($"🎯 Найдена загрузка checksum на позиции {i}");
                        
                        // Ищем конец блока проверки чексуммы - следующий stloc.s V_8 (flag)
                        for (int j = i + 1; j < Math.Min(instructions.Count, i + 25); j++)
                        {
                            var endInst = instructions[j];
                            
                            // Найдена инструкция сохранения в flag (V_8)
                            if (endInst.OpCode == OpCodes.Stloc_S)
                            {
                                Console.WriteLine($"🔥 ЗАМЕНЯЕМ ВЕСЬ БЛОК ПРОВЕРКИ ЧЕКСУММЫ (позиции {i}-{j})");
                                
                                // ПРАВИЛЬНАЯ СТРАТЕГИЯ:
                                // 1. Заменяем ldfld checksum на pop (убираем значение со стека)
                                // 2. Заменяем всё до stloc.s на ldc.i4.1 + NOP
                                
                                Console.WriteLine($"   🔄 ЗАМЕНА: {i}: {instructions[i].OpCode} → pop (убираем checksum со стека)");
                                ilProcessor.Replace(instructions[i], Instruction.Create(OpCodes.Pop));
                                
                                // Следующую инструкцию заменяем на ldc.i4.1
                                if (i + 1 < j)
                                {
                                    Console.WriteLine($"   🔄 ЗАМЕНА: {i + 1}: {instructions[i + 1].OpCode} → ldc.i4.1 (true)");
                                    ilProcessor.Replace(instructions[i + 1], Instruction.Create(OpCodes.Ldc_I4_1));
                                }
                                
                                // Все промежуточные заменяем на NOP
                                for (int k = i + 2; k < j; k++)
                                {
                                    Console.WriteLine($"   🔄 ЗАМЕНА: {k}: {instructions[k].OpCode} → nop");
                                    ilProcessor.Replace(instructions[k], Instruction.Create(OpCodes.Nop));
                                }
                                
                                // Последнюю (stloc.s) оставляем - она сохранит true в flag
                                Console.WriteLine($"   ✅ СОХРАНЯЕМ: {j}: {endInst.OpCode} (сохранение в flag)");
                                
                                foundAny = true;
                                break;
                            }
                        }
                        
                        break; // Нашли и обработали блок
                    }
                }

                // ЭТАП 3: УБИРАЕМ СООБЩЕНИЯ ОБ ОШИБКАХ  
                Console.WriteLine("🔬 === ЭТАП 3: УДАЛЕНИЕ СООБЩЕНИЙ ОБ ОШИБКАХ ===");
                for (int i = 0; i < instructions.Count; i++)
                {
                    var inst = instructions[i];
                    
                    // Нашли строку ошибки
                    if (inst.OpCode == OpCodes.Ldstr && 
                        inst.Operand?.ToString() == "Prefab.FileDoesntExist")
                    {
                        Console.WriteLine($"🎯 Найдена строка ошибки на позиции {i}");
                        foundAny = true;
                        
                        Console.WriteLine("💡 Удаляем ТОЛЬКО сообщение об ошибке, НЕ ТРОГАЯ логику!");
                        
                        // Ищем весь блок ошибки для удаления (от ldstr до PopupHandler)
                        Console.WriteLine("🔍 Поиск блока ошибки...");
                        bool foundPopupHandler = false;
                        
                        // КРИТИЧЕСКОЕ ДОПОЛНЕНИЕ: заменяем удаленный блок на NOP инструкции
                        Console.WriteLine("🔧 ЗАМЕНЯЕМ удаленный блок на NOP для сохранения структуры");
                        
                        // Заменяем блок ошибки на NOP
                        for (int j = i; j < Math.Min(instructions.Count, i + 15); j++)
                        {
                            var errorInst = instructions[j];
                            
                            Console.WriteLine($"   🔄 ЗАМЕНА: {j}: {errorInst.OpCode} → nop");  
                            ilProcessor.Replace(errorInst, Instruction.Create(OpCodes.Nop));
                            
                            // Останавливаемся на PopupHandler или безусловном переходе
                            if ((errorInst.OpCode == OpCodes.Call && 
                                 errorInst.Operand?.ToString().Contains("QueueMessage") == true) ||
                                errorInst.OpCode == OpCodes.Br || errorInst.OpCode == OpCodes.Br_S)
                            {
                                Console.WriteLine($"🏁 Конец замены блока на позиции {j}");
                                foundPopupHandler = true;
                                break;
                            }
                        }
                        
                        if (!foundPopupHandler)
                        {
                            Console.WriteLine("⚠️ PopupHandler не найден, заменил только строку");
                        }
                    }
                }
                
                if (!foundAny)
                {
                    Console.WriteLine("⚠️ Ничего не найдено для исправления");
                    return false;
                }
                
                if (foundAny)
                {
                    Console.WriteLine($"✅ SURGICAL исправления выполнены!");
                    Console.WriteLine("🔧 flag = true по умолчанию");
                    Console.WriteLine("🔥 ВСЯ проверка чексуммы заменена на flag = true");
                    Console.WriteLine("🔬 Блок ошибки заменен на NOP - стек сохранен");
                    Console.WriteLine("🎉 Префабы теперь загружаются БЕЗ ВСЯКИХ ПРОВЕРОК!");
                    return true;
                }
                else
                {
                    Console.WriteLine("⚠️ Не найдено элементов для исправления");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ОШИБКА: {ex.Message}");
                return false;
            }
        }

        private bool IsCoroutine(MethodDefinition method)
        {
            if (!method.HasBody) return false;
            
            var returnType = method.ReturnType.FullName;
            return returnType.Contains("IEnumerable") || 
                   returnType.Contains("IEnumerator") ||
                   method.CustomAttributes.Any(attr => attr.AttributeType.Name.Contains("IteratorStateMachine"));
        }

        private bool ContainsString(MethodDefinition method, string searchString)
        {
            if (!method.HasBody) return false;
            
            return method.Body.Instructions
                .Where(i => i.OpCode == OpCodes.Ldstr)
                .Any(i => i.Operand?.ToString().Contains(searchString) == true);
        }

        /// <summary>
        /// Публичный метод для внешнего анализа - находит все методы с "Prefab.FileDoesntExist"
        /// </summary>
        public System.Collections.Generic.List<(MethodDefinition method, string className, string methodName)> FindAllPrefabMethodsPublic()
        {
            return FindAllPrefabMethods();
        }
    }
} 