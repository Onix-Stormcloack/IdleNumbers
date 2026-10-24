# Plan d'implémentation

## Phase 1 — livraison actuelle

- [x] Nouveau cœur de calcul immuable et unifié.
- [x] Représentation `NumberExponent` avec futur `TowerScale` préinstallé.
- [x] Mode standard mathématique (0, signes, normalisation, quatre opérations, comparaisons, puissances entières, racines et log10).
- [x] Tests standard sans dépendances externes, CI, benchmark indicatif.
- [x] Notation scientifique et lettres (`K M B T aa ... ZZ`).
- [x] Premiers noms des n-illions ; repli explicite vers le scientifique au-delà.
- [x] Format de sauvegarde invariant pour les nombres standards.
- [ ] Compilation, exécution des tests et benchmarks dans un environnement .NET 8 (non disponibles ici).

## Phase 2 — rendre les tours vraiment utilisables

- [ ] Fixer les invariants canoniques des tours (`coefficient`, signe, `top`, `layer`, `local`).
- [ ] Proposer un constructeur/factory public pour les valeurs astronomiques.
- [ ] Comparaisons et opérateurs sur tours de même référence (y compris l'annulation exacte).
- [ ] Multiplicateurs ordinaires `×1.01 .. ×1e6` sur toutes les couches et très gros diviseurs.
- [ ] Traitement contrôlé des coefficients qui se compensent partiellement.
- [ ] Règles des tours de références différentes et de leurs pertes de précision.
- [ ] Stratégie de débordement de `local` et descentes de couche.
- [ ] Sauvegardes versionnées pour les tours, et trois rendus des tours.
- [ ] Tests de propriétés (commutativité si pertinente, antisymétrie, transitions, aller-retour de sauvegarde).

## Phase 3 — affichage Conway–Wechsler complet

- [ ] Spécification claire de l'échelle (courte anglaise / longue française).
- [ ] Algorithme génératif validé contre une table de références.
- [ ] Définir la limite et le fallback pour des exposants que l'on ne peut plus parcourir.

## Phase 4 — intégration/optimisation

- [ ] Benchmarks Release du runtime cible (Unity, Godot, MonoGame, etc.).
- [ ] Profiling allocations et taille des structs.
- [ ] Mesurer le coût du champ `TowerScale` dormant sur le hot path.
- [ ] Migration explicite des anciennes sauvegardes si nécessaire.
- [ ] Définir quand un ralentissement devient significatif et seulement alors optimiser.
