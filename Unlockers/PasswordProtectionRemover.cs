using System;
using System.Linq;
using System.Collections.Generic;
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
                // Патч 1: Модифицировать метод проверки защиты паролем (NGILNCKJOAM.ENIPGGOKCNF)
                bool patchedFileProtectionCheck = PatchFilePasswordProtectionMethod();
                
                // Патч 2: Модифицировать методы проверки пароля (DEEDFDIKIKJ)
                bool patchedPasswordValidation = PatchPasswordValidationMethods();
                
                // Патч 3: Модифицировать методы диалога пароля (HSUDFEEOGKC)
                bool patchedPasswordDialog = PatchPasswordDialogMethods();
                
                return patchedFileProtectionCheck || patchedPasswordValidation || patchedPasswordDialog;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при удалении защиты паролем: {ex.Message}");
                Console.WriteLine($"   {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Патчит метод проверки защищенности файла паролем (аналог NGILNCKJOAM.ENIPGGOKCNF)
        /// </summary>
        private bool PatchFilePasswordProtectionMethod()
        {
            Console.WriteLine("🔍 Поиск методов проверки защиты паролем...");
            
            // Ищем класс по характеристикам (аналог NGILNCKJOAM)
            TypeDefinition fileProtectionClass = FindFileProtectionClass();

            if (fileProtectionClass == null)
            {
                Console.WriteLine("❌ Не найден класс для проверки защиты файлов паролем");
                return false;
            }

            Console.WriteLine($"✅ Найден класс для проверки защиты паролем: {fileProtectionClass.Name}");
            
            // Ищем метод проверки защищенности файла паролем (аналог ENIPGGOKCNF)
            MethodDefinition fileProtectionMethod = FindFileProtectionMethod(fileProtectionClass);

            if (fileProtectionMethod == null)
            {
                Console.WriteLine("❌ Не найден метод проверки защиты паролем");
                return false;
            }

            Console.WriteLine($"✅ Найден метод проверки защиты паролем: {fileProtectionMethod.Name}");
            
            // Модифицируем метод, чтобы он всегда возвращал false
            if (fileProtectionMethod.HasBody)
            {
                var il = fileProtectionMethod.Body.GetILProcessor();
                fileProtectionMethod.Body.Instructions.Clear();
                il.Append(Instruction.Create(OpCodes.Ldc_I4_0)); // Загружаем false (0)
                il.Append(Instruction.Create(OpCodes.Ret));      // Возвращаем false
                
                Console.WriteLine($"✅ Метод {fileProtectionMethod.Name} успешно пропатчен - всегда возвращает false");
                return true;
            }
            
            Console.WriteLine($"❌ Метод {fileProtectionMethod.Name} не имеет тела");
            return false;
        }
        
        /// <summary>
        /// Находит класс, отвечающий за проверку защиты файлов паролем (аналог NGILNCKJOAM)
        /// </summary>
        private TypeDefinition FindFileProtectionClass()
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownClass = assembly.MainModule.GetTypes()
                .FirstOrDefault(t => t.Name == "NGILNCKJOAM");
                
            if (knownClass != null)
                return knownClass;
                
            // 2. Поиск по характеристикам - класс с методами проверки файлов на защиту паролем
            // Такие классы обычно имеют методы, которые проверяют пути файлов и используют File.Exists
            foreach (var type in assembly.MainModule.GetTypes())
            {
                if (type.Methods.Any(m => 
                    m.HasBody && 
                    m.ReturnType.FullName == "System.Boolean" &&
                    m.Parameters.Count == 1 && 
                    m.Parameters[0].ParameterType.FullName == "System.String" &&
                    ContainsFileCheck(m)))
                {
                    return type;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Находит метод, проверяющий защищен ли файл паролем (аналог ENIPGGOKCNF)
        /// </summary>
        private MethodDefinition FindFileProtectionMethod(TypeDefinition classType)
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownMethod = classType.Methods
                .FirstOrDefault(m => m.Name == "ENIPGGOKCNF");
                
            if (knownMethod != null)
                return knownMethod;
                
            // 2. Поиск по характеристикам - метод проверяющий защищенность файла паролем
            // Такие методы обычно принимают путь к файлу (string), возвращают boolean 
            // и содержат проверки файлов (File.Exists) с расширениями файлов защиты паролем
            foreach (var method in classType.Methods)
            {
                if (method.HasBody && 
                    method.ReturnType.FullName == "System.Boolean" &&
                    method.Parameters.Count == 1 && 
                    method.Parameters[0].ParameterType.FullName == "System.String" &&
                    ContainsFileCheck(method))
                {
                    return method;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверяет, содержит ли метод проверки файловых путей
        /// </summary>
        private bool ContainsFileCheck(MethodDefinition method)
        {
            if (!method.HasBody)
                return false;
                
            bool containsFileExists = false;
            bool containsPathManipulation = false;
            
            foreach (var inst in method.Body.Instructions)
            {
                if (inst.OpCode == OpCodes.Call && inst.Operand is MethodReference mr)
                {
                    // Проверка на вызов File.Exists
                    if (mr.DeclaringType.FullName == "System.IO.File" && mr.Name == "Exists")
                    {
                        containsFileExists = true;
                    }
                    
                    // Проверка на вызовы методов Path для работы с путями
                    if (mr.DeclaringType.FullName == "System.IO.Path")
                    {
                        containsPathManipulation = true;
                    }
                }
                
                // Проверка на наличие строковых констант с расширениями файлов
                if (inst.OpCode == OpCodes.Ldstr && inst.Operand is string str)
                {
                    string lower = str.ToLowerInvariant();
                    if (lower.Contains(".pwd") || lower.Contains(".key") || lower.EndsWith(".ps") || 
                        lower.Contains("password") || lower.Contains("защит") || 
                        lower.Contains("protect"))
                    {
                        return true;
                    }
                }
            }
            
            return containsFileExists && containsPathManipulation;
        }
        
        /// <summary>
        /// Патчит методы проверки пароля (аналог DEEDFDIKIKJ)
        /// </summary>
        private bool PatchPasswordValidationMethods()
        {
            Console.WriteLine("🔍 Поиск методов проверки пароля...");
            
            // Ищем класс проверки пароля по характеристикам (аналог DEEDFDIKIKJ)
            TypeDefinition passwordValidationClass = FindPasswordValidationClass();
            
            if (passwordValidationClass == null)
            {
                Console.WriteLine("❌ Не найден класс проверки пароля");
                return false;
            }
            
            Console.WriteLine($"✅ Найден класс для проверки пароля: {passwordValidationClass.Name}");
            
            bool patchedAny = false;
            
            // Патчим свойства проверки пароля (BCFBBFAIKFI и PENGPPJKOGC)
            string[] knownPropertyNames = { "BCFBBFAIKFI", "PENGPPJKOGC" };
            foreach (var propName in knownPropertyNames)
            {
                PropertyDefinition property = passwordValidationClass.Properties
                    .FirstOrDefault(p => p.Name == propName && p.PropertyType.FullName == "System.Boolean");
                    
                if (property != null && property.GetMethod != null && property.GetMethod.HasBody)
                {
                    Console.WriteLine($"✅ Найдено свойство проверки пароля: {property.Name}");
                    
                    // Модифицируем геттер свойства, чтобы он всегда возвращал false
                    var il = property.GetMethod.Body.GetILProcessor();
                    property.GetMethod.Body.Instructions.Clear();
                    il.Append(Instruction.Create(OpCodes.Ldc_I4_0)); // Загружаем false (0)
                    il.Append(Instruction.Create(OpCodes.Ret));      // Возвращаем false
                    
                    Console.WriteLine($"✅ Свойство {property.Name} успешно пропатчено - всегда возвращает false");
                    patchedAny = true;
                }
            }
            
            // Если не нашли известные свойства, ищем по характеристикам
            if (!patchedAny)
            {
                foreach (var property in passwordValidationClass.Properties)
                {
                    if (property.PropertyType.FullName == "System.Boolean" && 
                        property.GetMethod != null && 
                        property.GetMethod.HasBody)
                    {
                        // Проверяем, связано ли свойство с проверкой пароля
                        bool isPasswordProperty = IsPasswordRelatedProperty(property);
                        
                        if (isPasswordProperty)
                        {
                            Console.WriteLine($"✅ Найдено свойство проверки пароля: {property.Name}");
                            
                            // Модифицируем геттер свойства, чтобы он всегда возвращал false
                            var il = property.GetMethod.Body.GetILProcessor();
                            property.GetMethod.Body.Instructions.Clear();
                            il.Append(Instruction.Create(OpCodes.Ldc_I4_0)); // Загружаем false (0)
                            il.Append(Instruction.Create(OpCodes.Ret));      // Возвращаем false
                            
                            Console.WriteLine($"✅ Свойство {property.Name} успешно пропатчено - всегда возвращает false");
                            patchedAny = true;
                        }
                    }
                }
            }
            
            // Ищем и патчим методы проверки пароля (аналоги HDIFCDOGJNE, DDMMKBPGGFP и т.д.)
            var validationMethods = FindPasswordValidationMethods(passwordValidationClass);
            
            foreach (var method in validationMethods)
            {
                Console.WriteLine($"✅ Найден метод проверки пароля: {method.Name}");
                
                // Модифицируем метод, чтобы он всегда возвращал true
                var il = method.Body.GetILProcessor();
                method.Body.Instructions.Clear();
                il.Append(Instruction.Create(OpCodes.Ldc_I4_1)); // Загружаем true (1)
                il.Append(Instruction.Create(OpCodes.Ret));      // Возвращаем true
                
                Console.WriteLine($"✅ Метод {method.Name} успешно пропатчен - всегда возвращает true");
                patchedAny = true;
            }
            
            return patchedAny;
        }
        
        /// <summary>
        /// Проверяет, связано ли свойство с проверкой пароля
        /// </summary>
        private bool IsPasswordRelatedProperty(PropertyDefinition property)
        {
            if (property.GetMethod == null || !property.GetMethod.HasBody)
                return false;
                
            // Проверка по имени свойства
            string name = property.Name.ToLowerInvariant();
            if (name.Contains("pass") || 
                name.Contains("pwd") || 
                name.Contains("protect") || 
                name.Contains("secure") || 
                name.Contains("valid"))
            {
                return true;
            }
            
            // Проверка по полю, которое используется в свойстве
            foreach (var inst in property.GetMethod.Body.Instructions)
            {
                if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference fr)
                {
                    string fieldName = fr.Name.ToLowerInvariant();
                    if (fieldName.Contains("pass") || 
                        fieldName.Contains("pwd") || 
                        fieldName.Contains("protect") || 
                        fieldName.Contains("secure") || 
                        fieldName.Contains("valid"))
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Находит класс, отвечающий за валидацию пароля (аналог DEEDFDIKIKJ)
        /// </summary>
        private TypeDefinition FindPasswordValidationClass()
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownClass = assembly.MainModule.GetTypes()
                .FirstOrDefault(t => t.Name == "DEEDFDIKIKJ");
                
            if (knownClass != null)
                return knownClass;
                
            // 2. Поиск по характеристикам класса проверки пароля
            List<TypeDefinition> candidates = new List<TypeDefinition>();
            
            foreach (var type in assembly.MainModule.GetTypes())
            {
                // Такие классы обычно имеют булевы свойства и методы для проверки пароля
                int booleanProperties = type.Properties.Count(p => 
                    p.PropertyType.FullName == "System.Boolean" && p.GetMethod != null);
                    
                int booleanMethods = type.Methods.Count(m => 
                    m.ReturnType.FullName == "System.Boolean" && 
                    m.HasBody && 
                    (m.Parameters.Count == 0 || m.Parameters.Count == 1 && m.Parameters[0].ParameterType.FullName == "System.String"));
                
                // Проверка на наличие строковых констант, связанных с паролем
                bool hasPasswordStrings = false;
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        foreach (var inst in method.Body.Instructions)
                        {
                            if (inst.OpCode == OpCodes.Ldstr && inst.Operand is string str)
                            {
                                string lower = str.ToLowerInvariant();
                                if (lower.Contains("password") || 
                                    lower.Contains("пароль") || 
                                    lower.Contains("защищен") || 
                                    lower.Contains("protected") || 
                                    lower.Contains("pwd") || 
                                    lower.Contains("secure"))
                                {
                                    hasPasswordStrings = true;
                                    break;
                                }
                            }
                        }
                        if (hasPasswordStrings) break;
                    }
                }
                
                // Проверка на наличие полей, связанных с паролем
                bool hasPasswordFields = type.Fields.Any(f => 
                    f.Name.ToLowerInvariant().Contains("password") || 
                    f.Name.ToLowerInvariant().Contains("pass") || 
                    f.Name.ToLowerInvariant().Contains("pwd") || 
                    f.Name.ToLowerInvariant().Contains("secure"));
                
                // Вычисляем "рейтинг" класса как кандидата
                int score = 0;
                if (booleanProperties >= 1) score += booleanProperties * 2;
                if (booleanMethods >= 3) score += booleanMethods;
                if (hasPasswordStrings) score += 5;
                if (hasPasswordFields) score += 3;
                
                // Если класс набрал достаточно очков, добавляем его в кандидаты
                if (score >= 5)
                {
                    candidates.Add(type);
                }
            }
            
            // Сортируем кандидатов по "рейтингу" и возвращаем лучшего
            if (candidates.Count > 0)
            {
                var bestCandidate = candidates.OrderByDescending(c => 
                    c.Properties.Count(p => p.PropertyType.FullName == "System.Boolean") + 
                    c.Methods.Count(m => m.ReturnType.FullName == "System.Boolean") * 2).First();
                    
                return bestCandidate;
            }
            
            // Если не нашли по характеристикам, ищем класс с наибольшим количеством булевых свойств и методов
            return assembly.MainModule.GetTypes()
                .OrderByDescending(t => 
                    t.Properties.Count(p => p.PropertyType.FullName == "System.Boolean") + 
                    t.Methods.Count(m => m.ReturnType.FullName == "System.Boolean") * 2)
                .FirstOrDefault();
        }
        
        /// <summary>
        /// Находит методы проверки пароля (аналоги HDIFCDOGJNE, DDMMKBPGGFP и т.д.)
        /// </summary>
        private List<MethodDefinition> FindPasswordValidationMethods(TypeDefinition classType)
        {
            var methods = new List<MethodDefinition>();
            
            // 1. Поиск по известным именам (обратная совместимость)
            string[] knownNames = { "HDIFCDOGJNE", "DDMMKBPGGFP", "IGAHKMCNLIN", "ODNEDFJEAAL", "FOLPAEOEMAF" };
            foreach (var name in knownNames)
            {
                var method = classType.Methods.FirstOrDefault(m => m.Name == name && m.ReturnType.FullName == "System.Boolean");
                if (method != null && method.HasBody)
                {
                    methods.Add(method);
                }
            }
            
            // 2. Если не нашли по известным именам, ищем по характеристикам
            if (methods.Count == 0)
            {
                foreach (var method in classType.Methods)
                {
                    // Методы проверки пароля обычно возвращают boolean и имеют простую структуру
                    if (method.HasBody && 
                        method.ReturnType.FullName == "System.Boolean" && 
                        (method.Parameters.Count == 0 || method.Parameters.Count == 1 && 
                        method.Parameters[0].ParameterType.FullName == "System.String") &&
                        !method.Name.StartsWith("get_") && // Исключаем геттеры свойств
                        !method.Name.StartsWith("set_"))   // Исключаем сеттеры свойств
                    {
                        methods.Add(method);
                    }
                }
            }
            
            return methods;
        }
        
        /// <summary>
        /// Патчит методы диалога пароля (аналог HSUDFEEOGKC)
        /// </summary>
        private bool PatchPasswordDialogMethods()
        {
            Console.WriteLine("🔍 Поиск методов диалога пароля...");
            
            // Ищем класс диалога пароля по характеристикам (аналог HSUDFEEOGKC)
            TypeDefinition dialogClass = FindPasswordDialogClass();
            
            if (dialogClass == null)
            {
                Console.WriteLine("❌ Не найден класс диалога пароля");
                return false;
            }
            
            Console.WriteLine($"✅ Найден класс диалога пароля: {dialogClass.Name}");
            
            bool patchedAny = false;
            
            // Ищем свойство статуса пароля (аналог FOLPAEOEMAF)
            PropertyDefinition passwordProperty = FindPasswordProperty(dialogClass);
            
            if (passwordProperty != null && passwordProperty.SetMethod != null)
            {
                MethodDefinition setMethod = passwordProperty.SetMethod;
                Console.WriteLine($"✅ Найдено свойство статуса пароля: {passwordProperty.Name}");
                Console.WriteLine($"✅ Найден сеттер свойства: {setMethod.Name}");
                
                // Ищем методы, которые используют сеттер свойства пароля (вызывают set_FOLPAEOEMAF)
                var passwordSetterMethods = FindPasswordSetterMethods(dialogClass, setMethod);
                
                // Выводим все найденные методы для отладки
                foreach (var method in passwordSetterMethods)
                {
                    Console.WriteLine($"✅ Найден метод, использующий сеттер {passwordProperty.Name}: {method.Name}");
                }
                
                // Известные имена критических методов, которые нужно патчить особым образом
                string[] criticalMethodNames = { "LDJIAMBFFDO", "Reset", "CIHNHHDGLMI", "NHBKJMNDJGO" };
                
                foreach (var method in passwordSetterMethods)
                {
                    bool isCriticalMethod = criticalMethodNames.Contains(method.Name);
                    
                    // Если это HPIGKCGMGEG или его аналог (обработчик кнопки Enter), патчим полностью
                    if (IsEnterButtonHandler(method) || method.Name == "HPIGKCGMGEG")
                    {
                        Console.WriteLine($"✅ Найден метод ввода пароля: {method.Name}");
                        
                        var il = method.Body.GetILProcessor();
                        method.Body.Instructions.Clear();
                        
                        // Загружаем this
                        il.Append(Instruction.Create(OpCodes.Ldarg_0));
                        // Загружаем true (1)
                        il.Append(Instruction.Create(OpCodes.Ldc_I4_1));
                        // Вызываем сеттер FOLPAEOEMAF
                        il.Append(Instruction.Create(OpCodes.Call, setMethod));
                        // Возвращаемся
                        il.Append(Instruction.Create(OpCodes.Ret));
                        
                        Console.WriteLine($"✅ Метод {method.Name} успешно пропатчен - всегда устанавливает {passwordProperty.Name} в true");
                        patchedAny = true;
                    }
                    // Для критических методов применяем более безопасный патчинг
                    else if (isCriticalMethod)
                    {
                        Console.WriteLine($"✅ Найден метод ввода пароля: {method.Name}");
                        
                        // Для критических методов используем безопасный патчинг
                        SafePatchPasswordMethod(method, setMethod);
                        
                        Console.WriteLine($"✅ Метод {method.Name} успешно пропатчен - всегда устанавливает {passwordProperty.Name} в true");
                        patchedAny = true;
                    }
                }
            }
            
            // Ищем метод диалога пароля (аналог HAIGGPJCCFI)
            MethodDefinition dialogMethod = FindPasswordDialogMethod(dialogClass);
            
            if (dialogMethod != null && dialogMethod.HasBody)
            {
                Console.WriteLine($"✅ Найден метод диалога пароля: {dialogMethod.Name}");
                
                // Находим метод, который создает пустой IEnumerator
                MethodReference emptyEnumeratorMethod = null;
                
                // Ищем в коде метода вызов метода, который возвращает IEnumerator
                foreach (var instruction in dialogMethod.Body.Instructions)
                {
                    if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) && 
                        instruction.Operand is MethodReference mr && 
                        mr.ReturnType.FullName.Contains("IEnumerator"))
                    {
                        emptyEnumeratorMethod = mr;
                        break;
                    }
                }
                
                if (emptyEnumeratorMethod != null)
                {
                    // Модифицируем метод HAIGGPJCCFI, чтобы он сразу возвращал пустой IEnumerator
                    var il = dialogMethod.Body.GetILProcessor();
                    dialogMethod.Body.Instructions.Clear();
                    
                    // Находим и вызываем метод, который возвращает пустой IEnumerator
                    il.Append(Instruction.Create(OpCodes.Call, emptyEnumeratorMethod));
                    il.Append(Instruction.Create(OpCodes.Ret));
                    
                    Console.WriteLine($"✅ Метод {dialogMethod.Name} успешно пропатчен - не показывает диалог пароля");
                    patchedAny = true;
                }
                else
                {
                    // Если не нашли подходящий метод, просто очищаем тело метода
                    var il = dialogMethod.Body.GetILProcessor();
                    
                    // Находим любой метод, который возвращает IEnumerator
                    MethodReference anyEnumeratorMethod = FindEmptyEnumeratorMethod();
                    
                    if (anyEnumeratorMethod != null)
                    {
                        dialogMethod.Body.Instructions.Clear();
                        il.Append(Instruction.Create(OpCodes.Call, anyEnumeratorMethod));
                        il.Append(Instruction.Create(OpCodes.Ret));
                        
                        Console.WriteLine($"✅ Метод {dialogMethod.Name} успешно пропатчен - использует существующий IEnumerator");
                        patchedAny = true;
                    }
                }
            }
            
            return patchedAny;
        }

        /// <summary>
        /// Безопасно патчит метод проверки пароля, сохраняя его оригинальное поведение для других функций
        /// </summary>
        private void SafePatchPasswordMethod(MethodDefinition method, MethodDefinition setterMethod)
        {
            // Находим все инструкции вызова сеттера свойства пароля
            var patchedAny = false;
            var il = method.Body.GetILProcessor();
            
            // Создаем копию инструкций для безопасного перебора
            var instructions = method.Body.Instructions.ToArray();
            
            for (int i = 0; i < instructions.Length; i++)
            {
                var inst = instructions[i];
                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) &&
                    inst.Operand is MethodReference mr &&
                    mr.Name == setterMethod.Name)
                {
                    // Находим инструкцию загрузки параметра для сеттера (должен быть перед вызовом)
                    if (i > 0 && instructions[i-1].OpCode == OpCodes.Ldc_I4_0)
                    {
                        // Заменяем загрузку false (0) на загрузку true (1)
                        int index = method.Body.Instructions.IndexOf(instructions[i-1]);
                        if (index >= 0)
                        {
                            method.Body.Instructions[index] = Instruction.Create(OpCodes.Ldc_I4_1);
                            patchedAny = true;
                        }
                    }
                }
            }
            
            // Если не смогли найти и заменить инструкции, добавляем код в начало метода
            if (!patchedAny)
            {
                // Сохраняем первую инструкцию
                var firstInstruction = method.Body.Instructions[0];
                
                // Вставляем код в начало метода
                il.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldarg_0));
                il.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Ldc_I4_1));
                il.InsertBefore(firstInstruction, Instruction.Create(OpCodes.Call, setterMethod));
            }
        }
        
        /// <summary>
        /// Находит класс диалога пароля (аналог HSUDFEEOGKC)
        /// </summary>
        private TypeDefinition FindPasswordDialogClass()
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownClass = assembly.MainModule.GetTypes()
                .FirstOrDefault(t => t.Name == "HSUDFEEOGKC");
                
            if (knownClass != null)
                return knownClass;
                
            // 2. Поиск по характеристикам класса диалога пароля
            foreach (var type in assembly.MainModule.GetTypes())
            {
                // Классы диалога пароля обычно имеют UI компоненты и методы IEnumerator
                bool hasUIFields = type.Fields.Any(f => 
                    f.FieldType.FullName.Contains("Panel") || 
                    f.FieldType.FullName.Contains("Input") || 
                    f.FieldType.FullName.Contains("Button") || 
                    f.FieldType.FullName.Contains("Text"));
                    
                bool hasCoroutineMethods = type.Methods.Any(m => 
                    m.ReturnType.FullName.Contains("IEnumerator"));
                    
                bool hasBoolProperties = type.Properties.Any(p => 
                    p.PropertyType.FullName == "System.Boolean");
                    
                // Дополнительная проверка на наличие строковых констант, связанных с паролем
                bool hasPasswordStrings = false;
                foreach (var method in type.Methods)
                {
                    if (method.HasBody)
                    {
                        foreach (var inst in method.Body.Instructions)
                        {
                            if (inst.OpCode == OpCodes.Ldstr && inst.Operand is string str)
                            {
                                string lower = str.ToLowerInvariant();
                                if (lower.Contains("password") || lower.Contains("пароль"))
                                {
                                    hasPasswordStrings = true;
                                    break;
                                }
                            }
                        }
                        if (hasPasswordStrings) break;
                    }
                }
                
                // Если тип удовлетворяет нескольким условиям, вероятно это класс диалога пароля
                if ((hasUIFields && hasCoroutineMethods && hasBoolProperties) || hasPasswordStrings)
                {
                    return type;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Находит свойство, хранящее статус пароля (аналог FOLPAEOEMAF)
        /// </summary>
        private PropertyDefinition FindPasswordProperty(TypeDefinition classType)
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownProperty = classType.Properties
                .FirstOrDefault(p => p.Name == "FOLPAEOEMAF" || p.Name == "LCEMJGAJGAA");
                
            if (knownProperty != null)
                return knownProperty;
                
            // 2. Поиск по характеристикам свойства статуса пароля
            List<PropertyDefinition> candidates = new List<PropertyDefinition>();
            
            // Сначала ищем свойства, которые используются в методах класса
            foreach (var property in classType.Properties)
            {
                // Свойство статуса пароля - это булево свойство с сеттером
                if (property.PropertyType.FullName == "System.Boolean" && property.SetMethod != null)
                {
                    // Проверяем, используется ли свойство в методах класса
                    int usageCount = 0;
                    foreach (var method in classType.Methods)
                    {
                        if (method.HasBody)
                        {
                            foreach (var inst in method.Body.Instructions)
                            {
                                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) && 
                                    inst.Operand is MethodReference mr && 
                                    (mr.Name == property.GetMethod?.Name || mr.Name == property.SetMethod?.Name))
                                {
                                    usageCount++;
                                }
                            }
                        }
                    }
                    
                    // Если свойство часто используется, добавляем его в кандидаты
                    if (usageCount >= 2)
                    {
                        candidates.Add(property);
                    }
                }
            }
            
            // Если нашли кандидатов, возвращаем самое используемое свойство
            if (candidates.Count > 0)
            {
                // Находим свойство, которое чаще всего используется в методах
                PropertyDefinition mostUsedProperty = candidates.First();
                int maxUsage = 0;
                
                foreach (var prop in candidates)
                {
                    int usageCount = 0;
                    foreach (var method in classType.Methods)
                    {
                        if (method.HasBody)
                        {
                            foreach (var inst in method.Body.Instructions)
                            {
                                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) && 
                                    inst.Operand is MethodReference mr && 
                                    (mr.Name == prop.GetMethod?.Name || mr.Name == prop.SetMethod?.Name))
                                {
                                    usageCount++;
                                }
                            }
                        }
                    }
                    
                    if (usageCount > maxUsage)
                    {
                        maxUsage = usageCount;
                        mostUsedProperty = prop;
                    }
                }
                
                return mostUsedProperty;
            }
            
            // 3. Ищем свойство по имени поля, на котором оно основано
            foreach (var property in classType.Properties)
            {
                if (property.PropertyType.FullName == "System.Boolean" && property.SetMethod != null)
                {
                    // Ищем поле, которое используется в свойстве
                    FieldDefinition backingField = null;
                    
                    if (property.GetMethod != null && property.GetMethod.HasBody)
                    {
                        foreach (var inst in property.GetMethod.Body.Instructions)
                        {
                            if (inst.OpCode == OpCodes.Ldfld && inst.Operand is FieldReference fr)
                            {
                                backingField = classType.Fields.FirstOrDefault(f => f.Name == fr.Name);
                                break;
                            }
                        }
                    }
                    
                    // Если нашли поле и его имя связано с паролем, возвращаем свойство
                    if (backingField != null)
                    {
                        string fieldName = backingField.Name.ToLowerInvariant();
                        if (fieldName.Contains("pass") || 
                            fieldName.Contains("pwd") || 
                            fieldName.Contains("secure") || 
                            fieldName.Contains("protect") || 
                            fieldName.Contains("valid"))
                        {
                            return property;
                        }
                    }
                }
            }
            
            // 4. Если не нашли особых признаков, просто берем первое булево свойство с сеттером
            return classType.Properties
                .FirstOrDefault(p => p.PropertyType.FullName == "System.Boolean" && p.SetMethod != null);
        }
        
        /// <summary>
        /// Находит методы, которые используют сеттер свойства пароля
        /// </summary>
        private List<MethodDefinition> FindPasswordSetterMethods(TypeDefinition classType, MethodDefinition setMethod)
        {
            var methods = new List<MethodDefinition>();
            
            foreach (var method in classType.Methods)
            {
                if (method.HasBody)
                {
                    foreach (var inst in method.Body.Instructions)
                    {
                        if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) && 
                            inst.Operand is MethodReference mr && mr.Name == setMethod.Name)
                        {
                            methods.Add(method);
                            break;
                        }
                    }
                }
            }
            
            return methods;
        }
        
        /// <summary>
        /// Проверяет, является ли метод обработчиком кнопки Enter для пароля
        /// </summary>
        private bool IsEnterButtonHandler(MethodDefinition method)
        {
            // 1. Поиск по известному имени (обратная совместимость)
            if (method.Name == "HPIGKCGMGEG")
                return true;
                
            // 2. Проверка характеристик обработчика кнопки Enter
            if (!method.HasBody)
                return false;
                
            // Обработчики обычно имеют проверку на клавишу Enter (KeyCode.Return = 13)
            bool hasEnterKeyCheck = false;
            foreach (var inst in method.Body.Instructions)
            {
                if ((inst.OpCode == OpCodes.Ldc_I4 || inst.OpCode == OpCodes.Ldc_I4_S) && 
                    (inst.Operand is int value && (value == 13 || value == 271)))
                {
                    hasEnterKeyCheck = true;
                    break;
                }
            }
            
            // Проверка доступа к полям ввода (input fields)
            bool hasInputFieldAccess = method.Body.Instructions.Any(i => 
                (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Stfld) && 
                i.Operand is FieldReference fr && 
                (fr.FieldType.FullName.Contains("Input") || fr.Name.ToLowerInvariant().Contains("input")));
                
            return hasEnterKeyCheck || hasInputFieldAccess;
        }
        
        /// <summary>
        /// Находит метод диалога пароля (аналог HAIGGPJCCFI)
        /// </summary>
        private MethodDefinition FindPasswordDialogMethod(TypeDefinition classType)
        {
            // 1. Поиск по известным именам (обратная совместимость)
            var knownMethod = classType.Methods
                .FirstOrDefault(m => m.Name == "HAIGGPJCCFI");
                
            if (knownMethod != null)
            {
                Console.WriteLine($"✅ Метод диалога пароля найден по известному имени: {knownMethod.Name}");
                return knownMethod;
            }
            
            // Применяем несколько стратегий поиска для максимальной точности
            
            // Стратегия 1: Поиск по строковым константам, связанным с паролем
            foreach (var method in classType.Methods)
            {
                if (method.ReturnType.FullName.Contains("IEnumerator") && method.HasBody)
                {
                    // Проверка на наличие строк, связанных с паролем
                    foreach (var inst in method.Body.Instructions)
                    {
                        if (inst.OpCode == OpCodes.Ldstr && inst.Operand is string str)
                        {
                            string lower = str.ToLowerInvariant();
                            if (lower.Contains("password") || 
                                lower.Contains("пароль") ||
                                lower.Contains("защищен") ||
                                lower.Contains("protected") ||
                                lower.Contains("enter") ||
                                lower.Contains("ввод"))
                            {
                                Console.WriteLine($"✅ Метод диалога пароля {method.Name} найден по строковым литералам, связанным с паролем");
                                return method;
                            }
                        }
                    }
                }
            }
            
            // Стратегия 2: Поиск по работе с UI элементами и проверке активности
            foreach (var method in classType.Methods)
            {
                if (method.ReturnType.FullName.Contains("IEnumerator") && method.HasBody)
                {
                    bool hasUIInteraction = method.Body.Instructions.Any(i => 
                        (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Stfld) && 
                        i.Operand is FieldReference fr && 
                        (fr.FieldType.FullName.Contains("Panel") || 
                         fr.FieldType.FullName.Contains("Input") || 
                         fr.FieldType.FullName.Contains("Button") || 
                         fr.FieldType.FullName.Contains("Text")));
                         
                    bool hasActiveCheck = method.Body.Instructions.Any(i => 
                        (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) && 
                        i.Operand is MethodReference mr && 
                        (mr.Name == "SetActive" || mr.Name.Contains("Active") || mr.Name.Contains("Enable")));
                        
                    if (hasUIInteraction && hasActiveCheck)
                    {
                        Console.WriteLine($"✅ Метод диалога пароля {method.Name} найден по работе с UI элементами и проверке активности");
                        return method;
                    }
                }
            }
            
            // Стратегия 3: Поиск по использованию свойства пароля
            PropertyDefinition passwordProperty = FindPasswordProperty(classType);
            if (passwordProperty != null && passwordProperty.GetMethod != null)
            {
                string getterName = passwordProperty.GetMethod.Name;
                foreach (var method in classType.Methods)
                {
                    if (method.ReturnType.FullName.Contains("IEnumerator") && method.HasBody)
                    {
                        bool usesPasswordProperty = method.Body.Instructions.Any(i => 
                            (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) && 
                            i.Operand is MethodReference mr && 
                            mr.Name == getterName);
                            
                        if (usesPasswordProperty)
                        {
                            Console.WriteLine($"✅ Метод диалога пароля {method.Name} найден по использованию свойства пароля");
                            return method;
                        }
                    }
                }
            }
            
            // Стратегия 4: Поиск по характерным инструкциям ожидания (WaitForSeconds)
            foreach (var method in classType.Methods)
            {
                if (method.ReturnType.FullName.Contains("IEnumerator") && method.HasBody)
                {
                    bool hasWaitForSeconds = method.Body.Instructions.Any(i => 
                        (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt || i.OpCode == OpCodes.Newobj) && 
                        i.Operand is MethodReference mr && 
                        (mr.DeclaringType.FullName.Contains("WaitForSeconds") || 
                         mr.Name.Contains("Wait") || 
                         mr.Name.Contains("Yield")));
                        
                    bool hasUIInteraction = method.Body.Instructions.Any(i => 
                        (i.OpCode == OpCodes.Ldfld || i.OpCode == OpCodes.Stfld) && 
                        i.Operand is FieldReference fr && 
                        (fr.FieldType.FullName.Contains("Panel") || 
                         fr.FieldType.FullName.Contains("Input") || 
                         fr.FieldType.FullName.Contains("Button") || 
                         fr.FieldType.FullName.Contains("Text")));
                        
                    if (hasWaitForSeconds && hasUIInteraction)
                    {
                        Console.WriteLine($"✅ Метод диалога пароля {method.Name} найден по характерным инструкциям ожидания и работе с UI");
                        return method;
                    }
                }
            }
            
            // Стратегия 5: Поиск по размеру метода (диалоговые методы обычно большие)
            MethodDefinition largestMethod = null;
            int maxInstructions = 0;
            
            foreach (var method in classType.Methods)
            {
                if (method.ReturnType.FullName.Contains("IEnumerator") && method.HasBody)
                {
                    int instructionCount = method.Body.Instructions.Count;
                    if (instructionCount > maxInstructions)
                    {
                        maxInstructions = instructionCount;
                        largestMethod = method;
                    }
                }
            }
            
            if (largestMethod != null && maxInstructions > 30)
            {
                Console.WriteLine($"✅ Метод диалога пароля {largestMethod.Name} найден как наиболее крупный метод-корутина ({maxInstructions} инструкций)");
                return largestMethod;
            }
            
            // Если не нашли особых признаков, возвращаем первый метод IEnumerator
            var safeMethod = classType.Methods
                .FirstOrDefault(m => m.ReturnType.FullName.Contains("IEnumerator") && m.HasBody);
                      
            if (safeMethod != null)
            {
                Console.WriteLine($"✅ Метод диалога пароля {safeMethod.Name} найден как первый метод IEnumerator");
                return safeMethod;
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверяет, связан ли метод с загрузкой карт
        /// </summary>
        private bool IsMapLoadingRelatedMethod(MethodDefinition method)
        {
            // Проверка имени метода
            string name = method.Name.ToLowerInvariant();
            if (name.Contains("load") || 
                name.Contains("scene") || 
                name.Contains("map") || 
                name.Contains("terrain") || 
                name.Contains("world") || 
                name.Contains("progress") || 
                name.Contains("init") || 
                name.Contains("start") || 
                name == "reset" || 
                name == "ldjiambffdo" || 
                name == "cihnhhdglmi" || 
                name == "nhbkjmndjgo")
            {
                return true;
            }
            
            // Проверка декларирующего типа
            string typeName = method.DeclaringType.Name.ToLowerInvariant();
            if (typeName.Contains("loader") || 
                typeName.Contains("scene") || 
                typeName.Contains("map") || 
                typeName.Contains("terrain") || 
                typeName.Contains("world") || 
                typeName.Contains("progress"))
            {
                return true;
            }
            
            // Проверка инструкций
            if (!method.HasBody)
                return false;
                
            // Проверка на наличие строковых констант, связанных с загрузкой
            foreach (var inst in method.Body.Instructions)
            {
                if (inst.OpCode == OpCodes.Ldstr && inst.Operand is string str)
                {
                    string lower = str.ToLowerInvariant();
                    if (lower.Contains("map") || 
                        lower.Contains("scene") || 
                        lower.Contains("terrain") || 
                        lower.Contains("world") || 
                        lower.Contains("load") || 
                        lower.Contains(".map") || 
                        lower.Contains(".save") || 
                        lower.Contains("prefab") || 
                        lower.Contains("asset"))
                    {
                        return true;
                    }
                }
            }
            
            // Проверка вызовов методов, связанных с загрузкой
            int loadRelatedCalls = 0;
            foreach (var inst in method.Body.Instructions)
            {
                if ((inst.OpCode == OpCodes.Call || inst.OpCode == OpCodes.Callvirt) && 
                    inst.Operand is MethodReference mr)
                {
                    string methodName = mr.Name.ToLowerInvariant();
                    string declaringType = mr.DeclaringType?.FullName?.ToLowerInvariant() ?? "";
                    
                    if (declaringType.Contains("scenemanagement") || 
                        declaringType.Contains("loadscene") || 
                        declaringType.Contains("unityengine.scene") || 
                        methodName.Contains("loadscene") || 
                        methodName.Contains("loadasync") || 
                        methodName.Contains("instantiate"))
                    {
                        loadRelatedCalls++;
                    }
                }
            }
            
            return loadRelatedCalls >= 2;
        }
        
        /// <summary>
        /// Находит метод, возвращающий пустой IEnumerator
        /// </summary>
        private MethodReference FindEmptyEnumeratorMethod()
        {
            // 1. Поиск по известному классу IKPGLDIBCMB
            TypeDefinition ikpgldibcmbType = assembly.MainModule.GetTypes()
                .FirstOrDefault(t => t.Name == "IKPGLDIBCMB");
                
            if (ikpgldibcmbType != null)
            {
                var emptyEnumeratorMethod = ikpgldibcmbType.Methods
                    .FirstOrDefault(m => m.ReturnType.FullName.Contains("IEnumerator"));
                    
                if (emptyEnumeratorMethod != null)
                    return emptyEnumeratorMethod;
            }
            
            // 2. Поиск любого метода IEnumerator без параметров
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods)
                {
                    if (method.ReturnType.FullName.Contains("IEnumerator") && 
                        method.Parameters.Count == 0 && 
                        method.HasBody)
                    {
                        return method;
                    }
                }
            }
            
            return null;
        }
    }
} 