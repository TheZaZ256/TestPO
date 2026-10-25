namespace TestPO
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            //Application.Run(new TestPO());

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Запуск вашей главной формы
                Application.Run(new TestPO());
            }
            catch (Exception ex)
            {
                // Если форма падает на старте — мы увидим красивое окошко с ошибкой вместо тихого закрытия
                MessageBox.Show($"Критическая ошибка при запуске интерфейса:\n{ex.Message}\n\nДетали:\n{ex.StackTrace}",
                                "Сбой запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}