using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace TestPO
{
    public class Analyze
    {
        //Получаеминфу о файле
        public static string GetAllFileInfo(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден.";

            StringBuilder sb = new StringBuilder();
            FileInfo fileInfo = new FileInfo(filePath);

            sb.AppendLine("=== 1. БАЗОВАЯ ИНФОРМАЦИЯ ФАЙЛОВОЙ СИСТЕМЫ ===");
            sb.AppendLine($"Имя файла: {fileInfo.Name}");
            sb.AppendLine($"Путь: {fileInfo.FullName}");
            sb.AppendLine($"Размер: {fileInfo.Length / 1024.0 / 1024.0:F2} МБ ({fileInfo.Length} байт)");
            sb.AppendLine($"Создан: {fileInfo.CreationTime}");
            sb.AppendLine($"Изменен: {fileInfo.LastWriteTime}");
            sb.AppendLine($"Атрибуты: {fileInfo.Attributes}");
            sb.AppendLine();

            sb.AppendLine("=== 2. ВЕРСИЯ И МЕТАДАННЫЕ РАЗРАБОТЧИКА ===");
            try
            {
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(filePath);
                sb.AppendLine($"Компания: {fvi.CompanyName ?? "Нет данных"}");
                sb.AppendLine($"Продукт: {fvi.ProductName ?? "Нет данных"}");
                sb.AppendLine($"Описание: {fvi.FileDescription ?? "Нет данных"}");
                sb.AppendLine($"Версия файла: {fvi.FileVersion ?? "Нет данных"}");
                sb.AppendLine($"Версия продукта: {fvi.ProductVersion ?? "Нет данных"}");
                sb.AppendLine($"Авторские права: {fvi.LegalCopyright ?? "Нет данных"}");
                sb.AppendLine($"Язык: {fvi.Language ?? "Нет данных"}");
                sb.AppendLine($"Является ли Debug-сборкой: {fvi.IsDebug}");
                sb.AppendLine($"Является ли Patched (исправленным): {fvi.IsPatched}");
            }
            catch (Exception ex) { sb.AppendLine($"Ошибка чтения FileVersionInfo: {ex.Message}"); }
            sb.AppendLine();

            sb.AppendLine("=== 3. АНАЛИЗ ТЕХНОЛОГИИ (NATIVE?) ===");
            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(filePath);
                sb.AppendLine($"Тип сборки: Управляемый код (.NET Assembly)");
                sb.AppendLine($"Полное имя сборки: {assemblyName.FullName}");
                sb.AppendLine($"Архитектура процессора (.NET): {assemblyName.ProcessorArchitecture}");
                sb.AppendLine($"Версия CLR: {assemblyName.Version}");
            }
            catch (BadImageFormatException)
            {
                sb.AppendLine("Тип сборки: Нативный код (C++, C, Rust, Go, Delphi и т.д.)");
            }
            catch (Exception ex) { sb.AppendLine($"Ошибка проверки .NET: {ex.Message}"); }
            sb.AppendLine();

            sb.AppendLine("=== 4. АНАЛИЗ PE-ЗАГОЛОВКА (НИЗКОУРОВНЕВЫЙ) ===");
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Проверяем DOS-заголовок (сигнатура "MZ")
                    if (br.ReadUInt16() != 0x5A4D)
                    {
                        sb.AppendLine("Ошибка: Это не валидный исполняемый файл Windows (отсутствует сигнатура MZ).");
                    }
                    else
                    {
                        // Переходим к PE-заголовку
                        fs.Position = 0x3C;
                        uint peOffset = br.ReadUInt32();
                        fs.Position = peOffset;

                        // Проверяем PE-сигнатуру ("PE\0\0")
                        if (br.ReadUInt32() != 0x00004550)
                        {
                            sb.AppendLine("Ошибка: Неверная PE сигнатура.");
                        }
                        else
                        {
                            // Читаем тип архитектуры (Machine)
                            ushort machine = br.ReadUInt16();
                            string architecture = machine switch
                            {
                                0x014c => "x86 (32-bit Intel/AMD)",
                                0x8664 => "x64 (64-bit Intel/AMD)",
                                0xaa64 => "ARM64",
                                0x01c4 => "ARM",
                                _ => $"Неизвестная архитектура (Код: 0x{machine:X})"
                            };
                            sb.AppendLine($"Архитектура файла (Битность): {architecture}");

                            ushort numberOfSections = br.ReadUInt16();
                            sb.AppendLine($"Количество секций в PE: {numberOfSections}");

                            // Пропускаем TimeDateStamp (4), PointerToSymbolTable (4), NumberOfSymbols (4)
                            fs.Position += 12;

                            ushort sizeOfOptionalHeader = br.ReadUInt16();
                            ushort characteristics = br.ReadUInt16();
                            sb.AppendLine($"Файл является DLL: {(characteristics & 0x2000) != 0}");

                            // Читаем Optional Header для определения подсистемы (GUI или Консоль)
                            if (sizeOfOptionalHeader > 0)
                            {
                                ushort magic = br.ReadUInt16(); // 0x10b = PE32, 0x20b = PE32+

                                // Смещение до Subsystem внутри Optional Header зависит от битности
                                fs.Position += (magic == 0x20b) ? 66 : 66; // Для обоих типов смещение подсистемы одинаково относительно начала Optional Header (всего 68 байт от начала)
                                ushort subsystem = br.ReadUInt16();

                                string subsystemType = subsystem switch
                                {
                                    1 => "Native (Драйвер устройства)",
                                    2 => "Windows GUI (Приложение с графическим интерфейсом)",
                                    3 => "Windows Console (Консольное приложение / CUI)",
                                    _ => $"Другая подсистема (Код: {subsystem})"
                                };
                                sb.AppendLine($"Тип интерфейса (Subsystem): {subsystemType}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Ошибка чтения PE-заголовка: {ex.Message}");
            }

            sb.AppendLine();

            sb.AppendLine("=== 5. ЦИФРОВАЯ ПОДПИСЬ (БЕЗОПАСНОСТЬ) ===");
            try
            {
                // Проверяем, подписан ли файл сертификатом разработчика
                X509Certificate signer = X509Certificate.CreateFromSignedFile(filePath);
                X509Certificate2 cert2 = new X509Certificate2(signer);
                sb.AppendLine($"Файл подписан: Да");
                sb.AppendLine($"Субъект подписи: {cert2.Subject}");
                sb.AppendLine($"Кто выдал подпись: {cert2.Issuer}");
                sb.AppendLine($"Действителен с: {cert2.NotBefore}");
                sb.AppendLine($"Действителен до: {cert2.NotAfter}");
                sb.AppendLine($"Серийный номер: {cert2.SerialNumber}");
            }
            catch
            {
                sb.AppendLine("Файл подписан: Нет (ЭЦП отсутствует или повреждена)");
            }

            return sb.ToString();
        }

        public static string DetectLanguageAndGui(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден.";

            string fileContentSample = "";
            List<string> importedDlls = new List<string>();

            // Читаем первые 10 МБ файла в виде строк (для поиска сигнатур DLL и рантаймов)
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader sr = new StreamReader(fs, Encoding.ASCII))
                {
                    char[] buffer = new char[Math.Min(fs.Length, 1024 * 1024 * 10)]; // 10MB лимит
                    sr.Read(buffer, 0, buffer.Length);
                    fileContentSample = new string(buffer);
                }
            }
            catch (Exception ex) { return $"Ошибка чтения внутренностей файла: {ex.Message}"; }

            StringBuilder result = new StringBuilder();
            result.AppendLine("6=== ГЛУБОКИЙ АНАЛИЗ ТЕХНОЛОГИЙ ===");

            // 1. ОПРЕДЕЛЕНИЕ ЯЗЫКА ПРОГРАММИРОВАНИЯ
            string detectedLanguage = "Неизвестный Native код (C, C++, Assembler)";

            if (fileContentSample.Contains("mscoree.dll") || fileContentSample.Contains("_CorExeMain"))
            {
                // Уточняем, C# это или что-то другое в .NET
                if (fileContentSample.Contains("Microsoft.VisualBasic"))
                    detectedLanguage = "VB.NET (.NET)";
                else if (fileContentSample.Contains("FSharp.Core"))
                    detectedLanguage = "F# (.NET)";
                else
                    detectedLanguage = "C# / .NET";
            }
            else if (fileContentSample.Contains("Go build ID") || fileContentSample.Contains("runtime.goexit"))
            {
                detectedLanguage = "Go (Golang)";
            }
            else if (fileContentSample.Contains("rustc version") || fileContentSample.Contains("std::rt::lang_start"))
            {
                detectedLanguage = "Rust";
            }
            else if (fileContentSample.Contains("pydata") || fileContentSample.Contains("Py_Initialize") || fileContentSample.Contains("failed to write all bytes for cryptext"))
            {
                detectedLanguage = "Python (Упакован в EXE через PyInstaller/cx_Freeze)";
            }
            else if (fileContentSample.Contains("VCL") || fileContentSample.Contains("TApplication") || fileContentSample.Contains("Borland"))
            {
                detectedLanguage = "Delphi / Object Pascal";
            }

            result.AppendLine($"Язык разработки: {detectedLanguage}");

            // 2. ОПРЕДЕЛЕНИЕ GUI ФРЕЙМВОРКА (ИНТЕРФЕЙСА)
            string detectedGui = "Не удалось определить фреймворк интерфейса";

            // Проверяем, не консоль ли это изначально (по коду из прошлого шага, либо грубо по маркерам)
            if (fileContentSample.Contains("Windows Console") || fileContentSample.Contains("Standard input redirect"))
            {
                detectedGui = "Консольное приложение (CUI)";
            }

            // Если это .NET, уточняем GUI
            if (detectedLanguage.Contains(".NET"))
            {
                if (fileContentSample.Contains("System.Windows.Forms"))
                    detectedGui = "Windows Forms (Стандартный WinForms)";
                else if (fileContentSample.Contains("PresentationFramework") || fileContentSample.Contains("System.Windows.Markup"))
                    detectedGui = "WPF (Windows Presentation Foundation)";
                else if (fileContentSample.Contains("Avalonia.Controls"))
                    detectedGui = "Avalonia UI (Кроссплатформенный .NET)";
            }
            else // Если нативный код
            {
                if (fileContentSample.Contains("Qt5Core.dll") || fileContentSample.Contains("Qt6Core.dll") || fileContentSample.Contains("qt_plugin_instance"))
                    detectedGui = "Qt Framework (C++)";
                else if (fileContentSample.Contains("chrome_elf.dll") || fileContentSample.Contains("ffmpeg.dll") || fileContentSample.Contains("resources.pak"))
                    detectedGui = "Electron / NW.js (JavaScript/HTML/CSS приложение в оболочке Chromium)";
                else if (fileContentSample.Contains("TApplication") || fileContentSample.Contains("TForm"))
                    detectedGui = "VCL Forms (Delphi)";
                else if (fileContentSample.Contains("USER32.dll") && fileContentSample.Contains("CreateWindowEx"))
                    detectedGui = "Pure Win32 API GUI (С++ / Чистый Windows)";
            }

            result.AppendLine($"Технология GUI: {detectedGui}");

            return result.ToString();
        }

        public static string AnalyzeRealApplication(string exePath)
        {
            if (!File.Exists(exePath)) return "Файл не найден";

            StringBuilder sb = new StringBuilder();
            string directory = Path.GetDirectoryName(exePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(exePath);

            // Главный секрет .NET Core / 5 / 6 / 7 / 8 / 9:
            // Рядом с .exe ВСЕГДА лежит .dll с тем же именем и реальным кодом.
            string matchingDll = Path.Combine(directory, fileNameWithoutExt + ".dll");
            string runtimeConfig = Path.Combine(directory, fileNameWithoutExt + ".runtimeconfig.json");

            sb.AppendLine("7=== РЕЗУЛЬТАТЫ СВЕРХГЛУБОКОГО АНАЛИЗА ===");

            // Шаг 1. Детектируем современный .NET (Core / 5 / 6 / 7 / 8 / 9)
            if (File.Exists(matchingDll) && File.Exists(runtimeConfig))
            {
                sb.AppendLine("Язык разработки: C# (.NET Core / Современный .NET)");

                // Читаем версию .NET из файла конфигурации рантайма
                try
                {
                    string configText = File.ReadAllText(runtimeConfig);
                    if (configText.Contains("\"version\": \"8.0")) sb.AppendLine("Платформа: .NET 8.0");
                    else if (configText.Contains("\"version\": \"9.0")) sb.AppendLine("Платформа: .NET 9.0");
                    else if (configText.Contains("\"version\": \"6.0")) sb.AppendLine("Платформа: .NET 6.0");
                    else sb.AppendLine("Платформа: .NET Core / .NET 5+");
                }
                catch { sb.AppendLine("Платформа: Современный .NET (управляется через DLL)"); }

                // Анализируем настоящую DLL сборку
                try
                {
                    // Загружаем только метаданные без выполнения кода (безопасно)
                    AssemblyName assemblyName = AssemblyName.GetAssemblyName(matchingDll);
                    sb.AppendLine($"Полное имя сборки: {assemblyName.Name}");
                    sb.AppendLine($"Версия логики: {assemblyName.Version}");
                    sb.AppendLine($"Архитектура сборки: {assemblyName.ProcessorArchitecture}");

                    // Проверяем интерфейс по зависимостям DLL
                    string dllContent = File.ReadAllText(matchingDll, Encoding.ASCII);
                    if (dllContent.Contains("System.Windows.Forms"))
                    {
                        sb.AppendLine("Технология GUI: Windows Forms (WinForms)");
                    }
                    else if (dllContent.Contains("PresentationFramework"))
                    {
                        sb.AppendLine("Технология GUI: WPF (Windows Presentation Foundation)");
                    }
                    else
                    {
                        sb.AppendLine("Технология GUI: Не определена (Возможно, консоль или библиотека)");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"Ошибка чтения метаданных .NET: {ex.Message}");
                }
            }
            else
            {
                // Шаг 2. Если это старый .NET Framework или честный Native код
                sb.AppendLine("Язык разработки: Нативный код (C++, C, Rust, Go)");

                // Запускаем старый статический анализ по строкам (из предыдущего ответа)
                // ...
            }

            return sb.ToString();
        }

        public static string AnalyzeFile(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("8=== ТОЧНОЕ ОПРЕДЕЛЕНИЕ ТЕХНОЛОГИИ ===");

            try
            {
                // Читаем весь файл в память как массив байт (или кусок, если файл огромный)
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Переводим байты в две разные кодировки, так как Microsoft пишет данные и в ASCII, и в Unicode
                string asciiString = Encoding.ASCII.GetString(fileBytes);
                string unicodeString = Encoding.Unicode.GetString(fileBytes);

                // 1. Ищем следы современного .NET Core / 5 / 6 / 7 / 8 / 9
                bool isModernDotNet = false;
                string dotNetVersion = "Не определена";

                // Поиск во встроенном манифесте рантайма (работает даже для Single-File)
                if (asciiString.Contains(".NETCoreApp") || unicodeString.Contains(".NETCoreApp"))
                {
                    isModernDotNet = true;

                    // Пробуем вытащить точную версию (например, 8.0) регулярным выражением
                    var match = Regex.Match(asciiString, @"\.NETCoreApp,Version=v(\d+\.\d+)");
                    if (!match.Success) match = Regex.Match(unicodeString, @"\.NETCoreApp,Version=v(\d+\.\d+)");

                    if (match.Success)
                    {
                        dotNetVersion = match.Groups[1].Value;
                    }
                }

                // 2. Выводим точный вердикт
                if (isModernDotNet)
                {
                    sb.AppendLine("Язык разработки: C# (.NET Core)");
                    sb.AppendLine($"Версия платформы: .NET {dotNetVersion}");

                    // Определяем GUI по ссылкам на фреймворки внутри бинарника
                    if (asciiString.Contains("System.Windows.Forms") || unicodeString.Contains("System.Windows.Forms"))
                    {
                        sb.AppendLine("Технология GUI: Windows Forms (WinForms)");
                    }
                    else if (asciiString.Contains("PresentationFramework") || unicodeString.Contains("PresentationFramework"))
                    {
                        sb.AppendLine("Технология GUI: WPF (Windows Presentation Foundation)");
                    }
                    else
                    {
                        sb.AppendLine("Технология GUI: Console / Class Library (Без графического интерфейса)");
                    }
                }
                else
                {
                    // Проверка на старый .NET Framework 4.x
                    if (asciiString.Contains("clr.dll") || asciiString.Contains("mscoree.dll"))
                    {
                        sb.AppendLine("Язык разработки: C# / VB.NET");
                        sb.AppendLine("Платформа: Старый .NET Framework (4.x и ниже)");
                    }
                    else
                    {
                        sb.AppendLine("Язык разработки: Нативный код (C++ / C / Rust / Go)");
                        sb.AppendLine("Платформа: Win32/Win64 Native");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Ошибка при глубоком сканировании: {ex.Message}");
            }

            return sb.ToString();
        }

        // 1. ПРОВЕРКА UAC МАНИФЕСТА
        public static string CheckUacManifest(string filePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                string content = Encoding.UTF8.GetString(bytes);

                if (content.Contains("requestedExecutionLevel"))
                {
                    var match = Regex.Match(content, @"level=[""']([^""']+)[""']");
                    if (match.Success)
                    {
                        string level = match.Groups[1].Value;
                        return level.Equals("requireAdministrator", StringComparison.OrdinalIgnoreCase)
                            ? "🚨 Требует права АДМИНИСТРАТОРА (requireAdministrator)"
                            : $"Обычные права ({level})";
                    }
                }
                return "Обычные права (asInvoker / манифест отсутствует)";
            }
            catch (Exception ex) { return $"Ошибка чтения манифеста: {ex.Message}"; }
        }

        // 2. ВЫЗОВ WINDOWS DEFENDER
        public static string ScanWithWindowsDefender(string filePath)
        {
            // Дефолтный путь к утилите Защитника в Windows 10/11
            string defenderPath = @"C:\Program Files\Windows Defender\MpCmdRun.exe";

            if (!File.Exists(defenderPath))
                return "Windows Defender (MpCmdRun.exe) не найден по стандартному пути.";

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = defenderPath,
                    // Аргументы: -Scan (сканировать), -ScanType 3 (конкретный файл), -File (путь)
                    Arguments = $"-Scan -ScanType 3 -File \"{filePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };

                using (Process process = Process.Start(psi))
                {
                    process.WaitForExit(15000); // Таймаут 15 секунд

                    return process.ExitCode switch
                    {
                        0 => "✅ Чист (Угроз не обнаружено)",
                        1 => "❌ ВНИМАНИЕ: Обнаружена угроза или вредоносный код!",
                        2 => "⚠️ Ошибка: Сканирование заблокировано или прервано.",
                        _ => $"Защитник вернул неизвестный код: {process.ExitCode}"
                    };
                }
            }
            catch (Exception ex) { return $"Не удалось запустить сканирование: {ex.Message}"; }
        }

        // 3. СБОР ХАРДКОРНЫХ СТРОК (STRINGS DUMP)
        public static List<string> ExtractHardcodedStrings(string filePath, int minLength = 5)
        {
            List<string> foundStrings = new List<string>();
            if (!File.Exists(filePath)) return foundStrings;

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Ищем ASCII и UTF-8 печатные строки
                StringBuilder currentString = new StringBuilder();
                foreach (byte b in fileBytes)
                {
                    // Печатные ASCII символы от Пробела (32) до ~ (126)
                    if (b >= 32 && b <= 126)
                    {
                        currentString.Append((char)b);
                    }
                    else
                    {
                        if (currentString.Length >= minLength)
                        {
                            string str = currentString.ToString().Trim();
                            // Фильтруем мусор, оставляя только строки с полезным контекстом (пути, url, конфиги)
                            if (IsInterestingString(str)) foundStrings.Add(str);
                        }
                        currentString.Clear();
                    }
                }
            }
            catch { }

            return foundStrings;
        }

        private static bool IsInterestingString(string str)
        {
            // Оставляем строки, похожие на пути к исходникам, URL, вызовы API или ошибки
            if (str.Contains(":\\") || str.Contains("/") || str.Contains(".dll") ||
                str.Contains("Error") || str.Contains("Exception") || str.Contains("http"))
            {
                return true;
            }
            return false;
        }

        public static string SpeculateAllTech(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("9=== ГЛУБОКИЙ АНАЛИЗ ПЛАТФОРМЫ И ДВИЖКА ===");

            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                string utf8 = Encoding.UTF8.GetString(fileBytes);
                string ascii = Encoding.ASCII.GetString(fileBytes);

                // 1. ДЕТЕКЦИЯ ЯЗЫКА ПРОГРАММИРОВАНИЯ
                string language = "Native (C / C++ / Assembler)";

                if (ascii.Contains("mscoree.dll") || ascii.Contains("_CorExeMain") || utf8.Contains(".NETCoreApp"))
                {
                    language = "C# / F# / VB.NET (.NET)";
                }
                else if (ascii.Contains("Go build ID") || ascii.Contains("runtime.goexit"))
                {
                    language = "Go (Golang)";
                }
                else if (ascii.Contains("rustc version") || ascii.Contains("std::rt::lang_start"))
                {
                    language = "Rust";
                }
                else if (ascii.Contains("pydata") || ascii.Contains("Py_Initialize"))
                {
                    language = "Python (Скомпилирован в EXE)";
                }
                else if (ascii.Contains("VCL") || ascii.Contains("Borland") || ascii.Contains("PACKAGE INFO"))
                {
                    language = "Delphi (Object Pascal)";
                }
                sb.AppendLine($"Язык разработки: {language}");

                // 2. ДЕТЕКЦИЯ ИГРОВЫХ И ГРАФИЧЕСКИХ ДВИЖКОВ (ENGINES)
                string engine = "Не используется (Обычное приложение)";

                if (ascii.Contains("UnityPlayer.dll") || ascii.Contains("MonoBleedingEdge") || utf8.Contains("UnityEngine"))
                {
                    engine = "⚡ Unity Engine";
                }
                else if (ascii.Contains("UnrealEngine") || ascii.Contains("PROJECT_STREAMING_PAGES"))
                {
                    engine = "🔥 Unreal Engine";
                }
                else if (ascii.Contains("GODOT") || ascii.Contains("godot_") || ascii.Contains("GDScript"))
                {
                    engine = "🤖 Godot Engine";
                }
                else if (ascii.Contains("CryEngine") || ascii.Contains("CRYENGINE"))
                {
                    engine = "❄️ CryEngine";
                }
                sb.AppendLine($"Игровой движок: {engine}");

                // 3. ДЕТЕКЦИЯ GUI ФРЕЙМВОРКОВ И ТЕХНОЛОГИЙ
                string framework = "Чистый Win32 API / Консоль";

                if (language.Contains(".NET"))
                {
                    if (ascii.Contains("System.Windows.Forms") || utf8.Contains("System.Windows.Forms")) framework = "Windows Forms (WinForms)";
                    else if (ascii.Contains("PresentationFramework")) framework = "WPF (Windows Presentation Foundation)";
                    else if (ascii.Contains("Avalonia.")) framework = "Avalonia UI";
                    else if (ascii.Contains("Maui")) framework = "NET MAUI";
                }
                else
                {
                    if (ascii.Contains("Qt5Core.dll") || ascii.Contains("Qt6Core.dll") || ascii.Contains("qt_plugin")) framework = "Qt Framework (C++)";
                    else if (ascii.Contains("chrome_elf.dll") || ascii.Contains("ffmpeg.dll") || ascii.Contains("node.dll")) framework = "Electron / NW.js (JavaScript/HTML)";
                    else if (ascii.Contains("wxmsw")) framework = "wxWidgets";
                    else if (ascii.Contains("TApplication") || ascii.Contains("TForm")) framework = "VCL (Delphi)";
                    else if (ascii.Contains("mfc") || ascii.Contains("mfc42") || ascii.Contains("mfc140")) framework = "MFC (Microsoft Foundation Classes)";
                }
                sb.AppendLine($"Фреймворк интерфейса: {framework}");

                // 4. ДОПОЛНИТЕЛЬНЫЙ АНАЛИЗ ЗАВИСИМОСТЕЙ И СБОРОК
                sb.AppendLine("\n10=== АНАЛИЗ СТАТУСА СБОРКИ ===");

                // Проверка: это Debug или Release?
                if (ascii.Contains("MSVCP140D.dll") || ascii.Contains("VCRUNTIME140D.dll") || ascii.Contains("ucrtbased.dll"))
                {
                    sb.AppendLine("Тип сборки: ДЕБАГ (Debug Build — содержит отладочные символы и ассерты)");
                }
                else
                {
                    sb.AppendLine("Тип сборки: РЕЛИЗ (Release Build — оптимизированный код)");
                }

                // Проверка упаковщиков (Packers / Protectors)
                if (ascii.Contains("UPX0") || ascii.Contains("UPX1") || ascii.Contains("UPX!"))
                {
                    sb.AppendLine("⚠️ Внимание: Файл упакован сжатием UPX! Настоящие строки скрыты.");
                }
                else if (ascii.Contains("VMProtect"))
                {
                    sb.AppendLine("⚠️ Внимание: Файл защищен VMProtect. Статический анализ сильно ограничен.");
                }
                else
                {
                    sb.AppendLine("Упаковка/Защита: Не обнаружена (Файл открыт для анализа)");
                }

            }
            catch (Exception ex)
            {
                sb.AppendLine($"Ошибка детального анализа: {ex.Message}");
            }

            return sb.ToString();
        }

        public static string GetHardcorePeInfo(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("11=== ГЛУБОКИЙ СТРУКТУРНЫЙ АНАЛИЗ PE ===");

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // 1. Проверяем сигнатуру DOS (MZ)
                    if (br.ReadUInt16() != 0x5A4D) return "Файл не является валидным исполняемым файлом Windows.";

                    // 2. Идем по смещению 0x3C, чтобы узнать, где начинается заголовок PE
                    fs.Position = 0x3C;
                    uint peHeaderOffset = br.ReadUInt32();

                    // 3. Переходим к PE заголовку
                    fs.Position = peHeaderOffset;
                    if (br.ReadUInt32() != 0x00004550) return "Неверная сигнатура PE заголовка.";

                    // Мы в COFF File Header
                    fs.Position += 4; // Пропускаем тип процессора (Machine) и количество секций (NumberOfSections)

                    // 4. ЧИТАЕМ НАСТОЯЩУЮ ДАТУ КОМПИЛЯЦИИ (TimeDateStamp)
                    uint timeDateStamp = br.ReadUInt32();
                    // Время хранится в формате Unix Timestamp (секунды с 1 января 1970 года)
                    DateTime compileTime = DateTime.UnixEpoch.AddSeconds(timeDateStamp).ToLocalTime();

                    sb.AppendLine($"Истинная дата компиляции: {compileTime} (Вшито в заголовок компилятором)");

                    // Пропускаем PointerToSymbolTable (4) и NumberOfSymbols (4)
                    fs.Position += 8;

                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    ushort characteristics = br.ReadUInt16(); // Характеристики файла

                    // 5. ЧИТАЕМ ФЛАГИ БЕЗОПАСНОСТИ (Из Optional Header)
                    if (sizeOfOptionalHeader > 0)
                    {
                        ushort magic = br.ReadUInt16(); // 0x10B = PE32 (32bit), 0x20B = PE32+ (64bit)

                        // Смещение до поля DllCharacteristics внутри Optional Header:
                        // Для 32-бит это 70-й байт, для 64-бит — 66-й байт от начала Optional Header
                        int dllCharacteristicsOffset = (magic == 0x20B) ? 66 : 70;
                        fs.Position = peHeaderOffset + 4 + 20 + dllCharacteristicsOffset;

                        ushort dllCharacteristics = br.ReadUInt16();

                        // Маски флагов безопасности Windows
                        bool hasDynamicBase = (dllCharacteristics & 0x0040) != 0; // ASLR
                        bool hasNxCompat = (dllCharacteristics & 0x0100) != 0;    // DEP/NX
                        bool isAppContainer = (dllCharacteristics & 0x1000) != 0; // AppContainer Isolation

                        sb.AppendLine($"Защита ASLR (Рандомизация памяти): {(hasDynamicBase ? "ПОДДЕРЖИВАЕТСЯ" : "ОТСУТСТВУЕТ (Уязвимо)")}");
                        sb.AppendLine($"Защита DEP/NX (Запрет выполнения данных): {(hasNxCompat ? "ПОДДЕРЖИВАЕТСЯ" : "ОТСУТСТВУЕТ (Уязвимо)")}");
                        sb.AppendLine($"Изоляция AppContainer: {(isAppContainer ? "ДА (Песочница)" : "НЕТ (Обычный запуск)")}");
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Ошибка парсинга заголовков: {ex.Message}");
            }

            return sb.ToString();
        }

        public static string ParseImports(string filePath)
        {
            if (!File.Exists(filePath)) return "Файл не найден";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("12=== ТАБЛИЦА ИМПОРТИРУЕМЫХ ФУНКЦИЙ (IAT) ===");

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // Проверяем сигнатуру MZ
                    if (br.ReadUInt16() != 0x5A4D) return "Невалидный EXE";

                    // Переходим к PE заголовку
                    fs.Position = 0x3C;
                    uint peOffset = br.ReadUInt32();
                    fs.Position = peOffset;

                    if (br.ReadUInt32() != 0x00004550) return "Невалидный PE";

                    // Читаем COFF Header
                    ushort machine = br.ReadUInt16();
                    ushort numberOfSections = br.ReadUInt16();
                    fs.Position += 12; // Пропускаем TimeDateStamp и символы
                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    fs.Position += 2; // Пропускаем Characteristics

                    // Проверяем битность (0x10B = x86, 0x20B = x64)
                    ushort magic = br.ReadUInt16();
                    bool is64Bit = (magic == 0x20B);

                    // Находим смещение Data Directories
                    int rvaOffset = is64Bit ? 112 : 92;
                    fs.Position = peOffset + 4 + 20 + rvaOffset;

                    fs.Position += 8; // Пропускаем Export Directory
                    uint importDirectoryRva = br.ReadUInt32(); // RVA таблицы импорта
                    uint importDirectorySize = br.ReadUInt32();

                    if (importDirectoryRva == 0) return "Таблица импорта отсутствует.";

                    // Читаем заголовки секций в структуру для быстрого поиска
                    fs.Position = peOffset + 4 + 20 + sizeOfOptionalHeader;
                    var sections = new List<PeSection>();

                    for (int i = 0; i < numberOfSections; i++)
                    {
                        fs.Position += 8; // Пропускаем имя секции
                        uint virtualSize = br.ReadUInt32();
                        uint virtualAddress = br.ReadUInt32();
                        uint sizeOfRawData = br.ReadUInt32();
                        uint pointerToRawData = br.ReadUInt32();
                        fs.Position += 16; // Пропускаем остальные поля

                        sections.Add(new PeSection
                        {
                            VirtualAddress = virtualAddress,
                            VirtualSize = virtualSize,
                            PointerToRawData = pointerToRawData
                        });
                    }

                    // Переводим RVA самой таблицы импорта в физическое смещение
                    uint importTableRawOffset = RvaToOffset(importDirectoryRva, sections);
                    if (importTableRawOffset == 0) return "Не удалось сопоставить секцию импорта.";

                    fs.Position = importTableRawOffset;

                    while (true)
                    {
                        uint originalFirstThunk = br.ReadUInt32();
                        uint timeDateStamp = br.ReadUInt32();
                        uint forwarderChain = br.ReadUInt32();
                        uint nameRva = br.ReadUInt32();
                        uint firstThunk = br.ReadUInt32();

                        if (originalFirstThunk == 0 && nameRva == 0 && firstThunk == 0) break;

                        long nextDllPosition = fs.Position;

                        // ИСПОЛЬЗУЕМ УНИВЕРСАЛЬНЫЙ ПЕРЕВОД ДЛЯ ИМЕНИ DLL
                        uint dllNameRaw = RvaToOffset(nameRva, sections);
                        if (dllNameRaw == 0) continue;

                        fs.Position = dllNameRaw;
                        string dllName = "";
                        char c;
                        while ((c = (char)br.ReadByte()) != '\0') dllName += c;

                        sb.AppendLine($"\n📦 Зависимость: {dllName}");

                        uint thunkRva = originalFirstThunk != 0 ? originalFirstThunk : firstThunk;
                        // ИСПОЛЬЗУЕМ УНИВЕРСАЛЬНЫЙ ПЕРЕВОД ДЛЯ ТАБЛИЦЫ ФУНКЦИЙ
                        uint thunkRaw = RvaToOffset(thunkRva, sections);
                        if (thunkRaw == 0) continue;

                        fs.Position = thunkRaw;
                        int functionCount = 0;

                        while (true)
                        {
                            ulong thunkValue = is64Bit ? br.ReadUInt64() : br.ReadUInt32();
                            if (thunkValue == 0) break;

                            bool importByOrdinal = is64Bit
                                ? (thunkValue & 0x8000000000000000) != 0
                                : (thunkValue & 0x80000000) != 0;

                            if (!importByOrdinal)
                            {
                                // ИСПОЛЬЗУЕМ УНИВЕРСАЛЬНЫЙ ПЕРЕВОД ДЛЯ ИМЕНИ ФУНКЦИИ
                                uint funcNameRaw = RvaToOffset((uint)thunkValue, sections);
                                if (funcNameRaw != 0)
                                {
                                    funcNameRaw += 2; // Пропускаем Hint
                                    long currentThunkPos = fs.Position;

                                    fs.Position = funcNameRaw;
                                    string funcName = "";
                                    char fc;
                                    while ((fc = (char)br.ReadByte()) != '\0') funcName += fc;

                                    if (!string.IsNullOrEmpty(funcName) && functionCount < 10)
                                    {
                                        sb.AppendLine($"  ├── f() {funcName}");
                                        functionCount++;
                                    }
                                    fs.Position = currentThunkPos;
                                }
                            }
                        }

                        if (functionCount >= 20) sb.AppendLine("  └── ... (и другие функции)");

                        fs.Position = nextDllPosition;
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine($"Ошибка чтения таблицы импорта: {ex.Message}");
            }

            return sb.ToString();
        }

        // Вспомогательная функция для корректного перевода адресов
        private static uint RvaToOffset(uint rva, List<PeSection> sections)
        {
            foreach (var section in sections)
            {
                if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize)
                {
                    return section.PointerToRawData + (rva - section.VirtualAddress);
                }
            }
            return 0;
        }

        // Структура для хранения информации о секциях
        private struct PeSection
        {
            public uint VirtualAddress;
            public uint VirtualSize;
            public uint PointerToRawData;
        }

        public static List<string> ScanRdataStrings(string filePath)
        {
            List<string> foundStrings = new List<string>();
            if (!File.Exists(filePath)) return foundStrings;

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // --- 1. ПАРСИНГ ЗАГОЛОВКА ДЛЯ ПОИСКА СЕКЦИИ ---
                    if (br.ReadUInt16() != 0x5A4D) return foundStrings; // Сигнатура MZ

                    fs.Position = 0x3C;
                    uint peOffset = br.ReadUInt32();
                    fs.Position = peOffset;

                    if (br.ReadUInt32() != 0x00004550) return foundStrings; // Сигнатура PE

                    ushort machine = br.ReadUInt16();
                    ushort numberOfSections = br.ReadUInt16();
                    fs.Position += 12; // Пропускаем TimeDateStamp и символы
                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    fs.Position += 2; // Пропускаем Characteristics

                    // Прыгаем сразу к началу таблицы секций
                    fs.Position = peOffset + 4 + 20 + sizeOfOptionalHeader;

                    uint rdataRawOffset = 0;
                    uint rdataRawSize = 0;

                    // Перебираем секции в поисках .rdata или .rodata
                    for (int i = 0; i < numberOfSections; i++)
                    {
                        byte[] nameBytes = br.ReadBytes(8);
                        string sectionName = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0').ToLower();

                        uint virtualSize = br.ReadUInt32();
                        uint virtualAddress = br.ReadUInt32();
                        uint sizeOfRawData = br.ReadUInt32();
                        uint pointerToRawData = br.ReadUInt32();
                        fs.Position += 16; // Пропускаем остальное

                        if (sectionName == ".rdata" || sectionName == ".rodata")
                        {
                            rdataRawOffset = pointerToRawData;
                            rdataRawSize = sizeOfRawData;
                            break;
                        }
                    }

                    if (rdataRawOffset == 0 || rdataRawSize == 0)
                    {
                        // Если секцию .rdata не нашли, часто строки лежат в общей секции .data
                        return foundStrings;
                    }

                    // --- 2. СКАНИРОВАНИЕ СТРОК (АЛГОРИТМ STRINGS) ---
                    fs.Position = rdataRawOffset;
                    byte[] sectionData = br.ReadBytes((int)rdataRawSize);

                    // Регистрируем провайдер кодировок (нужно для работы с Windows-1251 в современном .NET)
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    Encoding win1251 = Encoding.GetEncoding(1251);

                    // --- 1. Поиск однобайтовых строк (English ASCII + Русский Win-1251) ---
                    StringBuilder ansiSb = new StringBuilder();
                    for (int i = 0; i < sectionData.Length; i++)
                    {
                        byte b = sectionData[i];

                        // Проверяем: это печатный символ английского ASCII (32-126) 
                        // ИЛИ русского алфавита в кодировке Windows-1251 (192-255, плюс буквы Ё/ё)
                        if ((b >= 32 && b <= 126) || (b >= 192 && b <= 255) || b == 168 || b == 184)
                        {
                            ansiSb.Append((char)b);
                        }
                        else
                        {
                            if (ansiSb.Length >= 4)
                            {
                                // Корректно декодируем байты обратно в читаемый текст
                                byte[] stringBytes = win1251.GetBytes(ansiSb.ToString());
                                string decodedString = win1251.GetString(stringBytes);

                                foundStrings.Add($"[ANSI/Win-1251] {decodedString}");
                            }
                            ansiSb.Clear();
                        }
                    }

                    // --- 2. Поиск двухбайтовых строк (Unicode / UTF-16) ---
                    StringBuilder uniSb = new StringBuilder();
                    for (int i = 0; i < sectionData.Length - 1; i += 2)
                    {
                        ushort u = BitConverter.ToUInt16(sectionData, i);

                        // Проверяем: английский Unicode ИЛИ диапазон кириллицы в Unicode (0x0400 - 0x04FF)
                        if ((u >= 32 && u <= 126) || (u >= 0x0400 && u <= 0x04FF))
                        {
                            uniSb.Append((char)u);
                        }
                        else
                        {
                            if (uniSb.Length >= 4)
                            {
                                foundStrings.Add($"[Unicode] {uniSb.ToString()}");
                            }
                            uniSb.Clear();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                foundStrings.Add($"Ошибка парсинга секции: {ex.Message}");
            }

            return foundStrings;
        }


        public static string Create_info_about_po(string filePath, bool defender)
        {
            StringBuilder finalReport = new StringBuilder();

            string analyzed_info_po1 = Analyze.GetAllFileInfo(filePath);
            string analyzed_info_po2 = Analyze.DetectLanguageAndGui(filePath);
            string analyzed_info_po3 = Analyze.AnalyzeRealApplication(filePath);
            string analyzed_info_po4 = Analyze.AnalyzeFile(filePath);
            string analyzed_info_po5 = Analyze.CheckUacManifest(filePath);


            finalReport.AppendLine(analyzed_info_po1);
            finalReport.AppendLine(analyzed_info_po2);
            finalReport.AppendLine(analyzed_info_po3);
            finalReport.AppendLine(analyzed_info_po4);
            finalReport.AppendLine(analyzed_info_po5);


            if (defender == true)
            {
                string analyzed_info_po6 = Analyze.ScanWithWindowsDefender(filePath);

                finalReport.AppendLine(analyzed_info_po6);
            }


            List<string> extractedStrings = Analyze.ExtractHardcodedStrings(filePath, 6);
            finalReport.AppendLine(Environment.NewLine + "ЗАШИТЫЕ СТРОКИ:");

            if (extractedStrings.Count > 0)
            {
                foreach (string s in extractedStrings)
                {
                    finalReport.AppendLine(Environment.NewLine + $"  > {s}");
                }
            }
            else
            {
                finalReport.AppendLine(Environment.NewLine + "  Интересных зашитых строк не обнаружено.");
            }

            string analyzed_info_po7 = Analyze.SpeculateAllTech(filePath);
            string analyzed_info_po8 = Analyze.GetHardcorePeInfo(filePath);
            string analyzed_info_po9 = Analyze.ParseImports(filePath);

            finalReport.AppendLine(analyzed_info_po7);
            finalReport.AppendLine(analyzed_info_po8);
            finalReport.AppendLine(analyzed_info_po9);

            List<string> extractedStringsErros = Analyze.ScanRdataStrings(filePath);

            finalReport.AppendLine(Environment.NewLine + "  Просканнированые возможные ошибки:");

            if (extractedStrings.Count > 0)
            {
                foreach (string s in extractedStrings)
                {
                    finalReport.AppendLine(Environment.NewLine + $"  > {s}");
                }
            }
            else
            {
                finalReport.AppendLine(Environment.NewLine + "  Возможных Ошибок не найдено");
            }


            return finalReport.ToString();
        }




    }

}

