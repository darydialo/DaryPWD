# 🚀 Guide SIMPLIFIÉ - Push vers GitHub en 3 étapes

## ⚠️ PROBLEME ACTUEL
Vous n'avez pas encore configuré le remote GitHub. C'est normal pour un nouveau projet !

## ✅ SOLUTION EN 3 ÉTAPES SIMPLES

### ÉTAPE 1 : Créer le repository sur GitHub (2 minutes)

1. **Ouvrez votre navigateur** et allez sur : **https://github.com/new**
2. **Remplissez le formulaire** :
   ```
   Repository name: DaryPWD
   Description: Application d'extraction de mots de passe IE et Edge
   [ ] Public (recommandé) ou [ ] Private
   [ ] NE PAS COCHER "Initialize with README"
   [ ] NE PAS COCHER "Add .gitignore"
   [ ] NE PAS COCHER "Choose a license"
   ```
3. **Cliquez sur "Create repository"**
4. **Copiez l'URL** qui s'affiche (exemple: `https://github.com/votre-nom/DaryPWD.git`)

### ÉTAPE 2 : Créer un Personal Access Token (3 minutes)

GitHub ne permet plus les mots de passe. Il faut un token.

1. **Allez sur** : **https://github.com/settings/tokens**
2. **Cliquez sur** : **"Generate new token"** → **"Generate new token (classic)"**
3. **Remplissez** :
   ```
   Note: DaryPWD
   Expiration: 90 days (ou selon votre choix)
   Scopes: ✅ repo (cochez cette case)
   ```
4. **Cliquez sur** : **"Generate token"**
5. **COPIEZ LE TOKEN** (il commence par `ghp_...`)
   - ⚠️ **IMPORTANT** : Vous ne pourrez plus le voir après !
   - Gardez-le dans un endroit sûr temporairement

### ÉTAPE 3 : Configurer et pousser (2 minutes)

**Option A : Script automatique (RECOMMANDÉ)**
```batch
scripts\SetupGitHub.bat
```
Le script vous guidera étape par étape.

**Option B : Commandes manuelles**

1. **Ajoutez le remote** (remplacez `VOTRE_USERNAME` par votre nom GitHub) :
   ```bash
   git remote add origin https://github.com/VOTRE_USERNAME/DaryPWD.git
   ```

2. **Vérifiez que c'est bien configuré** :
   ```bash
   git remote -v
   ```
   Vous devriez voir votre URL deux fois.

3. **Poussez le code** :
   ```bash
   git push -u origin main
   ```

4. **Quand Git demande l'authentification** :
   - **Username** : Votre nom d'utilisateur GitHub
   - **Password** : Collez votre **Personal Access Token** (pas votre mot de passe)

## 🎯 Résultat attendu

Après un push réussi, vous verrez :
```
Enumerating objects: XX, done.
Counting objects: 100% (XX/XX), done.
Writing objects: 100% (XX/XX), done.
To https://github.com/VOTRE_USERNAME/DaryPWD.git
 * [new branch]      main -> main
Branch 'main' set up to track remote branch 'main' from 'origin'.
```

## ❌ Erreurs courantes et solutions

### Erreur : "remote origin already exists"
```bash
git remote remove origin
git remote add origin https://github.com/VOTRE_USERNAME/DaryPWD.git
```

### Erreur : "repository not found"
- Vérifiez que le repository existe sur GitHub
- Vérifiez que l'URL est correcte
- Vérifiez que vous avez les droits d'accès

### Erreur : "authentication failed"
- Utilisez votre **Personal Access Token** (pas votre mot de passe)
- Vérifiez que le token a le scope `repo`
- Vérifiez que le token n'a pas expiré

### Erreur : "failed to push some refs"
Si le repository GitHub a un README initial :
```bash
git pull origin main --allow-unrelated-histories
git push -u origin main
```

## 📝 Commandes de vérification

```bash
# Vérifier le remote
git remote -v

# Vérifier les commits
git log --oneline -5

# Vérifier la branche
git branch

# Voir tous les fichiers
git status
```

## 🔐 Sécurité du token

- Ne partagez JAMAIS votre token
- Ne le commitez JAMAIS dans le code
- Régénérez-le si vous pensez qu'il est compromis
- Vous pouvez le révoquer à tout moment sur GitHub

## ✅ Après le push réussi

1. Allez sur votre repository GitHub
2. Vérifiez que tous les fichiers sont présents
3. Vérifiez que le README.md s'affiche correctement
4. Créez une Release (optionnel) :
   - Releases → Create a new release
   - Tag: `v1.0`
   - Uploadez `bin\Release\DaryPWD.exe`

---

**Besoin d'aide ?** Le script `scripts\SetupGitHub.bat` vous guidera interactivement !

