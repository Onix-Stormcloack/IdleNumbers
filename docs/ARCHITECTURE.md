# Architecture — invariants avant calculs

## 1. Deux modes, un seul `IdleNumber`

- Mode standard actif : `N = mantissa × 10^local`.
- Mode astronomique réservé : `N = mantissa × 10^(coefficient × T_layer(top) + local)`.
- `T_0(h)=h`, `T_(k+1)(h)=10^(T_k(h))`, toujours un modèle symbolique.

`IdleNumber` est une structure *valeur* immuable. `NumberExponent` fait partie de la structure, et `TowerScale` est lui-même une structure valeur. Pas de tableau ni `BigInteger` dans le chemin chaud initial.

Ce choix réserve davantage de mémoire par valeur qu'un couple `(double,long)` : on privilégie d'abord une API durable, puis on mesurera la taille réelle (`Unsafe.SizeOf<IdleNumber>()` ou `Marshal.SizeOf` avec précautions) et les performances sur le runtime utilisé par le jeu.

## 2. Invariants phase 1

- `mantissa == 0` implique `local == 0` et aucune tour.
- Sinon : `1 <= abs(mantissa) < 10`, mantisse finie.
- `local` de type `long` (pas de conversion en `double` pour l'alignement des exposants).
- Exposants intermédiaires du produit/quotient combinés en `Int128` pour éviter le faux overflow lors d'une correction `-1` ou `+1` de normalisation.
- Les additions alignent l'exposant le plus faible, et ignorent l'apport dont l'écart est d'au moins 18 puissances décimales.
- Toutes les méthodes de phase 1 utilisant la valeur numérique refusent explicitement le mode tour s'il devenait accessible avant son activation.
- `==` teste l'identité numérique représentée en mode standard normalisé, pas une « égalité presque égale ».

## 3. Portée de la précision

L'écart relatif du `double` est de l'ordre de `1e-16` dans les cas favorables. Les additions avec des valeurs minuscules sont volontairement approchées. Les soustractions de nombres proches peuvent perdre des chiffres significatifs (annulation numérique), comme toute arithmétique flottante.

La partie locale des exposants astronomiques conservera exactement des dizaines de puissances de 10 pendant les multiplications successives, mais elle n'est **pas** une preuve que toutes les divisions entre tours différentes sont exactes.

## 4. Contrats de la phase tours (à spécifier et tester avant activation)

1. **Canonicalisation** : deux écritures du même nombre doivent produire la même représentation lorsque la conversion est possible, ou au minimum être comparées de façon correcte.
2. **Ordre** : gérer signes, coefficient de tour positif/négatif et cancellation.
3. **Coefficient de tour** : si `N = m × 10^(aT+d)`, multiplier `N` par `N` doit donner un coefficient `2a` et un local `2d` tant qu'ils sont représentables.
4. **Diviseurs de même tour** : `(m1 × 10^(aT+d1)) / (m2 × 10^(aT+d2))` devrait annuler `aT`, puis renormaliser `(m1/m2) × 10^(d1-d2)`.
5. **Tours distinctes** : déterminer une règle explicite d'approximation *et* signaler si des composantes sont écartées ; sans cela une division ultérieure pourrait « révéler » une erreur arbitraire.
6. **Dépassements** : `long local` dépasse ±9,22e18 ; il faut choisir exception, représentation étendue, ou approximation contrôlée — jamais reboucler sans fin.
7. **Retour vers standard** : exiger une normalisation de mantisse et une valeur `local` dans la plage `long`.
8. **Sérialisation** : versionner le format (`v2:...`) AVANT d'enregistrer une tour ; ne pas étendre tacitement le parseur des sauvegardes standard.
9. **Affichage** : définir une notation non ambiguë, par exemple `1.25e(1e10)` pour `10^(10^10)` ; conserver toutes les notations sur le chemin standard.

Le cas `10^(T1+T2) / 10^T1` illustre la limite d'une représentation à une tour : si `T2` est écartée, la division ne restitue pas `10^T2`. Des « corrections par couche » ne seront introduites que si l'on dispose d'exemples de gameplay justifiant leur coût.

## 5. Séparation des responsabilités

- `IdleNumber.cs` : API publique, opérations standard, invariants.
- `Internal/NumberExponent.cs` : modèle d'exposant extensible.
- `Internal/TowerScale.cs` : terme symbolique de tour (dormant).
- `Formatting/NumberFormatter.cs` : présentation 2–3 décimales.
- `tests/IdleNumbers.Tests/Program.cs` : tests autonomes sans NuGet, uniquement standard pour la phase 1.

Les améliorations futures ne doivent pas casser l'API du gameplay (`+`, `*`, `ToString`...) ni les sauvegardes de la phase 1.
