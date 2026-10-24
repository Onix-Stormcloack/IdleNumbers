# Migration depuis IdleNumber (2024)

Il s'agit d'un remplacement des anciens types, pas d'une compatibilité binaire.

| Ancienne approche | Nouvelle |
|---|---|
| `ClassicNumber`, `BigNumber`, `BiggerNumber` | `IdleNumber` unique |
| `OperationService.Add(a,b)` | `a + b` |
| `ReturnTypeHelper.IsNumberSuperior(a,b)` | `a > b` |
| Choix d'un `TypeToString` mutable | `a.ToString(NumberNotation.Letters, 2)` |
| Construction d'une valeur exponentielle | `new IdleNumber(1.25, 1200)` |
| Sauvegarde via la chaîne affichée | `a.ToStorageString()` / `IdleNumber.Parse()` |

Ne pas copier les anciens fichiers `Numbers`, `OperationService`, `Operations`, `Helpers` dans le projet V2, faute de quoi on réintroduit les conversions instables. Les anciennes sauvegardes ne sont pas automatiquement compatibles.

Pour intégrer dans ta solution déjà créée, ajouter simplement `src/IdleNumbers/IdleNumbers.csproj` comme projet existant. Le fichier `.sln` fourni est facultatif.

## Règles de refonte

- Écrire des tests reproduisant les erreurs de l'ancien projet avant d'importer des fonctionnalités.
- Réimplémenter les fonctionnalités métier utiles plutôt que conserver tous les points d'entrée historiques.
- Conserver `NumberExponent` comme détail interne pour pouvoir faire évoluer les tours sans obliger le jeu à manipuler les layers.
