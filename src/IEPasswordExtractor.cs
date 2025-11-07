using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using System.Text;
using System.IO;
using System.Data.SQLite;

namespace DaryPWD
{
    public class PasswordEntry
    {
        public string EntryName { get; set; }
        public string Type { get; set; }
        public string StoredIn { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class IEPasswordExtractor
    {
        private static void LogMessage(string message)
        {
            try
            {
                string exeDirLog = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "DaryPWD.log");
                File.AppendAllText(exeDirLog, $"[IEPasswordExtractor] [{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch { }
        }
        #region Windows API Declarations

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredEnumerate(string filter, int flags, out int count, out IntPtr credentials);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public int CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn,
            StringBuilder ppszDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct,
            int dwFlags,
            ref DATA_BLOB pDataOut);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CRYPTPROTECT_PROMPTSTRUCT
        {
            public int cbSize;
            public int dwPromptFlags;
            public IntPtr hwndApp;
            public string szPrompt;
        }

        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;

        #endregion

        #region Password Cleaning Utilities

        /// <summary>
        /// Nettoie un mot de passe en supprimant les caractères non imprimables et en détectant les données binaires
        /// </summary>
        private static string CleanPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return password;

            // Vérifier si c'est du binaire (contient beaucoup de caractères non-ASCII)
            int nonAsciiCount = 0;
            int printableCount = 0;
            
            foreach (char c in password)
            {
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                {
                    nonAsciiCount++;
                }
                else if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || c == ' ')
                {
                    printableCount++;
                }
                else if (c > 127)
                {
                    nonAsciiCount++;
                }
            }

            // Si plus de 30% de caractères non-ASCII ou caractères de contrôle, c'est probablement du binaire corrompu
            if (password.Length > 0 && (nonAsciiCount * 100.0 / password.Length) > 30)
            {
                LogMessage($"Mot de passe suspect détecté ({nonAsciiCount} caractères non-ASCII sur {password.Length})");
                return string.Empty; // Retourner vide pour indiquer que c'est corrompu
            }

            // Nettoyer les caractères de contrôle sauf les espaces, tabulations, retours à la ligne
            StringBuilder cleaned = new StringBuilder();
            foreach (char c in password)
            {
                if (char.IsControl(c) && c != '\r' && c != '\n' && c != '\t')
                {
                    continue; // Ignorer les caractères de contrôle
                }
                else if (c >= 32 && c <= 126) // Caractères ASCII imprimables
                {
                    cleaned.Append(c);
                }
                else if (c > 127 && c < 256) // Caractères étendus (Latin-1)
                {
                    cleaned.Append(c);
                }
                else if (c == '\r' || c == '\n' || c == '\t')
                {
                    cleaned.Append(' '); // Remplacer par un espace
                }
                // Ignorer les autres caractères (Unicode supérieur à 256, caractères spéciaux)
            }

            string result = cleaned.ToString().Trim();
            
            // Si après nettoyage il ne reste presque rien, c'est probablement corrompu
            if (result.Length < password.Length * 0.5)
            {
                LogMessage($"Mot de passe trop dégradé après nettoyage ({result.Length} caractères sur {password.Length})");
                return string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Vérifie si un mot de passe est valide (non vide et non corrompu)
        /// </summary>
        private static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            string cleaned = CleanPassword(password);
            return !string.IsNullOrEmpty(cleaned);
        }

        #endregion

        public static List<PasswordEntry> ExtractPasswords()
        {
            List<PasswordEntry> entries = new List<PasswordEntry>();

            try
            {
                LogMessage("=== DÉBUT DE L'EXTRACTION DES MOTS DE PASSE ===");
                LogMessage($"Version OS: {Environment.OSVersion}");
                LogMessage($"Utilisateur: {Environment.UserName}");
                
                // Diagnostic préalable : vérifier l'existence des clés de registre IE
                DiagnoseIERegistry();
                
                // 1. Extraire depuis Windows Credential Manager (HTTP Authentication, FTP, Password-Protected Sites)
                LogMessage("Début extraction Credential Manager");
                ExtractFromCredentialManager(entries);
                LogMessage($"Après Credential Manager: {entries.Count} entrées");

                // 2. Extraire depuis le Registre (AutoComplete Internet Explorer)
                LogMessage("Début extraction Registry (IE AutoComplete)");
                ExtractFromRegistry(entries);
                LogMessage($"Après Registry IE: {entries.Count} entrées totales");

                // 3. Extraire depuis Microsoft Edge
                LogMessage("Début extraction Microsoft Edge");
                ExtractFromEdge(entries);
                LogMessage($"Après Edge: {entries.Count} entrées totales");
                
                // 4. Extraire les mots de passe FTP depuis le registre
                LogMessage("Début extraction FTP");
                ExtractFTPPasswords(entries);
                LogMessage($"Après FTP: {entries.Count} entrées totales");
                
                // 5. Extraire depuis Google Chrome
                LogMessage("Début extraction Google Chrome");
                ExtractFromChrome(entries);
                LogMessage($"Après Chrome: {entries.Count} entrées totales");
                
                // 6. Extraire depuis Opera
                LogMessage("Début extraction Opera");
                ExtractFromOpera(entries);
                LogMessage($"Après Opera: {entries.Count} entrées totales");
                
                // 7. Extraire depuis Brave
                LogMessage("Début extraction Brave");
                ExtractFromBrave(entries);
                LogMessage($"Après Brave: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR dans ExtractPasswords: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }

            return entries;
        }

        private static void ExtractFromCredentialManager(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromCredentialManager");
                int count = 0;
                IntPtr credentialsPtr = IntPtr.Zero;

                LogMessage("Appel CredEnumerate...");
                bool success = CredEnumerate(null, 0, out count, out credentialsPtr);
                LogMessage($"CredEnumerate retourné: {success}, count: {count}");

                if (success && count > 0)
                {
                    LogMessage($"Traitement de {count} credentials");

                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            IntPtr credentialPtr = Marshal.ReadIntPtr(credentialsPtr, i * IntPtr.Size);
                            if (credentialPtr == IntPtr.Zero)
                            {
                                LogMessage($"Credential pointer {i + 1} nul, passage au suivant");
                                continue;
                            }

                            LogMessage($"Traitement credential {i + 1}/{count}");
                            CREDENTIAL cred = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);

                            if (cred.TargetName == IntPtr.Zero)
                            {
                                LogMessage($"Credential {i + 1} sans TargetName, ignoré");
                                continue;
                            }

                            string targetName = Marshal.PtrToStringUni(cred.TargetName) ?? string.Empty;
                            string userName = cred.UserName != IntPtr.Zero ? Marshal.PtrToStringUni(cred.UserName) ?? string.Empty : string.Empty;

                            LogMessage($"Credential {i + 1}: Type={cred.Type}, TargetName={targetName}, UserName={userName}");

                            // Extraire toutes les entrées de type Generic et Domain
                            if (cred.Type == CRED_TYPE_GENERIC || cred.Type == 2) // Domain credentials
                            {
                                string password = string.Empty;
                                if (cred.CredentialBlob != IntPtr.Zero && cred.CredentialBlobSize > 0)
                                {
                                    try
                                    {
                                        byte[] passwordBytes = new byte[cred.CredentialBlobSize];
                                        Marshal.Copy(cred.CredentialBlob, passwordBytes, 0, cred.CredentialBlobSize);
                                        password = Encoding.Unicode.GetString(passwordBytes).TrimEnd('\0');
                                        
                                        // Nettoyer le mot de passe
                                        password = CleanPassword(password);
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"Erreur extraction password credential {i + 1}: {ex.Message}");
                                    }
                                }

                                string lowerTarget = targetName.ToLowerInvariant();

                                bool isWebEntry = !string.IsNullOrEmpty(targetName) && (
                                    lowerTarget.StartsWith("http://") ||
                                    lowerTarget.StartsWith("https://") ||
                                    lowerTarget.StartsWith("edge://") ||
                                    lowerTarget.StartsWith("ftp://") ||
                                    lowerTarget.Contains("internet") ||
                                    lowerTarget.Contains("ie:") ||
                                    lowerTarget.Contains("://") ||
                                    lowerTarget.Contains("www.") ||
                                    lowerTarget.Contains(".com") ||
                                    lowerTarget.Contains(".net") ||
                                    lowerTarget.Contains(".org") ||
                                    lowerTarget.Contains("login") ||
                                    lowerTarget.Contains("signin") ||
                                    lowerTarget.Contains("auth") ||
                                    lowerTarget.Contains("microsoft.com") ||
                                    lowerTarget.Contains("edge") ||
                                    lowerTarget.Contains("target"));

                                // Détecter le type de mot de passe
                                string passwordType = "Password-Protected Site";
                                string storedIn = "Credentials File";

                                if (!string.IsNullOrEmpty(targetName))
                                {
                                    if (lowerTarget.StartsWith("ftp://") || lowerTarget.Contains("ftp."))
                                    {
                                        passwordType = "FTP";
                                        storedIn = "Credentials File";
                                    }
                                    else if (lowerTarget.Contains("http") && !lowerTarget.Contains("://"))
                                    {
                                        passwordType = "HTTP Authentication";
                                        storedIn = "Credentials File";
                                    }
                                    else if (lowerTarget.Contains("edge") || lowerTarget.Contains("microsoft-edge"))
                                    {
                                        passwordType = "Microsoft Edge";
                                        storedIn = "Edge (Credentials File)";
                                    }
                                    else if (lowerTarget.Contains("internet") || lowerTarget.Contains("ie"))
                                    {
                                        passwordType = "Internet Explorer";
                                        storedIn = "IE (Credentials File)";
                                    }
                                }

                                // Ne pas ajouter si le mot de passe est corrompu et qu'il n'y a pas d'indice web
                                if (!string.IsNullOrEmpty(password) && !IsValidPassword(password))
                                {
                                    LogMessage($"Credential {i + 1} ignoré (mot de passe corrompu): {targetName}");
                                    password = string.Empty; // Réinitialiser pour éviter d'afficher du binaire
                                }

                                if ((!string.IsNullOrEmpty(password) && IsValidPassword(password)) || isWebEntry)
                                {
                                    entries.Add(new PasswordEntry
                                    {
                                        EntryName = targetName,
                                        Type = passwordType,
                                        StoredIn = storedIn,
                                        UserName = userName,
                                        Password = password
                                    });
                                    LogMessage($"✓ Ajouté credential {i + 1}: {targetName} | Type: {passwordType} | User: {userName} | Password: {(string.IsNullOrEmpty(password) ? "(vide)" : "***")}");
                                }
                                else
                                {
                                    LogMessage($"Credential {i + 1} ignoré (pas d'indice web ni mot de passe valide)");
                                }
                            }
                            else
                            {
                                LogMessage($"Credential {i + 1} ignoré (type {cred.Type})");
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"ERREUR traitement credential {i + 1}: {ex.Message}");
                            LogMessage($"Stack Trace: {ex.StackTrace}");
                        }
                    }

                    LogMessage("Libération des credentials");
                    CredFree(credentialsPtr);
                }
                else
                {
                    LogMessage("Aucun credential trouvé ou erreur CredEnumerate");
                }
                
                LogMessage($"Fin ExtractFromCredentialManager: {entries.Count} entrées");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromCredentialManager: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromRegistry(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromRegistry");
                
                // Chemin principal du registre pour les mots de passe AutoComplete d'Internet Explorer
                // Windows 7 utilise Storage2, mais peut aussi utiliser Storage1
                string[] ieStoragePaths = new string[]
                {
                    @"Software\Microsoft\Internet Explorer\IntelliForms\Storage2",
                    @"Software\Microsoft\Internet Explorer\IntelliForms\Storage1",
                    @"Software\Microsoft\Internet Explorer\IntelliForms\SPW"
                };
                
                // Essayer chaque chemin de stockage IE
                foreach (string iePath in ieStoragePaths)
                {
                    try
                    {
                        LogMessage($"Extraction depuis le chemin IE: {iePath}");
                        ExtractIEDataFromRegistryPath(Registry.CurrentUser, iePath, entries);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erreur chemin IE {iePath}: {ex.Message}");
                    }
                }
                
                // Essayer aussi d'autres chemins possibles pour Windows 7
                string[] additionalPaths = new string[]
                {
                    @"Software\Microsoft\Internet Explorer\Main",
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\5.0\Cache\Cookies",
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\ZoneMap"
                };

                foreach (string path in additionalPaths)
                {
                    try
                    {
                        LogMessage($"Tentative d'accès au registre: {path}");
                        ExtractFromRegistryPath(path, entries);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erreur chemin registre {path}: {ex.Message}");
                    }
                }
                
                // Méthode alternative pour Windows 7 : chercher dans toutes les sous-clés d'IntelliForms
                try
                {
                    ExtractFromIntelliFormsAllSubKeys(entries);
                }
                catch (Exception ex)
                {
                    LogMessage($"Erreur extraction IntelliForms complète: {ex.Message}");
                }
                
                LogMessage($"Fin ExtractFromRegistry: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromRegistry: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromRegistryPath(string registryPath, List<PasswordEntry> entries)
        {
            try
            {
                LogMessage($"ExtractFromRegistryPath: {registryPath}");
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
                {
                    if (key != null)
                    {
                        LogMessage($"Clé de registre ouverte: {registryPath}");
                        string[] valueNames = key.GetValueNames();
                        LogMessage($"Nombre de valeurs: {valueNames.Length}");
                        
                        foreach (string valueName in valueNames)
                        {
                            try
                            {
                                byte[] data = key.GetValue(valueName) as byte[];
                                if (data != null && data.Length > 0)
                                {
                                    string decrypted = DecryptDPAPI(data);
                                    if (!string.IsNullOrEmpty(decrypted))
                                    {
                                        // Parser les données pour extraire username et password
                                        ParseIEFormData(valueName, decrypted, entries);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Erreur valeur {valueName}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        LogMessage($"Clé de registre non trouvée: {registryPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromRegistryPath {registryPath}: {ex.Message}");
            }
        }

        private static void DiagnoseIERegistry()
        {
            try
            {
                LogMessage("=== DIAGNOSTIC DU REGISTRE IE ===");
                string[] pathsToCheck = new string[]
                {
                    @"Software\Microsoft\Internet Explorer\IntelliForms",
                    @"Software\Microsoft\Internet Explorer\IntelliForms\Storage1",
                    @"Software\Microsoft\Internet Explorer\IntelliForms\Storage2",
                    @"Software\Microsoft\Internet Explorer\IntelliForms\SPW",
                    @"Software\Microsoft\Internet Explorer\Main"
                };

                foreach (string path in pathsToCheck)
                {
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                LogMessage($"✓ Clé trouvée: {path}");
                                string[] subKeys = key.GetSubKeyNames();
                                string[] values = key.GetValueNames();
                                LogMessage($"  - Sous-clés: {subKeys.Length}, Valeurs: {values.Length}");
                                
                                if (subKeys.Length > 0)
                                {
                                    LogMessage($"  - Premières sous-clés: {string.Join(", ", subKeys.Take(5))}");
                                }
                            }
                            else
                            {
                                LogMessage($"✗ Clé non trouvée: {path}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"✗ Erreur accès {path}: {ex.Message}");
                    }
                }
                LogMessage("=== FIN DIAGNOSTIC ===");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR Diagnostic: {ex.Message}");
            }
        }

        private static void ExtractIEDataFromRegistryPath(RegistryKey rootKey, string iePath, List<PasswordEntry> entries)
        {
            try
            {
                LogMessage($"Extraction IE depuis: {iePath}");
                
                using (RegistryKey key = rootKey.OpenSubKey(iePath, RegistryKeyPermissionCheck.ReadSubTree))
                {
                    if (key != null)
                    {
                        LogMessage($"✓ Clé IE trouvée, lecture des sous-clés...");
                        string[] subKeyNames = key.GetSubKeyNames();
                        LogMessage($"Nombre de sous-clés trouvées: {subKeyNames.Length}");
                        
                        if (subKeyNames.Length == 0)
                        {
                            LogMessage($"ATTENTION: Aucune sous-clé trouvée dans {iePath}");
                            // Essayer de lire directement les valeurs de cette clé
                            string[] valueNames = key.GetValueNames();
                            LogMessage($"Nombre de valeurs directes: {valueNames.Length}");
                            foreach (string valueName in valueNames)
                            {
                                try
                                {
                                    object value = key.GetValue(valueName);
                                    byte[] data = value as byte[];
                                    if (data != null && data.Length > 0)
                                    {
                                        LogMessage($"Traitement valeur directe: {valueName} (taille: {data.Length} bytes)");
                                        string decrypted = DecryptDPAPI(data);
                                        if (!string.IsNullOrEmpty(decrypted))
                                        {
                                            ParseIEFormData(valueName, decrypted, entries);
                                        }
                                        else
                                        {
                                            TryAlternativeIEParsing(valueName, data, entries);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogMessage($"Erreur valeur directe {valueName}: {ex.Message}");
                                }
                            }
                        }
                        
                        foreach (string subKeyName in subKeyNames)
                        {
                            try
                            {
                                LogMessage($"Traitement sous-clé: {subKeyName}");
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree))
                                {
                                    if (subKey != null)
                                    {
                                        string url = DecodeUrl(subKeyName);
                                        LogMessage($"URL décodée: {url}");
                                        string[] valueNames = subKey.GetValueNames();
                                        LogMessage($"Nombre de valeurs dans cette sous-clé: {valueNames.Length}");

                                        if (valueNames.Length == 0)
                                        {
                                            LogMessage($"ATTENTION: Aucune valeur dans la sous-clé {subKeyName}");
                                        }

                                        foreach (string valueName in valueNames)
                                        {
                                            try
                                            {
                                                object value = subKey.GetValue(valueName, null, RegistryValueOptions.None);
                                                if (value != null)
                                                {
                                                    byte[] data = value as byte[];
                                                    if (data != null && data.Length > 0)
                                                    {
                                                        LogMessage($"Décryptage des données pour {valueName} (taille: {data.Length} bytes)...");
                                                        
                                                        // Afficher les premiers bytes pour diagnostic
                                                        string hexPreview = BitConverter.ToString(data.Take(16).ToArray()).Replace("-", " ");
                                                        LogMessage($"  Premiers bytes (hex): {hexPreview}");
                                                        
                                                        string decrypted = DecryptDPAPI(data);
                                                        if (!string.IsNullOrEmpty(decrypted))
                                                        {
                                                            LogMessage($"✓ Données décryptées avec succès (taille: {decrypted.Length} caractères), parsing...");
                                                            ParseIEFormData(url, decrypted, entries);
                                                        }
                                                        else
                                                        {
                                                            LogMessage($"✗ Échec du décryptage DPAPI pour {valueName}, essai méthode alternative...");
                                                            // Essayer une méthode alternative de parsing pour Windows 7
                                                            TryAlternativeIEParsing(url, data, entries);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LogMessage($"Valeur {valueName} n'est pas un tableau de bytes ou est vide");
                                                    }
                                                }
                                                else
                                                {
                                                    LogMessage($"Valeur {valueName} est null");
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                LogMessage($"ERREUR valeur {valueName}: {ex.Message}");
                                                LogMessage($"Stack Trace: {ex.StackTrace}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        LogMessage($"Impossible d'ouvrir la sous-clé: {subKeyName}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"ERREUR sous-clé {subKeyName}: {ex.Message}");
                                LogMessage($"Stack Trace: {ex.StackTrace}");
                            }
                        }
                    }
                    else
                    {
                        LogMessage($"✗ Clé IE non trouvée: {iePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractIEDataFromRegistryPath {iePath}: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromIntelliFormsAllSubKeys(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début extraction complète IntelliForms");
                string basePath = @"Software\Microsoft\Internet Explorer\IntelliForms";
                
                using (RegistryKey intelliFormsKey = Registry.CurrentUser.OpenSubKey(basePath))
                {
                    if (intelliFormsKey != null)
                    {
                        LogMessage($"Clé IntelliForms trouvée, recherche de toutes les sous-clés...");
                        string[] subKeys = intelliFormsKey.GetSubKeyNames();
                        LogMessage($"Nombre de sous-clés IntelliForms: {subKeys.Length}");
                        
                        foreach (string subKeyName in subKeys)
                        {
                            try
                            {
                                string fullPath = basePath + "\\" + subKeyName;
                                LogMessage($"Traitement sous-clé IntelliForms: {subKeyName}");
                                ExtractIEDataFromRegistryPath(Registry.CurrentUser, fullPath, entries);
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Erreur sous-clé IntelliForms {subKeyName}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        LogMessage($"Clé IntelliForms non trouvée: {basePath}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromIntelliFormsAllSubKeys: {ex.Message}");
            }
        }

        private static void TryAlternativeIEParsing(string url, byte[] data, List<PasswordEntry> entries)
        {
            try
            {
                LogMessage($"Tentative de parsing alternatif pour {url} (taille: {data.Length} bytes)");
                
                // Essayer de décoder directement en Unicode
                string unicodeString = Encoding.Unicode.GetString(data).TrimEnd('\0');
                if (!string.IsNullOrEmpty(unicodeString) && unicodeString.Length > 0)
                {
                    LogMessage($"Données Unicode trouvées ({unicodeString.Length} caractères), parsing...");
                    ParseIEFormData(url, unicodeString, entries);
                }
                
                // Essayer aussi en ASCII
                string asciiString = Encoding.ASCII.GetString(data).TrimEnd('\0');
                if (!string.IsNullOrEmpty(asciiString) && asciiString != unicodeString && asciiString.Length > 0)
                {
                    LogMessage($"Données ASCII trouvées ({asciiString.Length} caractères), parsing...");
                    ParseIEFormData(url, asciiString, entries);
                }
                
                // Essayer UTF8
                try
                {
                    string utf8String = Encoding.UTF8.GetString(data).TrimEnd('\0');
                    if (!string.IsNullOrEmpty(utf8String) && utf8String != unicodeString && utf8String != asciiString && utf8String.Length > 0)
                    {
                        LogMessage($"Données UTF8 trouvées ({utf8String.Length} caractères), parsing...");
                        ParseIEFormData(url, utf8String, entries);
                    }
                }
                catch { }
                
                // Si les données semblent être en clair (contiennent beaucoup de caractères imprimables)
                int printableCount = 0;
                foreach (byte b in data)
                {
                    if (b >= 32 && b <= 126) // Caractères ASCII imprimables
                        printableCount++;
                }
                if (printableCount > data.Length * 0.8) // Plus de 80% de caractères imprimables
                {
                    string plainText = Encoding.ASCII.GetString(data);
                    LogMessage($"Données semblent être en clair ({printableCount}/{data.Length} caractères imprimables), parsing...");
                    ParseIEFormData(url, plainText, entries);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR parsing alternatif pour {url}: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static string DecryptDPAPI(byte[] encryptedData)
        {
            IntPtr dataInPtr = IntPtr.Zero;
            IntPtr dataOutPtr = IntPtr.Zero;
            
            try
            {
                if (encryptedData == null || encryptedData.Length == 0)
                {
                    LogMessage("DecryptDPAPI: Données vides ou null");
                    return "";
                }

                DATA_BLOB dataIn = new DATA_BLOB();
                DATA_BLOB dataOut = new DATA_BLOB();
                DATA_BLOB entropy = new DATA_BLOB();
                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();

                dataIn.cbData = encryptedData.Length;
                dataIn.pbData = Marshal.AllocHGlobal(encryptedData.Length);
                dataInPtr = dataIn.pbData;
                Marshal.Copy(encryptedData, 0, dataIn.pbData, encryptedData.Length);

                prompt.cbSize = Marshal.SizeOf(typeof(CRYPTPROTECT_PROMPTSTRUCT));
                prompt.dwPromptFlags = 0;
                prompt.hwndApp = IntPtr.Zero;
                prompt.szPrompt = null;

                // Essayer avec différents flags pour Windows 7
                int[] flagsToTry = new int[] { 0, 0x1 }; // 0 = normal, 0x1 = CRYPTPROTECT_UI_FORBIDDEN
                
                foreach (int flags in flagsToTry)
                {
                    try
                    {
                        if (CryptUnprotectData(ref dataIn, null, ref entropy, IntPtr.Zero, ref prompt, flags, ref dataOut))
                {
                    dataOutPtr = dataOut.pbData;
                    byte[] decryptedBytes = new byte[dataOut.cbData];
                    Marshal.Copy(dataOut.pbData, decryptedBytes, 0, dataOut.cbData);
                    
                    // Libérer la mémoire allouée par CryptUnprotectData
                    if (dataOut.pbData != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(dataOut.pbData);
                                dataOutPtr = IntPtr.Zero;
                            }
                            
                            string result = Encoding.Unicode.GetString(decryptedBytes).TrimEnd('\0');
                            LogMessage($"DecryptDPAPI: Décryptage réussi (taille résultat: {result.Length} caractères)");
                            return result;
                        }
                        else
                        {
                            int error = Marshal.GetLastWin32Error();
                            LogMessage($"DecryptDPAPI: Échec avec flags {flags}, erreur Windows: {error}");
                }
            }
            catch (Exception ex)
            {
                        LogMessage($"DecryptDPAPI: Exception avec flags {flags}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR DecryptDPAPI: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
            finally
            {
                if (dataInPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(dataInPtr);
                }
            }

            return "";
        }

        private static string DecryptChromiumPassword(byte[] encryptedData)
        {
            try
            {
                if (encryptedData == null || encryptedData.Length == 0)
                    return string.Empty;

                // Les navigateurs Chromium stockent les mots de passe avec un préfixe de version
                // Format: "v10" ou "v11" suivi des données chiffrées avec DPAPI
                if (encryptedData.Length < 3)
                    return string.Empty;

                // Vérifier le préfixe de version (v10 ou v11)
                string versionPrefix = Encoding.ASCII.GetString(encryptedData, 0, 3);
                if (versionPrefix != "v10" && versionPrefix != "v11")
                {
                    // Si pas de préfixe, essayer de déchiffrer directement avec DPAPI
                    return DecryptDPAPI(encryptedData);
                }

                // Extraire les données chiffrées (tout sauf le préfixe de 3 bytes)
                byte[] encryptedPassword = new byte[encryptedData.Length - 3];
                Array.Copy(encryptedData, 3, encryptedPassword, 0, encryptedPassword.Length);

                // Déchiffrer avec DPAPI
                return DecryptDPAPI(encryptedPassword);
            }
            catch (Exception ex)
            {
                LogMessage($"Erreur décryptage Chromium: {ex.Message}");
                return string.Empty;
            }
        }

        private static void ParseIEFormData(string url, string data, List<PasswordEntry> entries)
        {
            try
            {
                if (string.IsNullOrEmpty(data))
                    return;

                LogMessage($"Parsing des données IE pour URL: {url} (taille: {data.Length} caractères)");
                LogMessage($"Données brutes (premiers 100 caractères): {data.Substring(0, Math.Min(100, data.Length))}");
                
                // Nettoyer les données en supprimant les caractères de contrôle sauf ceux utiles
                StringBuilder cleanedData = new StringBuilder();
                foreach (char c in data)
                {
                    if (!char.IsControl(c) || c == '\r' || c == '\n' || c == '\t' || c == '\0')
                    {
                        cleanedData.Append(c);
                    }
                }
                string cleanData = cleanedData.ToString();
                
                // Format typique d'IE: les données sont séparées par des caractères null
                // Mais parfois c'est juste username\0password\0 ou d'autres formats
                string[] parts = cleanData.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                
                LogMessage($"Nombre de parties après split null: {parts.Length}");
                
                // Si pas de séparateurs null, essayer d'autres méthodes
                if (parts.Length == 0 || (parts.Length == 1 && parts[0].Length == cleanData.Length))
                {
                    // Essayer de parser avec d'autres séparateurs
                    parts = cleanData.Split(new char[] { '\r', '\n', '\t', (char)0x01, (char)0x02, (char)0x1E }, StringSplitOptions.RemoveEmptyEntries);
                    LogMessage($"Nombre de parties après split alternatif: {parts.Length}");
                }
                
                // Si toujours rien, essayer de chercher des patterns dans les données
                if (parts.Length == 0)
                {
                    // Chercher des patterns comme email@domain.com ou des URLs
                    System.Text.RegularExpressions.Regex emailRegex = new System.Text.RegularExpressions.Regex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b");
                    System.Text.RegularExpressions.Regex urlRegex = new System.Text.RegularExpressions.Regex(@"https?://[^\s]+|www\.[^\s]+");
                    
                    var emailMatches = emailRegex.Matches(cleanData);
                    var urlMatches = urlRegex.Matches(cleanData);
                    
                    if (emailMatches.Count > 0 || urlMatches.Count > 0)
                    {
                        // Extraire les parties intéressantes
                        List<string> extractedParts = new List<string>();
                        foreach (System.Text.RegularExpressions.Match match in emailMatches)
                        {
                            extractedParts.Add(match.Value);
                        }
                        foreach (System.Text.RegularExpressions.Match match in urlMatches)
                        {
                            extractedParts.Add(match.Value);
                        }
                        parts = extractedParts.ToArray();
                        LogMessage($"Patterns trouvés: {parts.Length} parties");
                    }
                }
                
                if (parts.Length == 0)
                {
                    // Dernière tentative : traiter toute la chaîne comme un seul mot de passe si elle semble valide
                    string singlePassword = cleanData.Trim();
                    singlePassword = CleanPassword(singlePassword);
                    if (!string.IsNullOrEmpty(singlePassword) && IsValidPassword(singlePassword) && singlePassword.Length >= 3)
                    {
                        entries.Add(new PasswordEntry
                        {
                            EntryName = url,
                            Type = "Internet Explorer",
                            StoredIn = "Registry (AutoComplete)",
                            UserName = "",
                            Password = singlePassword
                        });
                        LogMessage($"✓ Entrée ajoutée (password unique) pour {url}");
                    }
                    return;
                }

                string username = "";
                string password = "";
                List<string> validParts = new List<string>();

                // Filtrer et nettoyer les parties
                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i].Trim();
                    if (string.IsNullOrEmpty(part))
                        continue;

                    // Filtrer les parties trop longues ou trop courtes
                    if (part.Length < 1 || part.Length > 200)
                        continue;
                    
                    // Vérifier si c'est principalement du texte imprimable
                    int printableChars = 0;
                    int letterOrDigitChars = 0;
                    foreach (char c in part)
                    {
                        if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || c == ' ' || c == '@' || c == '.' || c == '-' || c == '_')
                            printableChars++;
                        if (char.IsLetterOrDigit(c))
                            letterOrDigitChars++;
                    }
                    
                    // Accepter si au moins 60% de caractères imprimables et au moins 30% de lettres/chiffres
                    if (printableChars >= part.Length * 0.6 && letterOrDigitChars >= part.Length * 0.3)
                    {
                        validParts.Add(part);
                        LogMessage($"Partie valide {validParts.Count}: {part.Substring(0, Math.Min(50, part.Length))}...");
                    }
                }

                // Identifier username et password
                if (validParts.Count > 0)
                {
                    // Le premier élément valide est souvent le username (surtout si c'est un email)
                    username = validParts[0];
                    
                    // Chercher un email comme username
                    foreach (string part in validParts)
                    {
                        if (part.Contains("@") && part.Contains(".") && part.Length > 5 && part.Length < 100)
                    {
                        username = part;
                            LogMessage($"Username (email) identifié: {username}");
                            break;
                        }
                    }
                    
                    // Le password est généralement le deuxième élément ou le dernier
                    if (validParts.Count > 1)
                    {
                        // Chercher le password (généralement plus long et sans @)
                        for (int i = 1; i < validParts.Count; i++)
                        {
                            string part = validParts[i];
                            // Si ce n'est pas un email et que c'est différent du username
                            if (!part.Contains("@") && part != username && part.Length >= 3)
                    {
                        password = part;
                                LogMessage($"Password identifié: {new string('*', Math.Min(password.Length, 20))}");
                                break;
                            }
                        }
                        
                        // Si pas trouvé, prendre le dernier élément
                        if (string.IsNullOrEmpty(password) && validParts.Count > 1)
                        {
                            password = validParts[validParts.Count - 1];
                            LogMessage($"Password (dernier élément) identifié");
                        }
                    }
                    else if (validParts.Count == 1)
                    {
                        // Une seule partie : c'est probablement le password
                        if (!validParts[0].Contains("@"))
                        {
                            password = validParts[0];
                            username = "";
                    LogMessage($"Password unique identifié");
                        }
                    }
                }

                // Nettoyer le mot de passe avant de l'ajouter
                password = CleanPassword(password);
                username = username.Trim();
                
                // Extraire l'URL réelle si possible depuis les données
                string realUrl = url;
                if (validParts.Count > 0)
                {
                    foreach (string part in validParts)
                    {
                        if ((part.StartsWith("http://") || part.StartsWith("https://") || part.Contains("www.")) && part.Contains("."))
                        {
                            realUrl = part;
                            LogMessage($"URL réelle trouvée dans les données: {realUrl}");
                            break;
                        }
                    }
                }
                
                // Si l'URL est toujours en hexadécimal, essayer de la décoder
                if (System.Text.RegularExpressions.Regex.IsMatch(realUrl, @"^[0-9A-Fa-f]+$"))
                {
                    realUrl = DecodeUrl(realUrl);
                }
                
                // Ajouter seulement si on a au moins un password valide
                if (!string.IsNullOrEmpty(password) && IsValidPassword(password) && password.Length >= 3)
                {
                    // Vérifier si cette entrée existe déjà (éviter les doublons)
                    bool exists = entries.Any(e => e.EntryName == realUrl && e.UserName == username && e.Password == password);
                    if (!exists)
                {
                    entries.Add(new PasswordEntry
                    {
                            EntryName = realUrl,
                            Type = "Internet Explorer",
                            StoredIn = "Registry (AutoComplete)",
                        UserName = username,
                        Password = password
                    });
                        LogMessage($"✓ Entrée ajoutée pour {realUrl}: User={username}, Password={new string('*', Math.Min(password.Length, 20))}");
                }
                else
                {
                        LogMessage($"Entrée déjà présente pour {realUrl}, ignorée");
                    }
                }
                else
                {
                    LogMessage($"Aucun password valide trouvé pour {url} (password vide ou invalide: '{password}')");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ParseIEFormData pour {url}: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromEdge(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromEdge");
                
                // Edge stocke ses mots de passe dans une base de données SQLite
                // Chemin typique: %LOCALAPPDATA%\Microsoft\Edge\User Data\Default\Login Data
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string edgePath = Path.Combine(localAppData, @"Microsoft\Edge\User Data\Default\Login Data");
                
                LogMessage($"Recherche de la base de données Edge: {edgePath}");
                
                if (File.Exists(edgePath))
                {
                    LogMessage("Base de données Edge trouvée");
                    ExtractFromEdgeDatabase(edgePath, entries);
                }
                else
                {
                    LogMessage($"Base de données Edge non trouvée à: {edgePath}");
                    
                    // Essayer aussi avec des profils alternatifs
                    string edgeUserDataPath = Path.Combine(localAppData, @"Microsoft\Edge\User Data");
                    if (Directory.Exists(edgeUserDataPath))
                    {
                        LogMessage($"Recherche dans les profils Edge: {edgeUserDataPath}");
                        string[] profiles = Directory.GetDirectories(edgeUserDataPath);
                        foreach (string profile in profiles)
                        {
                            string loginDataPath = Path.Combine(profile, "Login Data");
                            if (File.Exists(loginDataPath))
                            {
                                LogMessage($"Profil Edge trouvé: {profile}");
                                ExtractFromEdgeDatabase(loginDataPath, entries);
                            }
                        }
                    }
                }
                
                // Les mots de passe Edge peuvent aussi être dans Credential Manager
                // (déjà capturés par ExtractFromCredentialManager)
                LogMessage("Les mots de passe Edge dans Credential Manager sont déjà extraits");
                LogMessage($"Fin ExtractFromEdge: {entries.Count} entrées Edge trouvées");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromEdge: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromEdgeDatabase(string dbPath, List<PasswordEntry> entries)
        {
            try
            {
                LogMessage($"Tentative d'extraction depuis la base Edge: {dbPath}");
                
                // Copier la base de données pour pouvoir la lire (Edge peut la verrouiller)
                string tempDbPath = Path.Combine(Path.GetTempPath(), $"EdgeLoginData_{Guid.NewGuid()}.db");
                
                try
                {
                    File.Copy(dbPath, tempDbPath, true);
                    LogMessage($"Base de données copiée vers: {tempDbPath}");
                    
                    // Utiliser System.Data.SQLite n'est pas disponible sans dépendance
                    // Mais on peut lire directement les données binaires et chercher les patterns
                    // Pour l'instant, on note que la base existe mais on ne peut pas la lire sans SQLite
                    LogMessage("Note: L'extraction SQLite nécessiterait System.Data.SQLite (non inclus)");
                    LogMessage("Les mots de passe Edge sont principalement dans Credential Manager");
                    
                    // Supprimer le fichier temporaire
                    try { File.Delete(tempDbPath); } catch { }
                }
                catch (Exception ex)
                {
                    LogMessage($"Erreur copie base Edge: {ex.Message}");
                    // Edge peut verrouiller le fichier, c'est normal
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromEdgeDatabase: {ex.Message}");
            }
        }

        private static void ExtractFTPPasswords(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFTPPasswords");
                
                // FTP passwords sont stockés dans plusieurs endroits
                // 1. Credential Manager (déjà extrait)
                // 2. Registre Windows
                string[] ftpRegistryPaths = new string[]
                {
                    @"Software\Microsoft\FTP\Accounts",
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings\5.0\Cache\Cookies"
                };

                foreach (string path in ftpRegistryPaths)
                {
                    try
                    {
                        LogMessage($"Recherche FTP dans: {path}");
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(path))
                        {
                            if (key != null)
                            {
                                LogMessage($"Clé FTP trouvée: {path}");
                                string[] subKeys = key.GetSubKeyNames();
                                foreach (string subKeyName in subKeys)
                                {
                                    try
                                    {
                                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                                        {
                                            if (subKey != null)
                                            {
                                                string url = subKeyName;
                                                string username = "";
                                                string password = "";
                                                
                                                foreach (string valueName in subKey.GetValueNames())
                                                {
                                                    object value = subKey.GetValue(valueName);
                                                    if (value != null)
                                                    {
                                                        if (valueName.ToLower().Contains("user") || valueName.ToLower().Contains("login"))
                                                        {
                                                            username = value.ToString();
                                                        }
                                                        else if (valueName.ToLower().Contains("pass"))
                                                        {
                                                            if (value is byte[])
                                                            {
                                                                password = DecryptDPAPI((byte[])value);
                                                            }
                                                            else
                                                            {
                                                                password = value.ToString();
                                                            }
                                                        }
                                                    }
                                                }
                                                
                                                if (!string.IsNullOrEmpty(password) || !string.IsNullOrEmpty(username))
                                                {
                                                    entries.Add(new PasswordEntry
                                                    {
                                                        EntryName = url,
                                                        Type = "FTP",
                                                        StoredIn = "Registry",
                                                        UserName = username,
                                                        Password = password
                                                    });
                                                    LogMessage($"✓ FTP ajouté: {url}");
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"Erreur sous-clé FTP {subKeyName}: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erreur chemin FTP {path}: {ex.Message}");
                    }
                }
                
                LogMessage($"Fin ExtractFTPPasswords: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFTPPasswords: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static string DecodeUrl(string encodedUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(encodedUrl))
                    return encodedUrl;

                LogMessage($"Décodage URL: {encodedUrl}");
                
                // Si c'est une chaîne hexadécimale (que des caractères 0-9, A-F), essayer de la décoder
                if (System.Text.RegularExpressions.Regex.IsMatch(encodedUrl, @"^[0-9A-Fa-f]+$"))
                {
                    try
                    {
                        // Convertir hexadécimal en bytes puis en string Unicode
                        byte[] hexBytes = new byte[encodedUrl.Length / 2];
                        for (int i = 0; i < hexBytes.Length; i++)
                        {
                            hexBytes[i] = Convert.ToByte(encodedUrl.Substring(i * 2, 2), 16);
                        }
                        
                        // Essayer Unicode
                        string unicodeDecoded = Encoding.Unicode.GetString(hexBytes).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(unicodeDecoded) && unicodeDecoded.Length > 0)
                        {
                            // Nettoyer les caractères de contrôle
                            unicodeDecoded = new string(unicodeDecoded.Where(c => !char.IsControl(c) || c == '\r' || c == '\n').ToArray());
                            if (unicodeDecoded.Length > 0 && (unicodeDecoded.Contains("http://") || unicodeDecoded.Contains("https://") || unicodeDecoded.Contains("www.") || unicodeDecoded.Contains(".com") || unicodeDecoded.Contains(".net")))
                            {
                                LogMessage($"URL décodée depuis hex (Unicode): {unicodeDecoded}");
                                return unicodeDecoded;
                            }
                        }
                        
                        // Essayer ASCII
                        string asciiDecoded = Encoding.ASCII.GetString(hexBytes).TrimEnd('\0');
                        if (!string.IsNullOrEmpty(asciiDecoded) && asciiDecoded.Length > 0 && asciiDecoded != unicodeDecoded)
                        {
                            asciiDecoded = new string(asciiDecoded.Where(c => !char.IsControl(c) || c == '\r' || c == '\n').ToArray());
                            if (asciiDecoded.Length > 0 && (asciiDecoded.Contains("http://") || asciiDecoded.Contains("https://") || asciiDecoded.Contains("www.") || asciiDecoded.Contains(".com")))
                            {
                                LogMessage($"URL décodée depuis hex (ASCII): {asciiDecoded}");
                                return asciiDecoded;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Erreur décodage hex: {ex.Message}");
                    }
                }
                
                // Décodage URL standard
                string decoded = encodedUrl;
                
                // Remplacement des caractères encodés courants
                decoded = decoded.Replace("%3A", ":");
                decoded = decoded.Replace("%2F", "/");
                decoded = decoded.Replace("%3F", "?");
                decoded = decoded.Replace("%3D", "=");
                decoded = decoded.Replace("%26", "&");
                decoded = decoded.Replace("%20", " ");
                decoded = decoded.Replace("%2E", ".");
                decoded = decoded.Replace("%2D", "-");
                decoded = decoded.Replace("%5F", "_");
                
                // Décodage URL complet si possible
                try
                {
                    decoded = Uri.UnescapeDataString(decoded);
                }
                catch { }
                
                // Si ça ressemble à une URL valide, la retourner
                if (decoded.Contains("http://") || decoded.Contains("https://") || decoded.Contains("www.") || decoded.Contains(".com") || decoded.Contains(".net") || decoded.Contains(".org"))
                {
                    LogMessage($"URL décodée (standard): {decoded}");
                return decoded;
                }
                
                // Si ça ne ressemble toujours pas à une URL, essayer de chercher dans les données décryptées
                // Pour l'instant, retourner la valeur originale
                LogMessage($"URL non décodée, utilisation originale: {encodedUrl}");
                return encodedUrl;
            }
            catch (Exception ex)
            {
                LogMessage($"Erreur décodage URL {encodedUrl}: {ex.Message}");
                return encodedUrl;
            }
        }

        private static void ExtractFromChrome(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromChrome");
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string chromePath = Path.Combine(localAppData, @"Google\Chrome\User Data\Default\Login Data");
                
                LogMessage($"Recherche de la base de données Chrome: {chromePath}");
                
                if (File.Exists(chromePath))
                {
                    LogMessage("Base de données Chrome trouvée");
                    ExtractFromChromiumDatabase(chromePath, entries, "Google Chrome");
                }
                else
                {
                    // Essayer avec des profils alternatifs
                    string chromeUserDataPath = Path.Combine(localAppData, @"Google\Chrome\User Data");
                    if (Directory.Exists(chromeUserDataPath))
                    {
                        LogMessage($"Recherche dans les profils Chrome: {chromeUserDataPath}");
                        string[] profiles = Directory.GetDirectories(chromeUserDataPath);
                        foreach (string profile in profiles)
                        {
                            string loginDataPath = Path.Combine(profile, "Login Data");
                            if (File.Exists(loginDataPath))
                            {
                                LogMessage($"Profil Chrome trouvé: {profile}");
                                ExtractFromChromiumDatabase(loginDataPath, entries, "Google Chrome");
                            }
                        }
                    }
                }
                
                LogMessage($"Fin ExtractFromChrome: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromChrome: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromOpera(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromOpera");
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string operaPath = Path.Combine(appData, @"Opera Software\Opera Stable\Login Data");
                
                LogMessage($"Recherche de la base de données Opera: {operaPath}");
                
                if (File.Exists(operaPath))
                {
                    LogMessage("Base de données Opera trouvée");
                    ExtractFromChromiumDatabase(operaPath, entries, "Opera");
                }
                else
                {
                    // Essayer avec des versions alternatives
                    string operaBasePath = Path.Combine(appData, @"Opera Software");
                    if (Directory.Exists(operaBasePath))
                    {
                        string[] operaVersions = Directory.GetDirectories(operaBasePath);
                        foreach (string version in operaVersions)
                        {
                            string loginDataPath = Path.Combine(version, "Login Data");
                            if (File.Exists(loginDataPath))
                            {
                                LogMessage($"Version Opera trouvée: {version}");
                                ExtractFromChromiumDatabase(loginDataPath, entries, "Opera");
                            }
                        }
                    }
                }
                
                LogMessage($"Fin ExtractFromOpera: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromOpera: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromBrave(List<PasswordEntry> entries)
        {
            try
            {
                LogMessage("Début ExtractFromBrave");
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string bravePath = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data\Default\Login Data");
                
                LogMessage($"Recherche de la base de données Brave: {bravePath}");
                
                if (File.Exists(bravePath))
                {
                    LogMessage("Base de données Brave trouvée");
                    ExtractFromChromiumDatabase(bravePath, entries, "Brave");
                }
                else
                {
                    // Essayer avec des profils alternatifs
                    string braveUserDataPath = Path.Combine(localAppData, @"BraveSoftware\Brave-Browser\User Data");
                    if (Directory.Exists(braveUserDataPath))
                    {
                        LogMessage($"Recherche dans les profils Brave: {braveUserDataPath}");
                        string[] profiles = Directory.GetDirectories(braveUserDataPath);
                        foreach (string profile in profiles)
                        {
                            string loginDataPath = Path.Combine(profile, "Login Data");
                            if (File.Exists(loginDataPath))
                            {
                                LogMessage($"Profil Brave trouvé: {profile}");
                                ExtractFromChromiumDatabase(loginDataPath, entries, "Brave");
                            }
                        }
                    }
                }
                
                LogMessage($"Fin ExtractFromBrave: {entries.Count} entrées totales");
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromBrave: {ex.Message}");
                LogMessage($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void ExtractFromChromiumDatabase(string dbPath, List<PasswordEntry> entries, string browserName)
        {
            try
            {
                LogMessage($"Tentative d'extraction depuis la base {browserName}: {dbPath}");

                string tempDbPath = Path.Combine(Path.GetTempPath(), $"{browserName}LoginData_{Guid.NewGuid()}.db");
                
                try
                {
                    File.Copy(dbPath, tempDbPath, true);
                    LogMessage($"Base de données copiée vers: {tempDbPath}");

                    string connectionString = $"Data Source={tempDbPath};Version=3;New=False;Compress=True;";
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        LogMessage($"Connexion à la base {browserName} réussie");
                        
                        string sql = "SELECT origin_url, username_value, password_value FROM logins";
                        using (SQLiteCommand command = new SQLiteCommand(sql, connection))
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            int entryCount = 0;
                            int successCount = 0;
                            
                            while (reader.Read())
                            {
                                entryCount++;
                                try
                                {
                                    string url = reader.IsDBNull(0) ? string.Empty : reader.GetString(0);
                                    string username = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                                    byte[] encryptedPasswordBytes = reader.IsDBNull(2) ? null : (byte[])reader[2];

                                    if (string.IsNullOrEmpty(url))
                                    {
                                        LogMessage($"Entrée {entryCount} ignorée: URL vide");
                                        continue;
                                    }
                                    
                                    if (encryptedPasswordBytes == null || encryptedPasswordBytes.Length == 0)
                                    {
                                        LogMessage($"Entrée {entryCount} ignorée: pas de mot de passe pour {url}");
                                        continue;
                                    }

                                    LogMessage($"Traitement entrée {entryCount}: {url} | User: {username} | Password bytes: {encryptedPasswordBytes.Length}");
                                    
                                    string decryptedPassword = DecryptChromiumPassword(encryptedPasswordBytes);

                                    if (string.IsNullOrEmpty(decryptedPassword))
                                    {
                                        LogMessage($"Échec déchiffrement pour {url}");
                                        continue;
                                    }
                                    
                                    if (!IsValidPassword(decryptedPassword))
                                    {
                                        LogMessage($"Mot de passe invalide pour {url}");
                                        continue;
                                    }

                                    entries.Add(new PasswordEntry
                                    {
                                        EntryName = url,
                                        Type = browserName,
                                        StoredIn = $"{browserName} Database",
                                        UserName = username,
                                        Password = decryptedPassword
                                    });
                                    successCount++;
                                    LogMessage($"✓ Ajouté {browserName}: {url} | User: {username}");
                                }
                                catch (Exception ex)
                                {
                                    LogMessage($"Erreur lecture entrée {entryCount} {browserName}: {ex.Message}");
                                    LogMessage($"Stack Trace: {ex.StackTrace}");
                                }
                            }
                            
                            LogMessage($"Extraction {browserName} terminée: {successCount} entrées réussies sur {entryCount} totales");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"ERREUR copie/lecture base {browserName}: {ex.Message}");
                    LogMessage($"Stack Trace: {ex.StackTrace}");
                }
                finally
                {
                    if (File.Exists(tempDbPath))
                    {
                        try { File.Delete(tempDbPath); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERREUR ExtractFromChromiumDatabase {browserName}: {ex.Message}");
            }
        }
    }
}
