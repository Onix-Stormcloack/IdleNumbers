# IdleNumbers V2 — phase 1 (scientifique actif, tours déjà modélisées)

Bibliothèque C# / .NET 8 pour le **cœur numérique d'un Idle Game**, livrée comme **nouveau projet source**. Elle remplace l'architecture `ClassicNumber` / `BigNumber` / `BiggerNumber` de l'archive historique. **Ce n'est pas une mise à jour binaire compatible**.

## Ce qui est implémenté maintenant

- `IdleNumber` immuable (`readonly struct`) : `mantisse double × 10^exposant long`.
- Normalisation, signes, zéro canonique, `+ - * /`, comparaisons, `Min`, `Max`, `Clamp`, `Pow(long)`, `Sqrt`, `Log10`.
- Les opérations courantes ne créent pas d'objets alloués sur le tas pour représenter les nombres.
- Précision interne du `double` (environ 15 à 16 chiffres significatifs), **2 décimales affichées par défaut**, ou 3 si demandé.
- Affichages indépendants : `Scientific`, `Letters` (`K M B T aa ab ... ZZ`), premiers noms de `ConwayWechsler` (échelle courte anglaise jusqu'à vigintillion). **Le générateur Conway–Wechsler général n'est pas encore implémenté** : au-delà, retour explicite à la notation scientifique.
- Sérialisation indépendante de la langue (`ToStorageString`, `Parse`, `TryParse`).
- Un exécutable de tests autonome sans paquets NuGet et une CI GitHub Actions.

## Tours présentes dans l'architecture, MAIS non actives

Dès cette phase, `IdleNumber` contient une structure interne `NumberExponent`. Celle-ci sait stocker conceptuellement :

```
N = m × 10^E
E = a × T_layer(top) + local
T_0(h) = h
T_1(h) = 10^h
T_2(h) = 10^(10^h)
```

`TowerScale` (`src/IdleNumbers/Internal/TowerScale.cs`) valide le layer, le sommet et le coefficient, sans jamais calculer explicitement la tour. `NumberExponent` contient la partie `long Local` et l'éventuel `TowerScale`.

**La phase 1 ne permet pas de créer publiquement de nombre astronomique.** Aucun calcul, comparaison numérique, affichage ou chargement de tours n'est activé ; il serait dangereux de prétendre que ces fonctions sont fiables sans leurs règles de normalisation et d'annulation. Le contrat externe de `IdleNumber` est déjà prêt pour cette évolution. La présence des champs ne veut pas dire que les tours sont « mathématiquement implémentées ».

Le mode standard atteint déjà `1e10000000000` = 10^(10^10) grâce au `long`.

## Intégration à TA solution

Dans Visual Studio : `Ajouter` → `Projet existant` → `src/IdleNumbers/IdleNumbers.csproj`, puis ajouter une **référence de projet** depuis ton projet principal.

Ou en CLI :

```bash
dotnet sln Chemin/Vers/TaSolution.sln add src/IdleNumbers/IdleNumbers.csproj
dotnet sln Chemin/Vers/TaSolution.sln add tests/IdleNumbers.Tests/IdleNumbers.Tests.csproj
dotnet build src/IdleNumbers/IdleNumbers.csproj -c Release
dotnet run --project tests/IdleNumbers.Tests/IdleNumbers.Tests.csproj -c Release
# Optionnel : benchmark indicatif
dotnet run --project tests/IdleNumbers.Tests/IdleNumbers.Tests.csproj -c Release -- --bench
```

Il y a aussi une solution `IdleNumbers.sln` prête à ouvrir. Le runner de tests ne nécessite pas xUnit/NUnit ni connexion NuGet.

## Exemple

```csharp
using IdleNumbers;

IdleNumber money = new(1.25, 10_000_000_000); // 1.25 × 10^(10^10)
money *= new IdleNumber(1.01);                // multiplicateur ordinaire
money /= new IdleNumber(1, 100);              // challenge : division par 10^100

bool canBuy = money >= new IdleNumber(5, 1000);
string display = money.ToString(NumberNotation.Letters, decimalPlaces: 2);
string sci = money.ToString(NumberNotation.Scientific, decimalPlaces: 3);
string save = money.ToStorageString();
IdleNumber loaded = IdleNumber.Parse(save);
```

`1e1000 + 1` reste `1e1000` (la contribution est négligeable à 15 chiffres significatifs). `1e10 - 1e3` est conservé à précision `double` proche de la valeur attendue. Une valeur métier exigeant un nombre **exact** d'unités à très haute échelle doit utiliser un entier adapté, pas `IdleNumber`.

## Contrats importants

1. La mantisse non nulle reste dans `[1,10)` en valeur absolue ; le zéro vaut `(0,0)`.
2. Les entrées `NaN` et `Infinity` sont rejetées. Division par zéro : exception.
3. En mode standard, débordement de l'exposant `long` : `OverflowException` (pas de promotion silencieuse avant la phase 2).
4. Comparaison numérique par signe, exposant, mantisse ; égalité `==` exacte sur la représentation stockée, **pas** une tolérance automatique.
5. `Log10` renvoie un `double`, donc perd la précision à l'unité pour des exposants bien supérieurs à `2^53` ; il ne faut pas l'utiliser pour restituer un exposant entier exact.
6. Le format d'affichage (par exemple `1.25M`) **ne doit jamais servir de format de sauvegarde**.
7. Les noms Conway-Wechsler utilisent l'échelle **courte anglaise** (billion = 10^9), et pas l'échelle longue française.
8. Le formatage utilise `.` et la culture invariante ; l'interface utilisateur pourra appliquer une localisation séparée.
9. L'implémentation et les tests du mode tour restent l'étape suivante. Une tour ne doit jamais être ignorée par les opérateurs.

## Statut de vérification

- Sources et projet générés et archivés.
- Tests et pipeline CI préparés.
- **Compilation et tests non exécutés dans cet environnement** : le SDK .NET n'est pas présent. Ne pas considérer cette version comme validée pour la production avant `dotnet build` et `dotnet run` sur ta machine ou en CI.

À lire ensuite : `docs/ARCHITECTURE.md`, `docs/ROADMAP.md` et `docs/MIGRATION.md`.
