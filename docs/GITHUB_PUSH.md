# Guide rapide pour pousser vers GitHub

## ✅ État actuel
- ✅ Repository Git initialisé
- ✅ Tous les fichiers ajoutés
- ✅ Commit initial créé
- ✅ Branche renommée en `main`

## 🚀 Étapes pour publier sur GitHub

### Option 1 : Utiliser le script automatique

```batch
scripts\PushToGitHub.bat
```

Le script vous demandera l'URL de votre repository GitHub.

### Option 2 : Commandes manuelles

1. **Créer le repository sur GitHub**
   - Allez sur https://github.com/new
   - Nom : `DaryPWD`
   - Description : `Application d'extraction de mots de passe Internet Explorer et Microsoft Edge`
   - Public ou Private (selon votre choix)
   - **NE PAS** cocher "Initialize with README"
   - Cliquez sur "Create repository"

2. **Ajouter le remote GitHub**
   ```bash
   git remote add origin https://github.com/VOTRE_USERNAME/DaryPWD.git
   ```
   Remplacez `VOTRE_USERNAME` par votre nom d'utilisateur GitHub.

3. **Pousser le code**
   ```bash
   git push -u origin main
   ```

4. **Authentification**
   - Si demandé, utilisez votre **Personal Access Token** GitHub
   - Pour créer un token : https://github.com/settings/tokens
   - Scopes nécessaires : `repo`

## 🔐 Authentification GitHub

### Méthode 1 : Personal Access Token (recommandé)
1. Allez sur https://github.com/settings/tokens
2. Cliquez sur "Generate new token (classic)"
3. Nommez-le (ex: "DaryPWD")
4. Sélectionnez le scope `repo`
5. Cliquez sur "Generate token"
6. **Copiez le token** (il ne sera affiché qu'une fois)
7. Utilisez-le comme mot de passe lors du push

### Méthode 2 : SSH (optionnel)
Si vous avez configuré SSH :
```bash
git remote set-url origin git@github.com:VOTRE_USERNAME/DaryPWD.git
git push -u origin main
```

## ✅ Vérification

Après le push, vérifiez sur GitHub :
- Tous les fichiers sont présents
- Le README.md s'affiche correctement
- La structure des dossiers est correcte

## 📦 Créer une Release

1. Allez sur votre repository GitHub
2. Cliquez sur "Releases" → "Create a new release"
3. Tag : `v1.0`
4. Titre : `DaryPWD v1.0`
5. Description : Copiez le contenu du README.md
6. Uploadez `bin/Release/DaryPWD.exe` comme fichier binaire
7. Cliquez sur "Publish release"

---

**Besoin d'aide ?** Consultez https://docs.github.com/en/get-started

