using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace DaryPWD
{
    internal static class Program
    {
        private static string logFile = Path.Combine(Application.StartupPath, "DaryPWD.log");

        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Créer le fichier de log
            try
            {
                File.WriteAllText(logFile, $"DaryPWD démarré le {DateTime.Now}\n");
            }
            catch { }

            LogMessage("Démarrage de l'application");

            // Gestionnaire d'exceptions global
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                LogMessage("Activation des styles visuels");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                LogMessage("Création de MainForm");
                MainForm mainForm = new MainForm();
                
                LogMessage("Lancement de l'application");
                // Application.Run maintient l'application ouverte jusqu'à ce que la fenêtre soit fermée
                Application.Run(mainForm);
                
                LogMessage("Application fermée normalement");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Erreur critique lors du démarrage:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                LogMessage($"ERREUR: {errorMsg}");
                
                MessageBox.Show(
                    errorMsg,
                    "Erreur DaryPWD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            string errorMsg = $"Erreur dans l'application:\n{e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}";
            LogMessage($"ERREUR Thread: {errorMsg}");
            
            MessageBox.Show(
                errorMsg,
                "Erreur DaryPWD",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                string errorMsg = $"Erreur non gérée:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                LogMessage($"ERREUR Non gérée: {errorMsg}");
                
                MessageBox.Show(
                    errorMsg,
                    "Erreur Critique DaryPWD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static void LogMessage(string message)
        {
            try
            {
                File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { }
        }
    }
}



