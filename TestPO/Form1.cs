using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management; // Если подчеркивает красным,  Alt+Enter -> Добавить ссылку на System.Management
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TestPO
{

    /// <summary>
    /// Получить доступ ко всем элементам GUI
    /// сделать возможность загрузить из текстового файла команды + режим ожидания межджу командами для консоли
    /// </summary>
    public partial class TestPO : Form
    {
        // --- БЛОК СТРУКТУР И КОНСТАНТ WIN32 ---
        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CONSOLE_SCREEN_BUFFER_INFO
        {
            public COORD dwSize;
            public COORD dwCursorPosition;
            public ushort wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private const int SW_RESTORE = 9;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // --- ИМПОРТЫ ИЗ USER32.DLL ---
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);


        // --- ИМПОРТЫ ИЗ KERNEL32.DLL ---
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId); // Изменен тип на int для стабильности PID

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleScreenBufferInfo(IntPtr hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO lpConsoleScreenBufferInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput, [Out] StringBuilder lpCharacter, uint nLength, COORD dwReadCoord, out uint lpNumberOfCharsRead);

        // 1. Импортируем функцию из системной библиотеки user32.dll для управления окнами Windows
       

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd); // Проверяет, свернуто ли окно

        private IntPtr FindWindowByProcessId(int processId)
        {
            IntPtr foundHandle = IntPtr.Zero;

            // Запускаем перебор всех окон в операционной системе Windows
            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                uint windowPid;
                GetWindowThreadProcessId(hWnd, out windowPid);

                if (windowPid == processId)
                {
                    // Проверяем, что окно физически отрисовано на экране
                    if (IsWindowVisible(hWnd))
                    {
                        foundHandle = hWnd;
                        return false; // Окно найдено, завершаем системный перебор
                    }
                }
                return true; // Продолжаем поиск окон
            }, IntPtr.Zero);

            return foundHandle;
        }


        private Process _testedProcess;
        string filePath = "";


        AppMetadata testParams;

        private void StartConsoleScreenSniffer(int pid)
        {
            System.Threading.Tasks.Task.Run(() =>
            {
                AppendToLog("[СИСТЕМА]: Динамический трекер экрана подключен...");
                Thread.Sleep(500);

                if (!AttachConsole(pid))
                {
                    AppendToLog("[ОШИБКА]: Не удалось подключиться к видеобуферу окна.");
                    return;
                }

                IntPtr hConsole = GetStdHandle(STD_OUTPUT_HANDLE);
                int lastReadRow = 0;

                // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ:  следим за глобальным процессом _testedProcess 
                // Пока черное окноcmd висит на экране и не закрыто пользователем — трекер работает!
                while (_testedProcess != null && !_testedProcess.HasExited)
                {
                    if (GetConsoleScreenBufferInfo(hConsole, out CONSOLE_SCREEN_BUFFER_INFO bufferInfo))
                    {
                        int currentCursorRow = bufferInfo.dwCursorPosition.Y;

                        // УМНЫЙ СБРОС СЧЕТЧИКА ПРИ ПОВТОРНОМ ЗАПУСКЕ:
                        // Если пользователь запустил программу заново из ТекстБокса, консоль может очиститься 
                        // или курсор может прыгнуть вверх. Если курсор стал выше, чем последняя прочитанная строка —
                        // значит пошел новый цикл тестирования, сбрасываем счетчик на 0!
                        if (currentCursorRow < lastReadRow)
                        {
                            lastReadRow = 0;
                        }

                        if (currentCursorRow >= lastReadRow)
                        {
                            for (short row = (short)lastReadRow; row <= currentCursorRow; row++)
                            {
                                COORD readCoord = new COORD { X = 0, Y = row };
                                uint maxLineWidth = (uint)bufferInfo.dwSize.X;

                                StringBuilder sb = new StringBuilder((int)maxLineWidth);

                                if (ReadConsoleOutputCharacter(hConsole, sb, maxLineWidth, readCoord, out uint charsRead))
                                {
                                    string cleanLine = sb.ToString().Trim();

                                    // Белый список регулярных выражений: очищает иероглифы, сохраняя слэши папок
                                    cleanLine = System.Text.RegularExpressions.Regex.Replace(cleanLine, @"[^А-Яа-яЁёA-Za-z0-9\s=\[\]_:\-\.,\+\\]", "");
                                    cleanLine = cleanLine.Trim();

                                    if (string.IsNullOrEmpty(cleanLine) ||
                                        cleanLine.Contains("Microsoft Corporation") ||
                                        cleanLine.Contains("bin\\Debug") ||
                                        cleanLine.EndsWith(">"))
                                    {
                                        continue;
                                    }

                                    // Новые строки лога прилетают на форму мгновенно при любом перезапуске
                                    AppendToLog($"[КОНСОЛЬ_ЭКРАН]: {cleanLine}");
                                }
                            }
                            lastReadRow = currentCursorRow + 1;
                        }
                    }
                    Thread.Sleep(1000); // Опрос экрана 10 раз в секунду
                }

                FreeConsole(); // Отключаемся от буфера Windows, когда закроют само черное окно
                AppendToLog("[СИСТЕМА]: Динамический мониторинг экрана успешно завершен.");
            });
        }

        public TestPO()
        {
            InitializeComponent();

            // Настраиваем таймер обновления памяти (каждые 500 миллисекунд = 2 раза в секунду)
            memoryTimer.Interval = 500;

            // Связываем "тик" таймера с методом, который будет замерять память
            memoryTimer.Tick += MemoryTimer_Tick;

            // Привязываем обработчики событий закрытия формы
            this.FormClosing += Form1_FormClosing;
            this.FormClosed += Form1_FormClosed;
        }

        // 1. Срабатывает ПЕРЕД закрытием
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Проверяем: если тест еще идет, а пользователь случайно нажал на крестик
            if (!btn_send_command.Enabled)
            {
                var result = MessageBox.Show("Тестирование еще не завершено! Вы действительно хотите прервать тест и закрыть программу?",
                                             "Внимание", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                {
                    e.Cancel = true; // ОТМЕНЯЕМ закрытие формы, программа продолжит работать!
                    return;
                }
            }

            // ХАРДКОРНАЯ ЗАЧИСТКА: если тестируемый процесс еще жив, убиваем его,
            // чтобы не оставлять в системе мусорные процессы
            if (_testedProcess != null && !_testedProcess.HasExited)
            {
                try
                {
                    _testedProcess.Kill();
                    AppendToLog("[СИСТЕМА]: Тестируемый процесс принудительно завершен перед выходом.");
                }
                catch { /* Игнорируем ошибки, если процесс уже умирает */ }
            }
        }

        // 2. Срабатывает ПОСЛЕ того, как форма закрылась
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {// 1. Полностью выключаем  таймеры, освобождая ресурсы памяти
            memoryTimer?.Stop();

            try
            {
                // 2. Проверяем, есть ли вообще какой-то текст в txtLog, чтобы не плодить пустые файлы
                if (!string.IsNullOrWhiteSpace(txtLog.Text))
                {
                    // Получаем текущую дату и время в компактном формате (ГодМесяцДень_ЧасМинутаСекунда)
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

                    // Пытаемся узнать PID процесса для имени файла, если процесса не было — ставим 0
                    int processId = (_testedProcess != null) ? _testedProcess.Id : 0;

                    // Формируем уникальное имя файла лога
                    string logFileName = $"LOG_{timestamp}_PID{processId}.txt";

                    // Склеиваем имя файла с путем к папке, где лежит наш .exe (AppDomain.CurrentDomain.BaseDirectory)
                    string fullLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);

                    // 3. Записываем весь текст из txtLog в файл. 
                    // Метод WriteAllText сам создаст файл и закроет его, кодировка UTF-8 сохранит русский текст.
                    File.WriteAllText(fullLogPath, txtLog.Text, System.Text.Encoding.UTF8);
                }
            }
            catch
            {
                // Игнорируем возможные ошибки записи (например, если папка защищена от записи администратором), 
                // чтобы программа не вылетала при самом закрытии.
            }

        }
        private void btn_choose_programm_Click(object sender, EventArgs e)
        {
            // Создаем экземпляр диалогового окна в блоке using для автоматического освобождения ресурсов
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                // Настройки диалогового окна (необязательные, но полезные)
                openFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory; //открываем текущее расположение
                openFileDialog.Filter = "Все файлы (*.*)|*.*|Исполняемые файлы (*.exe)|*.exe|Логи (*.log)|*.log"; // Фильтр расширений
                openFileDialog.FilterIndex = 1; // По умолчанию выбран первый фильтр
                openFileDialog.RestoreDirectory = true; // Возвращать исходную директорию после закрытия

                // Показываем диалог и проверяем, нажал ли пользователь "ОК"
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Получаем путь к выбранному файлу
                    filePath = openFileDialog.FileName;

                    // Выводим путь в текстовое поле на форме
                    txtFilePath.Text = filePath;

                    btn_start_analyze.Enabled = true;

                }
            }
        }


        private async void btn_start_analyze_Click(object sender, EventArgs e)
        {
            // Изменяем UI перед началом долгой работы
            btn_start_analyze.Enabled = false; // Блокируем кнопку от повторных кликов
            string resultReport = "";
            string fullReportPath = "";

            // Получаем папку, откуда запущена НАША тестовая программа (.exe)
            string myAppDirectory = Application.StartupPath;

            // Получаем имя тестируемого файла без расширения (например, "VM1")
            string testedFileName = Path.GetFileNameWithoutExtension(filePath);

            try
            {
                // Обернули весь поочередный анализ в асинхронность. 
                resultReport = await Task.Run(() => Analyze.Create_info_about_po(filePath, checkBox_Defender.Checked));

                // Возвращаемся в поток интерфейса и выводим результат
                txt_about_po.Text = resultReport;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при анализе: {ex.Message}");
            }
            finally
            {
                // Возвращаем UI в исходное состояние
                btn_start_analyze.Enabled = true;
            }

            try
            {
                this.Text = "Анализирую и формирую отчет";
                // Формируем имя для текстового отчета: "VM1_report.txt"
                string reportFileName = $"{testedFileName}_report.txt";

                // Сохраняем в директорию НАШЕЙ программы
                fullReportPath = Path.Combine(myAppDirectory, reportFileName);

                // Записываем весь текст отчета в файл
                File.WriteAllText(fullReportPath, resultReport, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось автоматически сохранить отчет на диск: {ex.Message}", "Ошибка сохранения");
            }

            // Ищем целевую DLL рядом с тестируемым файлом
            string testedFileDirectory = Path.GetDirectoryName(filePath);
            string matchingDll = Path.Combine(testedFileDirectory, testedFileName + ".dll");

            // Проверяем: если наш статический анализатор определил, что это C# / .NET
            if (File.Exists(matchingDll) && checkBox_Decompile.Checked)
            {
                try
                {
                    this.Text = "Декомпиляция";
                    // Файл действительно на C#! Запускаем декомпиляцию
                    string sourceCode = await Task.Run(() => CsharpDecompiler.DecompileAssembly(matchingDll));

                    // Формируем имя декомпилированного файла с привязкой к имени софта
                    string sourceFileName = $"{testedFileName}_sourceDECOMPILITED.cs";

                    // Сохраняем полученные исходники в папку НАШЕЙ программы
                    string sourcePath = Path.Combine(myAppDirectory, sourceFileName);
                    File.WriteAllText(sourcePath, sourceCode, Encoding.UTF8);
                    MessageBox.Show($"Декомпилировано");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при декомпиляции или сохранении исходного кода: {ex.Message}", "Ошибка декомпиляции");
                }
            }
            this.Text = "Анализ выполнен";

            // Парсим лог и заполняем ключевые переменные
            testParams = AppMetadata.PopulateVariablesFromReport(resultReport, filePath);

            checkBox_gui.Checked = testParams.HasGui;
            checkBox_Admin.Checked = testParams.RequiresAdmin;
            checkBox_Debug.Checked = testParams.IsDebugBuild;
            checkBox_DotNet.Checked = testParams.IsDotNet;
            checkBox_Unity.Checked = testParams.IsUnity;

            btn_start_test.Enabled = true;
        }
        private void StartTestedApplication()
        {
            try
            {
                // Проверяем, жив ли прошлый процесс оболочки
                if (_testedProcess != null && !_testedProcess.HasExited)
                {
                    return; // Уже работает, ничего не делаем
                }

                string workingDir = Path.GetDirectoryName(filePath);
                _testedProcess = new Process();
                _testedProcess.StartInfo.WorkingDirectory = workingDir;

                if (!testParams.HasGui)
                {
                    // ЧИСТЕЙШИЙ НАТИВНЫЙ ЗАПУСК WINDOWS
                    _testedProcess.StartInfo.FileName = "cmd.exe";
                    _testedProcess.StartInfo.Arguments = $"/k \u0022cd /d \u0022{workingDir}\u0022 && \u0022{filePath}\u0022\u0022";

                    _testedProcess.StartInfo.UseShellExecute = true;
                    _testedProcess.StartInfo.RedirectStandardOutput = false;
                    _testedProcess.StartInfo.RedirectStandardError = false;
                    _testedProcess.StartInfo.RedirectStandardInput = false;
                    _testedProcess.StartInfo.CreateNoWindow = false;
                }
                else
                {
                    _testedProcess.StartInfo.FileName = filePath;
                    _testedProcess.StartInfo.UseShellExecute = true;
                    _testedProcess.StartInfo.CreateNoWindow = false;
                }

                if (_testedProcess.Start())
                {
                    memoryTimer.Start();
                    lblMemory.Text = "RAM: Инициализация...";
                    AppendToLog($"[СИСТЕМА]: Универсальный скрипт-тестер запущен. PID: {_testedProcess.Id}");

                    if (!testParams.HasGui)
                    {
                        StartConsoleScreenSniffer((int)_testedProcess.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"[ОШИБКА АВТОЗАПУСКА]: {ex.Message}");
            }
        }
        private void btn_start_test_Click(object sender, EventArgs e)
        {
            if (_testedProcess != null && !_testedProcess.HasExited)
            {
                MessageBox.Show("Приложение уже запущено!");
                return;
            }

            StartTestedApplication();
            btn_send_command.Enabled = true;

        }


        private void AppendToLog(string text)
        {
            // Проверяем, не вызвана ли функция из фонового потока байтового трекера
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendToLog), text);
                return;
            }

            // ВЫВОДИМ ТЕКСТ СТРОГО НА ФОРМУ. Кавычки и слова больше никогда не сломают cmd.exe!
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} {text}{Environment.NewLine}");
        }

        private void SendInputsToTestedApp(string commandText, int delayAfterMs = 100)
        {
            if (_testedProcess == null || _testedProcess.HasExited)
            {
                AppendToLog("[ВВОД ОТКЛОНЕН]: Тестируемый процесс не запущен.");
                return;
            }

            try
            {
                // ВМЕСТО КАПРИЗНОГО MainWindowHandle ИСПОЛЬЗУЕМ  НАДЁЖНЫЙ ПОИСК ПО PID
                IntPtr hWnd = FindWindowByProcessId(_testedProcess.Id);

                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd); // Жестко приковываем фокус Windows к этому окну
                    Thread.Sleep(80);          // Даем микропаузу для переключения фокуса интерфейса ОС

                    if (!testParams.HasGui)
                    {
                        // Если консоль — вкатываем текст команды и прожимаем клавишу Enter ({ENTER})
                        SendKeys.SendWait(commandText + "{ENTER}");
                        AppendToLog($"[ТЕСТЕР ОТПРАВИЛ В КОНСОЛЬ]: {commandText}");
                    }
                    else
                    {
                        SendKeys.SendWait(commandText);
                        AppendToLog($"[ТЕСТЕР ОТПРАВИЛ В GUI]: {commandText}");
                    }
                }
                else
                {
                    AppendToLog("[УПРАВЛЕНИЕ]: Ошибка. Операционная система скрыла окно процесса. Попробуйте отправить команду еще раз.");
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"[КРИТИЧЕСКАЯ ОШИБКА ВВОДА]: {ex.Message}");
            }
        }

        // Этот метод срабатывает автоматически каждые 500 мс, пока таймер включен
        private void MemoryTimer_Tick(object sender, EventArgs e)
        {
            // Если процесс закрылся или еще не был создан — выключаем таймер
            if (_testedProcess == null || _testedProcess.HasExited)
            {
                memoryTimer.Stop();
                lblMemory.Text = "RAM: Приложение закрыто";
                return;
            }

            try
            {
                // Обновляем внутреннее состояние процесса, чтобы получить свежие данные из Windows
                _testedProcess.Refresh();

                // WorkingSet64 возвращает память в байтах.
                // Переводим в Мегабайты: байты / 1024 (получаем КБ) / 1024 (получаем МБ)
                long ramBytes = _testedProcess.WorkingSet64;
                long ramMb = ramBytes / (1024 * 1024);

                // Выводим результат в нашу метку на форме
                lblMemory.Text = $"RAM: {ramMb} MB";
            }
            catch
            {
                // Сюда код может попасть, если программа закрылась прямо в момент замера памяти
                memoryTimer.Stop();
                lblMemory.Text = "RAM: Ошибка чтения памяти";
            }
        }

        private async void btn_send_command_Click(object sender, EventArgs e)
        {//в тктсбоксе команды делятся абзацами
         // Проверяем, что дескриптор окна консоли существует
            if (_testedProcess != null && _testedProcess.MainWindowHandle != IntPtr.Zero)
            {
                IntPtr handle = _testedProcess.MainWindowHandle;

                // Если окно свернуто в панель задач — принудительно восстанавливаем его
                if (IsIconic(handle))
                {
                    ShowWindow(handle, SW_RESTORE);
                }

                // Выводим окно на передний план, делая его активным для SendKeys
                SetForegroundWindow(handle);
            }
            string userInput = txt_Input.Text;

            if (string.IsNullOrEmpty(userInput))
            {
                MessageBox.Show("Введите команду для отправки!");
                return;
            }

            btn_send_command.Enabled = false;

            try
            {
                string[] commands = userInput
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(cmd => cmd.Trim())
                        .Where(cmd => !string.IsNullOrEmpty(cmd))
                        .ToArray();

                await ExecuteCommandsAsync(commands);
            }
            finally
            {
                btn_send_command.Enabled = true;
            }

            // 3. Передаем эту строку в наш универсальный метод
            //SendInputsToTestedApp(userInput);

        }

        // 2. Сам метод поочередного выполнения с парсингом sleep(...)
        private async Task ExecuteCommandsAsync(string[] commands)
        {
            foreach (string command in commands)
            {
                string cleanCommand = command.Trim();

                // Проверяем, начинается ли команда на "sleep" (теперь без жесткой привязки к скобкам)
                if (cleanCommand.StartsWith("sleep", StringComparison.OrdinalIgnoreCase))
                {
                    // Выдергиваем только цифры из строки (сработает и для "sleep60", и для "sleep(60)")
                    string digitsOnly = new string(cleanCommand.Where(char.IsDigit).ToArray());

                    if (int.TryParse(digitsOnly, out int seconds))
                    {
                        txtLog.AppendText($"[Пауза]: ждем {seconds} сек. В консоль НЕ отправляем." + Environment.NewLine);

                        // Ждем нужное количество секунд
                        await Task.Delay(seconds * 1000);
                    }
                    else
                    {
                        txtLog.AppendText($"[Пауза]: Обнаружен sleep без указания времени." + Environment.NewLine);
                    }

                    // Пропускаем отправку этой команды в тестируемое приложение
                    continue;
                }

                // Если это обычная команда, отправляем её целиком
                txtLog.AppendText($"[Отправка]: {cleanCommand}" + Environment.NewLine);

                SendInputsToTestedApp(cleanCommand);
            }

            txtLog.AppendText("[Готово]: Все команды обработаны!" + Environment.NewLine);
        }

        private void btnLoadTextAndSend_Click(object sender, EventArgs e)
        {//В файле команды делятся запятыми
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            string fileContent = "";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = openFileDialog.FileName;

                    // Одной командой читаем ВЕСЬ текст из файла в одну переменную string
                    fileContent = File.ReadAllText(filePath);


                    MessageBox.Show("Весь текст успешно загружен в строку!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (!string.IsNullOrEmpty(fileContent))
            {
                // Применяем алгоритм разделения
                string[] commands = fileContent
                    .Split(',')
                    .Select(cmd => cmd.Trim())
                    .Where(cmd => !string.IsNullOrEmpty(cmd))
                    .ToArray();

                // Выводим полученный массив в ListBox на форме
                txt_Input.Multiline = true;
                txt_Input.ScrollBars = ScrollBars.Both;
                txt_Input.Lines = commands;
            }
        }

        private void processMonitorTimer_Tick(object sender, EventArgs e)
        {
            // 1. Получаем чистое имя  тестируемого файла (без .exe)
            string processNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

            // 2. Ищем в системе, запущен ли реальный процесс этого приложения (включая админские)
            Process[] runningTargetProcesses = Process.GetProcessesByName(processNameWithoutExt);

            // Если приложение найдено в списке процессов Windows — оно точно живо (возможно, под админом)
            if (runningTargetProcesses.Length > 0)
            {
                // Приложение живо! 
                // Если старая оболочка cmd.exe закрылась, привязываем наш _testedProcess к новому админскому процессу
                if (_testedProcess == null || _testedProcess.HasExited)
                {
                    _testedProcess = runningTargetProcesses[0];
                    AppendToLog($"[МОНИТОР]: Перехвачен процесс с правами администратора. Новый PID: {_testedProcess.Id}");

                    // Если нужно, здесь перезапускаем трекер экрана для нового PID:
                    if (!testParams.HasGui)
                    {
                        StartConsoleScreenSniffer((int)_testedProcess.Id);
                    }
                }

                return; // Выходим из таймера, запускать ничего НЕ НУЖНО
            }

            // 3. Если по имени файла ничего не нашли, проверяем саму запущенную нами оболочку cmd.exe
            if (_testedProcess == null || _testedProcess.HasExited)
            {
                // Сюда мы попадем ТОЛЬКО если приложения нет в процессах И окно консоли закрыто.
                AppendToLog("[МОНИТОР]: Приложение полностью закрыто. Выполняется чистый автозапуск...");

                if (_testedProcess != null)
                {
                    try { _testedProcess.Kill(); } catch { }
                    _testedProcess.Dispose();
                    _testedProcess = null;
                }

                // Запускаем приложение с нуля
                StartTestedApplication();
            }
        }

        private void btn_make_dump_Click(object sender, EventArgs e)
        {
            if (_testedProcess == null || _testedProcess.HasExited)
            {
                MessageBox.Show("Целевое приложение не запущено!");
                return;
            }

            try
            {
                int pid = (int)_testedProcess.Id;
                string timestamp = DateTime.Now.ToString("HHmmss");

                // Пути к файлам в папке нашей программы
                string dumpFileName = $"Dump_PID_{pid}_{timestamp}.dmp";
                string textFileName = $"Extracted_Strings_PID_{pid}_{timestamp}.txt";

                string fullDumpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dumpFileName);
                string fullTextPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, textFileName);

                AppendToLog($"[ДАМПЕР]: Создание слепка памяти для PID {pid}...");

                if (ProcessDumper.CreateProcessDump(pid, fullDumpPath))
                {
                    AppendToLog($"[ДАМПЕР]: Бинарный дамп успешно создан.");
                    AppendToLog($"[ПАРСЕР]: Извлечение понятного текста из дампа без исходников...");

                    // Запускаем автоматическое извлечение строк
                    ProcessDumper.ExtractStringsFromDump(fullDumpPath, fullTextPath);

                    AppendToLog($"[ПАРСЕР]: Текст успешно выгружен в файл: {textFileName}");

                    MessageBox.Show($"Анализ завершен без исходного кода!\n\n" +
                                    $"1. Сырой дамп: {dumpFileName}\n" +
                                    $"2. Понятные данные из памяти: {textFileName}",
                                    "Анализ памяти выполнен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                AppendToLog($"[ОШИБКА АНАЛИЗА]: {ex.Message}");
            }
        }
    }
}
