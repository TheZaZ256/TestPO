using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TestPO
{
    public static class ProcessDumper
    {
        // Импорт функции создания дампа из системной библиотеки dbghelp.dll
        [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern bool MiniDumpWriteDump(
            IntPtr hProcess,
            int processId,
            IntPtr hFile,
            int dumpType,
            IntPtr exceptionParam,
            IntPtr userStreamParam,
            IntPtr callbackParam);

        // Константы для настройки доступа и типа дампа
        private const int MiniDumpWithFullMemory = 3; // Число 3 означает полный слепок всей памяти процесса
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// Универсальный метод выгрузки оперативной памяти процесса в файл
        /// </summary>
        /// <param name="pid">Идентификатор целевого процесса (PID)</param>
        /// <param name="outputFilePath">Полный путь к создаваемому файлу дампа</param>
        /// <returns>True, если дамп успешно создан</returns>
        public static bool CreateProcessDump(int pid, string outputFilePath)
        {
            // Открываем процесс в ОС Windows с правами на чтение и опрос информации
            IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);

            if (processHandle == IntPtr.Zero)
            {
                throw new Exception("Не удалось получить доступ к процессу. Убедитесь, что утилита запущена от Администратора.");
            }

            try
            {
                // Создаем бинарный файл на диске
                using (FileStream fs = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    IntPtr fileHandle = fs.SafeFileHandle.DangerousGetHandle();

                    // Вызываем нативную функцию ОС для записи дампа
                    bool success = MiniDumpWriteDump(
                        processHandle,
                        pid,
                        fileHandle,
                        MiniDumpWithFullMemory,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero);

                    return success;
                }
            }
            finally
            {
                // Всегда закрываем открытый дескриптор процесса
                CloseHandle(processHandle);
            }
        }
        /// <summary>
        /// Извлекает из сырого файла дампа все читаемые текстовые строки
        /// </summary>
        /// <param name="dumpPath">Путь к файлу дампа (.dmp)</param>
        /// <param name="outputPath">Путь к сохраняемому текстовому файлу (.txt)</param>
      
        public static void ExtractStringsFromDump(string dumpPath, string outputPath)
        {
            if (!File.Exists(dumpPath)) return;

            HashSet<string> extractedStrings = new HashSet<string>();
            byte[] dumpBytes = File.ReadAllBytes(dumpPath);

            // --- 1. ПАРСЕР ОБЫЧНЫХ СТРОК (ASCII / UTF-8) ---
            StringBuilder sbAscii = new StringBuilder();
            for (int i = 0; i < dumpBytes.Length; i++)
            {
                byte b = dumpBytes[i];
                if (b >= 32 && b <= 126) // Печатные символы
                {
                    sbAscii.Append((char)b);
                }
                else
                {
                    if (sbAscii.Length >= 4) extractedStrings.Add(sbAscii.ToString());
                    sbAscii.Clear();
                }
            }

            // --- 2. ПАРСЕР ДВУХБАЙТОВЫХ СТРОК (Unicode / UTF-16) ---
            // Сканируем с шагом в 2 байта, так как в Юникоде символы парные
            StringBuilder sbUnicode = new StringBuilder();
            for (int i = 0; i < dumpBytes.Length - 1; i += 2)
            {
                byte b1 = dumpBytes[i];
                byte b2 = dumpBytes[i + 1];

                // В UTF-16 для стандартных символов (латиница, кириллица, цифры) 
                // второй байт b2 определяет языковую зону (0 — латиница, 4 — кириллица)
                if ((b2 == 0 && b1 >= 32 && b1 <= 126) || (b2 == 4 && b1 >= 16 && b1 <= 127))
                {
                    // Собираем символ из двух байт
                    char ch = BitConverter.ToChar(dumpBytes, i);
                    sbUnicode.Append(ch);
                }
                else
                {
                    if (sbUnicode.Length >= 4)
                    {
                        extractedStrings.Add(sbUnicode.ToString());
                    }
                    sbUnicode.Clear();
                }
            }

            // Сохраняем все уникальные найденные строки в файл
            File.WriteAllLines(outputPath, extractedStrings, Encoding.UTF8);
        }
    }
}


