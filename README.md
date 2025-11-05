# DaryPWD - Application d'extraction de mots de passe Internet Explorer et Microsoft Edge

![Version](https://img.shields.io/badge/version-1.0-blue.svg)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue.svg)
![Windows](https://img.shields.io/badge/Windows-7%2B-blue.svg)

## 📋 Description

DaryPWD est une application Windows portable qui permet d'extraire et d'afficher les mots de passe stockés par Internet Explorer et Microsoft Edge. L'application fonctionne avec toutes les versions de Windows et ne nécessite aucune dépendance supplémentaire.

**Développé par :**  By Dary  
**Contact :** darydialo@gmail.com

## ✨ Fonctionnalités

- ✅ Extraction automatique des mots de passe depuis le Credential Manager Windows
- ✅ Extraction des mots de passe AutoComplete depuis le Registre Windows (Internet Explorer)
- ✅ Extraction des mots de passe Microsoft Edge (via Credential Manager)
- ✅ Support pour FTP, HTTP Authentication et AutoComplete
- ✅ Affichage des résultats dans un tableau avec colonnes: Entry Name, Type, Stored In, User Name, Password
- ✅ Recherche et filtrage en temps réel
- ✅ Masquage/affichage des mots de passe
- ✅ Export en format TXT, CSV, HTML, XML
- ✅ Édition et suppression des entrées
- ✅ Copie des entrées sélectionnées dans le presse-papiers
- ✅ Rafraîchissement de la liste des mots de passe
- ✅ Interface utilisateur moderne et intuitive

## 🌐 Navigateurs supportés

- **Internet Explorer** : Extraction depuis le registre Windows (IntelliForms) et Credential Manager
- **Microsoft Edge** : Extraction depuis le Credential Manager Windows
- **Chrome/Opera/Brave** : Détection des bases de données (extraction complète nécessite des dépendances supplémentaires)

## 📦 Types de mots de passe extraits

1. **Saisie automatique (AutoComplete)** : Mots de passe enregistrés dans les formulaires web
2. **Authentification HTTP** : Mots de passe pour les sites web protégés
3. **Mots de passe FTP** : Mots de passe pour les serveurs FTP

## 🚀 Installation

### Prérequis
- Windows 7 ou supérieur
- .NET Framework 4.7.2 (généralement préinstallé sur Windows 10/11)

### Utilisation simple
1. Téléchargez `DaryPWD.exe` depuis la section Releases
2. Exécutez l'application (aucune installation requise)
3. Les mots de passe sont automatiquement extraits au démarrage

## 🔨 Compilation depuis les sources

### Prérequis pour la compilation
- Visual Studio 2017 ou supérieur
- .NET Framework 4.7.2 SDK

### Étapes de compilation

#### Option 1 : Utiliser Visual Studio
1. Ouvrir le fichier `DaryPWD.csproj` dans Visual Studio
2. Sélectionner la configuration **Release**
3. Compiler le projet (Build → Build Solution)
4. L'exécutable sera généré dans `bin\Release\DaryPWD.exe`

#### Option 2 : Utiliser MSBuild (ligne de commande)
```batch
msbuild DaryPWD.csproj /p:Configuration=Release /p:Platform=AnyCPU /t:Rebuild
```

#### Option 3 : Utiliser le script fourni
```batch
scripts\Build.bat
```

## 📖 Utilisation

1. **Lancer l'application** : Double-cliquez sur `DaryPWD.exe`
2. **Attendre l'extraction** : Les mots de passe sont automatiquement extraits au démarrage
3. **Utiliser les fonctionnalités** :
   - **Barre de recherche** : Filtrer les entrées en temps réel
   - **Bouton "Show Passwords"** : Masquer/afficher les mots de passe
   - **Menu Edit** : Modifier ou supprimer une entrée sélectionnée
   - **Menu File → Export** : Exporter en TXT, CSV, HTML ou XML
   - **Bouton "Copy"** : Copier l'entrée sélectionnée dans le presse-papiers
   - **Bouton "Refresh"** : Rafraîchir la liste des mots de passe

## 🎯 Fonctionnalités avancées

### Édition des entrées
- Double-cliquez sur une cellule "User Name" ou "Password" pour éditer directement
- Ou utilisez le menu **Edit → Edit Entry...** pour ouvrir un formulaire d'édition

### Export des données
- **TXT** : Format texte simple avec toutes les informations
- **CSV** : Format compatible avec Excel et autres tableurs
- **HTML** : Format HTML avec tableau stylisé
- **XML** : Format XML structuré pour traitement automatique

## 🔒 Sécurité et confidentialité

- ⚠️ **Avertissement** : Cette application est destinée à la récupération de **vos propres mots de passe** uniquement
- ✅ Aucune connexion réseau : Toutes les données restent locales
- ✅ Aucune transmission de données : Aucune information n'est envoyée à l'extérieur
- ✅ Portable : Aucune installation requise, aucun fichier système modifié

## 📋 Compatibilité

| Système | Version | Support |
|---------|---------|---------|
| Windows 11 | Toutes versions | ✅ |
| Windows 10 | Toutes versions | ✅ |
| Windows 8.1 | Toutes versions | ✅ |
| Windows 8 | Toutes versions | ✅ |
| Windows 7 | SP1+ | ✅ |

## ⚙️ Notes techniques

- L'application nécessite des droits administrateur pour accéder à certaines données du registre
- Les mots de passe sont extraits depuis le système Windows actuel uniquement
- L'application utilise les APIs Windows natives (`CredEnumerate`, `CryptUnprotectData`) pour décrypter les mots de passe
- Aucune dépendance externe requise (100% portable)

## 📁 Structure du projet

```
DaryPWD/
├── src/                    # Fichiers sources
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   ├── IEPasswordExtractor.cs
│   └── Program.cs
├── resources/              # Ressources (icônes, images)
│   ├── DaryPWD.ico
│   └── DaryPWD.png
├── docs/                   # Documentation
│   └── AMELIORATIONS.md
├── scripts/                # Scripts utilitaires
│   ├── Build.bat
│   ├── TestApp.bat
│   └── ...
├── bin/                    # Fichiers compilés
│   └── Release/
│       └── DaryPWD.exe
├── Properties/             # Propriétés du projet
├── Build.bat              # Script de compilation
├── DaryPWD.csproj         # Fichier projet
├── App.config             # Configuration
├── LICENSE                 # Licence MIT
├── .gitignore             # Fichiers à ignorer (Git)
└── README.md              # Ce fichier
```

## 🐛 Dépannage

### L'application ne trouve aucun mot de passe
- Vérifiez que vous avez utilisé Internet Explorer ou Microsoft Edge pour enregistrer des mots de passe
- Assurez-vous que l'application est exécutée avec les droits appropriés
- Vérifiez que des sites web ont été consultés et que les mots de passe ont été sauvegardés

### L'application ne démarre pas
- Vérifiez que .NET Framework 4.7.2 est installé
- Exécutez l'application en tant qu'administrateur
- Consultez le fichier `DaryPWD.log` pour plus d'informations

### Erreurs de compilation
- Vérifiez que Visual Studio 2017+ est installé
- Assurez-vous que .NET Framework 4.7.2 SDK est installé
- Utilisez MSBuild depuis la ligne de commande avec les chemins complets

## 📝 Licence

Cette application est fournie sous licence MIT. Voir le fichier `LICENSE` pour plus de détails.

## 👨‍💻 Développeur

**Juste By Dary**  
📧 Email : darydialo@gmail.com

## 🙏 Remerciements

Merci d'utiliser DaryPWD ! Si cette application vous est utile, n'hésitez pas à partager vos retours et suggestions.

## 📌 Version

**Version actuelle :** 1.0  
**Dernière mise à jour :** 2025

---

⚠️ **Avertissement légal** : Cette application est destinée à la récupération de vos propres mots de passe. Utilisez-la de manière responsable et conforme aux lois locales. L'auteur n'est pas responsable de l'utilisation abusive de cet outil.
