# Guide de dépannage - Push vers GitHub

## 🔍 Problème identifié

**Aucun remote GitHub configuré** - C'est pour cela que le push ne fonctionne pas.

## ✅ Solution étape par étape

### Étape 1 : Créer le repository sur GitHub

1. Allez sur **https://github.com/new**
2. Remplissez :
   - **Repository name** : `DaryPWD`
   - **Description** : `Application d'extraction de mots de passe Internet Explorer et Microsoft Edge`
   - **Public** ou **Private** (selon votre choix)
   - **NE PAS** cocher "Initialize with README"
   - **NE PAS** ajouter .gitignore ou LICENSE (déjà présents)
3. Cliquez sur **"Create repository"**

### Étape 2 : Ajouter le remote GitHub

**Option A : Utiliser le script automatique**
```batch
scripts\SetupGitHub.bat
```

**Option B : Commandes manuelles**
```bash
git remote add origin https://github.com/VOTRE_USERNAME/DaryPWD.git
```
Remplacez `VOTRE_USERNAME` par votre nom d'utilisateur GitHub.

### Étape 3 : Pousser le code

```bash
git push -u origin main
```

## 🔐 Authentification GitHub

GitHub ne permet plus d'utiliser votre mot de passe. Vous devez utiliser un **Personal Access Token**.

### Créer un Personal Access Token

1. Allez sur **https://github.com/settings/tokens**
2. Cliquez sur **"Generate new token (classic)"**
3. Remplissez :
   - **Note** : `DaryPWD` (ou tout autre nom)
   - **Expiration** : Selon votre choix (90 jours recommandé)
   - **Scopes** : Cochez **`repo`** (accès complet aux repositories)
4. Cliquez sur **"Generate token"**
5. **COPIEZ LE TOKEN** (il ne sera affiché qu'une fois !)
6. Utilisez ce token comme **mot de passe** lors du push

### Utiliser le token

Quand vous exécutez `git push`, Git vous demandera :
- **Username** : Votre nom d'utilisateur GitHub
- **Password** : Collez votre **Personal Access Token** (pas votre mot de passe)

## 🐛 Dépannage

### Erreur : "remote origin already exists"

```bash
git remote remove origin
git remote add origin https://github.com/VOTRE_USERNAME/DaryPWD.git
```

### Erreur : "repository not found"

- Vérifiez que le repository existe sur GitHub
- Vérifiez que l'URL est correcte : `git remote -v`
- Vérifiez que vous avez les droits d'accès

### Erreur : "authentication failed"

- Vérifiez que votre token est correct
- Assurez-vous que le scope `repo` est sélectionné
- Vérifiez que le token n'a pas expiré

### Erreur : "failed to push some refs"

```bash
git pull origin main --allow-unrelated-histories
git push -u origin main
```

## 📝 Commandes utiles

```bash
# Vérifier le remote
git remote -v

# Changer le remote
git remote set-url origin https://github.com/VOTRE_USERNAME/DaryPWD.git

# Vérifier les commits
git log --oneline -5

# Vérifier la branche
git branch
```

## ✅ Vérification après le push

Après un push réussi, vérifiez sur GitHub :
- Tous les fichiers sont présents
- Le README.md s'affiche correctement
- Les commits sont visibles dans l'historique

---

**Besoin d'aide ?** Consultez https://docs.github.com/en/get-started

