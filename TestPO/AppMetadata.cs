using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPO
{
    public class AppMetadata
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

        // Ключевые логические переменные для логики тестов
        public bool HasGui { get; set; } = false;
        public bool IsDotNet { get; set; } = false;
        public bool IsDebugBuild { get; set; } = false;
        public bool RequiresAdmin { get; set; } = false;
        public bool IsUnity { get; set; } = false;


        public static AppMetadata PopulateVariablesFromReport(string reportText, string originalFilePath)
        {
            AppMetadata metadata = new AppMetadata();
            metadata.FilePath = originalFilePath;
            metadata.FileName = Path.GetFileName(originalFilePath);

            // 1. Проверяем наличие GUI (сопоставляем жесткий флаг подсистемы PE и наличие библиотек интерфейса)
            if (reportText.Contains("Windows GUI") ||
                reportText.Contains("System.Windows.Forms") ||
                reportText.Contains("PresentationFramework"))
            {
                // Небольшая защита от ложных срабатываний: если это консольный AppHost от .NET,
                // но сама логика консольная, флаг GUI должен выставляться аккуратно.
                if (reportText.Contains("Windows Console") && !reportText.Contains("System.Windows.Forms"))
                {
                    metadata.HasGui = false;
                }
                else
                {
                    metadata.HasGui = true;
                }
            }

            // 2. Проверяем, написан ли софт на C# (.NET)
            if (reportText.Contains(".NET") ||
                reportText.Contains("mscoree.dll"))
            {
                metadata.IsDotNet = true;
            }

            // 3. Проверяем режим сборки (Debug / Release)
            if (reportText.Contains("ДЕБАГ") ||
                reportText.Contains("Debug Build") ||
                reportText.Contains("MSVCP140D.dll") ||
                reportText.Contains("ucrtbased.dll"))
            {
                metadata.IsDebugBuild = true;
            }

            // 4. Проверяем требования к правам UAC
            if (reportText.Contains("requireAdministrator") ||
                reportText.Contains("Требует права АДМИНИСТРАТОРА") ||
                    reportText.Contains("-Verb RunAs"))
            {
                metadata.RequiresAdmin = true;
            }

            // 5. Проверяем, является ли файл частью игры на Unity
            if (reportText.Contains("Unity Engine") ||
                reportText.Contains("UnityPlayer.dll"))
            {
                metadata.IsUnity = true;
            }

            return metadata;
        }
    }
}
