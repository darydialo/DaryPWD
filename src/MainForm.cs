using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DaryPWD
{
    public partial class MainForm : Form
    {
        private List<PasswordEntry> passwordEntries;
        private List<PasswordEntry> filteredEntries;
        private BindingSource bindingSource;
        private bool passwordsVisible = true;

        public MainForm()
        {
            try
            {
                LogMessage("Début de l'initialisation de MainForm");
                
                LogMessage("Appel de InitializeComponent");
                InitializeComponent();
                
                LogMessage("Création des collections");
                passwordEntries = new List<PasswordEntry>();
                filteredEntries = new List<PasswordEntry>();
                bindingSource = new BindingSource();
                
                LogMessage("Initialisation du DataGridView");
                InitializeDataGridView();
                
                // Initialiser les états des boutons
                UpdateButtonStates();
                
                // S'assurer que la fenêtre reste ouverte
                this.FormClosing += MainForm_FormClosing;
                
                // Charger l'icône de l'application
                try
                {
                    string iconPath = Path.Combine(Application.StartupPath, "resources", "DaryPWD.ico");
                    if (!File.Exists(iconPath))
                    {
                        iconPath = Path.Combine(Application.StartupPath, "DaryPWD.ico");
                    }
                    if (File.Exists(iconPath))
                    {
                        this.Icon = new Icon(iconPath);
                    }
                }
                catch
                {
                    // Si l'icône ne peut pas être chargée, continuer sans icône
                }
                
                LogMessage("Affichage de la fenêtre");
                this.Show();
                this.BringToFront();
                this.Activate();
                this.Focus();
                Application.DoEvents();
                
                // Charger les mots de passe de manière asynchrone après l'affichage
                LogMessage("Démarrage du chargement asynchrone des mots de passe");
                System.Threading.Thread loadThread = new System.Threading.Thread(() =>
                {
                    try
                    {
                        LoadPasswords();
                    }
                    catch (Exception ex)
                    {
                        if (!this.IsDisposed && this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                MessageBox.Show(
                                    $"Erreur lors du chargement:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                                    "Erreur",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                            }));
                        }
                    }
                });
                loadThread.IsBackground = true;
                loadThread.Start();
                
                LogMessage("Initialisation terminée avec succès");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Erreur lors de l'initialisation:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                LogMessage($"ERREUR: {errorMsg}");
                
                MessageBox.Show(
                    errorMsg,
                    "Erreur DaryPWD",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogMessage("Fermeture de la fenêtre demandée par l'utilisateur");
            // Permettre la fermeture normale
        }

        private void LogMessage(string message)
        {
            try
            {
                string logFile = Path.Combine(Application.StartupPath, "DaryPWD.log");
                File.AppendAllText(logFile, $"[MainForm] [{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { }
        }

        private void InitializeDataGridView()
        {
            dataGridView.AutoGenerateColumns = false;
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MultiSelect = false;
            dataGridView.ReadOnly = false; // Permettre l'édition
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView.RowHeadersVisible = true;

            // Colonnes
            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "EntryName",
                HeaderText = "Entry Name",
                DataPropertyName = "EntryName",
                Width = 250,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                DataPropertyName = "Type",
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "StoredIn",
                HeaderText = "Stored In",
                DataPropertyName = "StoredIn",
                Width = 120,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UserName",
                HeaderText = "User Name",
                DataPropertyName = "UserName",
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            dataGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Password",
                HeaderText = "Password",
                DataPropertyName = "Password",
                Width = 150,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            bindingSource.DataSource = filteredEntries;
            dataGridView.DataSource = bindingSource;

            dataGridView.SelectionChanged += DataGridView_SelectionChanged;
            
            // Activer le tri sur toutes les colonnes
            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.Automatic;
            }
            
            LogMessage("DataGridView initialisé avec " + dataGridView.Columns.Count + " colonnes");
        }

        private void dataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            // Tri personnalisé pour les colonnes
            object value1 = e.CellValue1 ?? string.Empty;
            object value2 = e.CellValue2 ?? string.Empty;
            
            if (value1 is string && value2 is string)
            {
                e.SortResult = string.Compare((string)value1, (string)value2, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                e.SortResult = Comparer<object>.Default.Compare(value1, value2);
            }
            
            e.Handled = true;
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            UpdateStatusBar();
            UpdateButtonStates();
        }

        private void LoadPasswords()
        {
            try
            {
                LogMessage("Début de LoadPasswords");
                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        Cursor = Cursors.WaitCursor;
                        statusLabel.Text = "Extraction des mots de passe...";
                    }));
                }
                else
                {
                    Cursor = Cursors.WaitCursor;
                    statusLabel.Text = "Extraction des mots de passe...";
                }
                
                Application.DoEvents();
                
                LogMessage("Appel de IEPasswordExtractor.ExtractPasswords()");
                List<PasswordEntry> entries = IEPasswordExtractor.ExtractPasswords();
                LogMessage($"Extraction terminée: {entries.Count} entrées trouvées");
                
                // Log détaillé des entrées trouvées
                foreach (var entry in entries)
                {
                    LogMessage($"  - {entry.EntryName} | {entry.UserName} | {entry.Type}");
                }
                
                passwordEntries = entries;
                ApplyFilter(); // Appliquer le filtre après chargement
                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        RefreshDataGridView();
                        UpdateButtonStates();
                        Cursor = Cursors.Default;
                        
                        if (passwordEntries.Count == 0)
                        {
                            statusLabel.Text = "Aucun mot de passe trouvé";
                            MessageBox.Show(
                                "Aucun mot de passe n'a été trouvé.\n\n" +
                                "Vérifiez que:\n" +
                                "- Internet Explorer ou Microsoft Edge a été utilisé pour enregistrer des mots de passe\n" +
                                "- L'application est exécutée avec les droits appropriés\n" +
                                "- Les mots de passe sont stockés dans le Credential Manager Windows\n" +
                                "- Des sites web ont été consultés et les mots de passe sauvegardés",
                                "Information",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }));
                }
                else
                {
                    RefreshDataGridView();
                    UpdateButtonStates();
                    Cursor = Cursors.Default;
                    
                    if (passwordEntries.Count == 0)
                    {
                        statusLabel.Text = "Aucun mot de passe trouvé";
                        MessageBox.Show(
                            "Aucun mot de passe n'a été trouvé.\n\n" +
                            "Vérifiez que:\n" +
                            "- Internet Explorer ou Microsoft Edge a été utilisé pour enregistrer des mots de passe\n" +
                            "- L'application est exécutée avec les droits appropriés\n" +
                            "- Les mots de passe sont stockés dans le Credential Manager Windows\n" +
                            "- Des sites web ont été consultés et les mots de passe sauvegardés",
                            "Information",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
                
                LogMessage("LoadPasswords terminé avec succès");
            }
            catch (Exception ex)
            {
                string errorMsg = $"Erreur lors de l'extraction des mots de passe:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                LogMessage($"ERREUR dans LoadPasswords: {errorMsg}");
                
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show(
                            errorMsg,
                            "Erreur",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        statusLabel.Text = "Erreur lors de l'extraction";
                        Cursor = Cursors.Default;
                    }));
                }
                else
                {
                    MessageBox.Show(
                        errorMsg,
                        "Erreur",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    statusLabel.Text = "Erreur lors de l'extraction";
                    Cursor = Cursors.Default;
                }
            }
        }

        private void UpdateStatusBar()
        {
            int selectedCount = dataGridView.SelectedRows.Count;
            int totalCount = passwordEntries.Count;
            int filteredCount = filteredEntries.Count;
            
            if (filteredCount < totalCount)
            {
                statusLabel.Text = $"{filteredCount} sur {totalCount} entrée(s) affichée(s), {selectedCount} sélectionnée(s)";
            }
            else
            {
                statusLabel.Text = $"{totalCount} entrée(s), {selectedCount} sélectionnée(s)";
            }
        }

        private void RefreshDataGridView()
        {
            bindingSource.DataSource = filteredEntries;
            bindingSource.ResetBindings(false);
            UpdatePasswordColumnVisibility();
            dataGridView.Refresh();
            Application.DoEvents();
            UpdateStatusBar();
        }

        private void ApplyFilter()
        {
            string searchText = searchTextBox.Text.Trim().ToLowerInvariant();
            
            if (string.IsNullOrEmpty(searchText))
            {
                filteredEntries = new List<PasswordEntry>(passwordEntries);
            }
            else
            {
                filteredEntries = passwordEntries.Where(entry =>
                    (entry.EntryName ?? "").ToLowerInvariant().Contains(searchText) ||
                    (entry.Type ?? "").ToLowerInvariant().Contains(searchText) ||
                    (entry.StoredIn ?? "").ToLowerInvariant().Contains(searchText) ||
                    (entry.UserName ?? "").ToLowerInvariant().Contains(searchText) ||
                    (entry.Password ?? "").ToLowerInvariant().Contains(searchText)
                ).ToList();
            }
            
            RefreshDataGridView();
        }

        private void UpdatePasswordColumnVisibility()
        {
            DataGridViewColumn passwordColumn = dataGridView.Columns["Password"];
            if (passwordColumn != null)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.DataBoundItem is PasswordEntry entry)
                    {
                        if (passwordsVisible)
                        {
                            row.Cells["Password"].Value = entry.Password;
                        }
                        else
                        {
                            row.Cells["Password"].Value = new string('*', entry.Password?.Length ?? 0);
                        }
                    }
                }
            }
        }

        private void UpdateButtonStates()
        {
            bool hasData = passwordEntries.Count > 0;
            bool hasSelection = dataGridView.SelectedRows.Count > 0;
            
            saveToolStripButton.Enabled = hasData;
            copyToolStripButton.Enabled = hasSelection;
            exportToolStripButton.Enabled = hasData;
            exportTxtToolStripMenuItem.Enabled = hasData;
            exportCsvToolStripMenuItem.Enabled = hasData;
            exportHtmlToolStripMenuItem.Enabled = hasData;
            exportXmlToolStripMenuItem.Enabled = hasData;
        }

        private void searchTextBox_TextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void togglePasswordButton_Click(object sender, EventArgs e)
        {
            passwordsVisible = !passwordsVisible;
            togglePasswordButton.Text = passwordsVisible ? "Hide Passwords" : "Show Passwords";
            UpdatePasswordColumnVisibility();
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            SaveToFile();
        }

        private void refreshToolStripButton_Click(object sender, EventArgs e)
        {
            LoadPasswords();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            CopySelectedToClipboard();
        }

        private void exportToolStripButton_Click(object sender, EventArgs e)
        {
            ExportToFile();
        }

        private void SaveToFile()
        {
            ExportToTxt();
        }

        private void ExportToTxt()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = "DaryPWD_Passwords.txt"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("DaryPWD - Extracted Passwords");
                    sb.AppendLine("Juste By Dary");
                    sb.AppendLine("Contact: darydialo@gmail.com");
                    sb.AppendLine("Generated: " + DateTime.Now.ToString());
                    sb.AppendLine(new string('=', 80));

                    foreach (var entry in passwordEntries)
                    {
                        sb.AppendLine($"Entry Name: {entry.EntryName}");
                        sb.AppendLine($"Type: {entry.Type}");
                        sb.AppendLine($"Stored In: {entry.StoredIn}");
                        sb.AppendLine($"User Name: {entry.UserName}");
                        sb.AppendLine($"Password: {entry.Password}");
                        sb.AppendLine(new string('-', 80));
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Fichier sauvegardé avec succès:\n{saveDialog.FileName}", 
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la sauvegarde:\n{ex.Message}", 
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToCsv()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = "DaryPWD_Passwords.csv"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("Entry Name,Type,Stored In,User Name,Password");

                    foreach (var entry in passwordEntries)
                    {
                        sb.AppendLine($"\"{EscapeCsv(entry.EntryName)}\",\"{EscapeCsv(entry.Type)}\",\"{EscapeCsv(entry.StoredIn)}\",\"{EscapeCsv(entry.UserName)}\",\"{EscapeCsv(entry.Password)}\"");
                    }

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Fichier exporté avec succès:\n{saveDialog.FileName}", 
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export:\n{ex.Message}", 
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToHtml()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html|All Files (*.*)|*.*",
                FileName = "DaryPWD_Passwords.html"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html>");
                    sb.AppendLine("<html><head><meta charset='UTF-8'>");
                    sb.AppendLine("<title>DaryPWD - Extracted Passwords</title>");
                    sb.AppendLine("<style>");
                    sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
                    sb.AppendLine("h1 { color: #333; }");
                    sb.AppendLine("table { border-collapse: collapse; width: 100%; margin-top: 20px; }");
                    sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
                    sb.AppendLine("th { background-color: #4CAF50; color: white; }");
                    sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
                    sb.AppendLine("</style></head><body>");
                    sb.AppendLine("<h1>DaryPWD - Extracted Passwords</h1>");
                    sb.AppendLine("<p><strong>Juste By Dary</strong><br>");
                    sb.AppendLine("Contact: darydialo@gmail.com<br>");
                    sb.AppendLine($"Generated: {DateTime.Now}</p>");
                    sb.AppendLine("<table>");
                    sb.AppendLine("<tr><th>Entry Name</th><th>Type</th><th>Stored In</th><th>User Name</th><th>Password</th></tr>");

                    foreach (var entry in passwordEntries)
                    {
                        sb.AppendLine("<tr>");
                        sb.AppendLine($"<td>{EscapeHtml(entry.EntryName)}</td>");
                        sb.AppendLine($"<td>{EscapeHtml(entry.Type)}</td>");
                        sb.AppendLine($"<td>{EscapeHtml(entry.StoredIn)}</td>");
                        sb.AppendLine($"<td>{EscapeHtml(entry.UserName)}</td>");
                        sb.AppendLine($"<td>{EscapeHtml(entry.Password)}</td>");
                        sb.AppendLine("</tr>");
                    }

                    sb.AppendLine("</table></body></html>");

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Fichier exporté avec succès:\n{saveDialog.FileName}", 
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export:\n{ex.Message}", 
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportToXml()
        {
            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "XML Files (*.xml)|*.xml|All Files (*.*)|*.*",
                FileName = "DaryPWD_Passwords.xml"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    sb.AppendLine("<DaryPWD>");
                    sb.AppendLine($"<Info>");
                    sb.AppendLine($"<Developer>Juste By Dary</Developer>");
                    sb.AppendLine($"<Contact>darydialo@gmail.com</Contact>");
                    sb.AppendLine($"<Generated>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</Generated>");
                    sb.AppendLine($"</Info>");
                    sb.AppendLine("<Passwords>");

                    foreach (var entry in passwordEntries)
                    {
                        sb.AppendLine("<Password>");
                        sb.AppendLine($"<EntryName>{EscapeXml(entry.EntryName)}</EntryName>");
                        sb.AppendLine($"<Type>{EscapeXml(entry.Type)}</Type>");
                        sb.AppendLine($"<StoredIn>{EscapeXml(entry.StoredIn)}</StoredIn>");
                        sb.AppendLine($"<UserName>{EscapeXml(entry.UserName)}</UserName>");
                        sb.AppendLine($"<Password>{EscapeXml(entry.Password)}</Password>");
                        sb.AppendLine("</Password>");
                    }

                    sb.AppendLine("</Passwords>");
                    sb.AppendLine("</DaryPWD>");

                    File.WriteAllText(saveDialog.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show($"Fichier exporté avec succès:\n{saveDialog.FileName}", 
                        "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'export:\n{ex.Message}", 
                        "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string EscapeCsv(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("\"", "\"\"");
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        private string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");
        }

        private void CopySelectedToClipboard()
        {
            if (dataGridView.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridView.SelectedRows[0];
                if (selectedRow.DataBoundItem is PasswordEntry entry)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine($"Entry Name: {entry.EntryName}");
                    sb.AppendLine($"Type: {entry.Type}");
                    sb.AppendLine($"Stored In: {entry.StoredIn}");
                    sb.AppendLine($"User Name: {entry.UserName}");
                    sb.AppendLine($"Password: {entry.Password}");

                    Clipboard.SetText(sb.ToString());
                    MessageBox.Show("Informations copiées dans le presse-papiers.", 
                        "Copié", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExportToFile()
        {
            ExportToCsv();
        }

        private void exportTxtToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToTxt();
        }

        private void exportCsvToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToCsv();
        }

        private void exportHtmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToHtml();
        }

        private void exportXmlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToXml();
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Menu File - pourrait être étendu
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Menu Edit - pourrait être étendu
        }

        private void editEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditSelectedEntry();
        }

        private void editToolStripButton_Click(object sender, EventArgs e)
        {
            EditSelectedEntry();
        }

        private void deleteEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DeleteSelectedEntry();
        }

        private void deleteToolStripButton_Click(object sender, EventArgs e)
        {
            DeleteSelectedEntry();
        }

        private void EditSelectedEntry()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une entrée à modifier.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
            PasswordEntry entry = selectedRow.DataBoundItem as PasswordEntry;
            
            if (entry == null)
                return;

            // Créer un formulaire d'édition
            Form editForm = new Form
            {
                Text = "Modifier l'entrée",
                Size = new Size(450, 250),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            Label entryNameLabel = new Label { Text = "Entry Name:", Location = new Point(20, 20), Size = new Size(100, 20) };
            TextBox entryNameTextBox = new TextBox { Text = entry.EntryName, Location = new Point(130, 18), Size = new Size(280, 20), ReadOnly = true };

            Label userNameLabel = new Label { Text = "User Name:", Location = new Point(20, 50), Size = new Size(100, 20) };
            TextBox userNameTextBox = new TextBox { Text = entry.UserName, Location = new Point(130, 48), Size = new Size(280, 20) };

            Label passwordLabel = new Label { Text = "Password:", Location = new Point(20, 80), Size = new Size(100, 20) };
            TextBox passwordTextBox = new TextBox { Text = entry.Password, Location = new Point(130, 78), Size = new Size(280, 20) };

            Label typeLabel = new Label { Text = "Type:", Location = new Point(20, 110), Size = new Size(100, 20) };
            TextBox typeTextBox = new TextBox { Text = entry.Type, Location = new Point(130, 108), Size = new Size(280, 20), ReadOnly = true };

            Button okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(245, 150), Size = new Size(75, 25) };
            Button cancelButton = new Button { Text = "Annuler", DialogResult = DialogResult.Cancel, Location = new Point(330, 150), Size = new Size(75, 25) };

            editForm.Controls.AddRange(new Control[] { 
                entryNameLabel, entryNameTextBox, 
                userNameLabel, userNameTextBox, 
                passwordLabel, passwordTextBox,
                typeLabel, typeTextBox,
                okButton, cancelButton 
            });

            editForm.AcceptButton = okButton;
            editForm.CancelButton = cancelButton;

            if (editForm.ShowDialog(this) == DialogResult.OK)
            {
                entry.UserName = userNameTextBox.Text;
                entry.Password = passwordTextBox.Text;
                
                RefreshDataGridView();
                LogMessage($"Entrée modifiée: {entry.EntryName}");
                MessageBox.Show("Entrée modifiée avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteSelectedEntry()
        {
            if (dataGridView.SelectedRows.Count == 0)
            {
                MessageBox.Show("Veuillez sélectionner une entrée à supprimer.", "Information", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DataGridViewRow selectedRow = dataGridView.SelectedRows[0];
            PasswordEntry entry = selectedRow.DataBoundItem as PasswordEntry;
            
            if (entry == null)
                return;

            DialogResult result = MessageBox.Show(
                $"Voulez-vous vraiment supprimer cette entrée ?\n\n{entry.EntryName}",
                "Confirmation de suppression",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                passwordEntries.Remove(entry);
                ApplyFilter();
                RefreshDataGridView();
                LogMessage($"Entrée supprimée: {entry.EntryName}");
                MessageBox.Show("Entrée supprimée avec succès.", "Succès", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void dataGridView_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            // Permettre l'édition uniquement pour UserName et Password
            if (e.ColumnIndex == 0 || e.ColumnIndex == 1 || e.ColumnIndex == 2) // EntryName, Type, StoredIn
            {
                e.Cancel = true;
            }
        }

        private void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            // Mettre à jour l'entrée après édition
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView.Rows.Count)
            {
                DataGridViewRow row = dataGridView.Rows[e.RowIndex];
                PasswordEntry entry = row.DataBoundItem as PasswordEntry;
                
                if (entry != null)
                {
                    if (e.ColumnIndex == 3) // UserName
                    {
                        entry.UserName = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    }
                    else if (e.ColumnIndex == 4) // Password
                    {
                        entry.Password = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";
                    }
                    
                    LogMessage($"Cellule modifiée: {entry.EntryName} - Colonne {e.ColumnIndex}");
                }
            }
        }

        private void viewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Menu View - pourrait être étendu
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form aboutForm = new Form
            {
                Text = "À propos de DaryPWD",
                Size = new Size(400, 320),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "resources", "DaryPWD.ico");
                if (!File.Exists(iconPath)) iconPath = Path.Combine(Application.StartupPath, "DaryPWD.ico");
                if (File.Exists(iconPath)) aboutForm.Icon = new Icon(iconPath);
            }
            catch { }

            PictureBox iconPictureBox = new PictureBox { Size = new Size(64, 64), Location = new Point(20, 20), SizeMode = PictureBoxSizeMode.Zoom };
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "resources", "DaryPWD.ico");
                if (!File.Exists(iconPath)) iconPath = Path.Combine(Application.StartupPath, "DaryPWD.ico");
                if (File.Exists(iconPath)) iconPictureBox.Image = new Icon(iconPath, 64, 64).ToBitmap();
            }
            catch
            {
                Bitmap iconBmp = new Bitmap(64, 64);
                using (Graphics g = Graphics.FromImage(iconBmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.FromArgb(255, 255, 220, 0));
                    using (Pen pen = new Pen(Color.Black, 2))
                    using (SolidBrush brush = new SolidBrush(Color.Black))
                    {
                        g.DrawRectangle(pen, 10, 20, 15, 10);
                        g.DrawRectangle(pen, 39, 20, 15, 10);
                        g.DrawLine(pen, 25, 25, 39, 25);
                        g.FillEllipse(brush, 15, 23, 5, 5);
                        g.FillEllipse(brush, 44, 23, 5, 5);
                        g.DrawArc(pen, 15, 32, 34, 20, 0, 180);
                    }
                }
                iconPictureBox.Image = iconBmp;
            }

            Label titleLabel = new Label { Text = "DaryPWD", Font = new Font("Microsoft Sans Serif", 14, FontStyle.Bold), Location = new Point(100, 20), Size = new Size(270, 25), AutoSize = false };
            Label versionLabel = new Label { Text = "Version 1.0", Location = new Point(100, 50), Size = new Size(270, 20) };
            Label descLabel = new Label { Text = "Application d'extraction de mots de passe\nInternet Explorer et Microsoft Edge\n\n⚠️ AVERTISSEMENT:\nUtilisation strictement limitée à vos propres systèmes.\nToute utilisation malveillante est interdite.", Location = new Point(20, 100), Size = new Size(350, 80) };
            Label authorLabel = new Label { Text = " By Dary", Font = new Font("Microsoft Sans Serif", 9, FontStyle.Bold), Location = new Point(20, 190), Size = new Size(350, 20) };
            Label contactLabel = new Label { Text = "Contact : darydialo@gmail.com", Location = new Point(20, 210), Size = new Size(350, 20) };
            Label disclaimerLabel = new Label { Text = "⚠️ L'auteur n'est pas responsable de l'utilisation\nmalveillante ou illégale de cet outil.", Font = new Font("Microsoft Sans Serif", 7, FontStyle.Italic), ForeColor = Color.DarkRed, Location = new Point(20, 235), Size = new Size(350, 30) };
            Button okButton = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(305, 270), Size = new Size(75, 25) };

            aboutForm.Controls.AddRange(new Control[] { iconPictureBox, titleLabel, versionLabel, descLabel, authorLabel, contactLabel, disclaimerLabel, okButton });
            aboutForm.AcceptButton = okButton;
            aboutForm.ShowDialog(this);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
