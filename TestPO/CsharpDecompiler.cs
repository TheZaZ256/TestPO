using System;
using System.IO;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.Metadata;

namespace TestPO
{
    internal class CsharpDecompiler
    {
        public static string DecompileAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath)) return "Файл для декомпиляции не найден.";

            try
            {
                // Настраиваем декомпилятор (отключаем лишние системные вставки для чистоты кода)
                var settings = new DecompilerSettings
                {
                    ThrowOnAssemblyResolveErrors = false,
                    RemoveDeadCode = true
                };

                // Инициализируем движок ILSpy
                var decompiler = new CSharpDecompiler(assemblyPath, settings);

                // Декомпилируем всю сборку целиком в синтаксис C#
                string syntaxTree = decompiler.DecompileWholeModuleAsString();

                return syntaxTree;
            }
            catch (Exception ex)
            {
                return $"[Ошибка декомпиляции C#]: {ex.Message}";
            }
        }
    }
}
