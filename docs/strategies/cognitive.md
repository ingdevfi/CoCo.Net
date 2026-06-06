# Cognitive Complexity Strategy

## Formula

La complexité cognitive (Cognitive Complexity) est une mesure introduite par Sonar:copyright: pour quantifier la **difficulté à comprendre le flux de contrôle** d'une méthode, au-delà du simple comptage des chemins.
Si vous voulez en savoir plus vous pouvez lire cette [documentation](https://www.sonarsource.com/docs/CognitiveComplexity.pdf).
Nous nous en sommes inspiré, mais notre implémentation diffère de la leur. En effet dans notre imlémentation nous comptons aussi les `return` ou les `throw` selon certaines conditions car il peuvent eux aussi casser la lecture linéaire du code.

```
Cognitive Complexity = 1 (base) + Σ(incrément de chaque élément de contrôle) + Σ(incrément d'imbrication)
```

où :
- **1 (base)** = toute méthode commence avec une complexité minimale
- **incrément de chaque élément** = certaines constructions ajoutent directement de la complexité
- **incrément d'imbrication** = chaque niveau d'imbrication multiplie les incréments de complexité par le facteur de nesting

## Choix d'implémentation

### Niveau méthode

Comme McCabe, Cognitive Complexity est calculée **au niveau méthode**. Toutes les lignes d'une méthode partagent la même valeur de complexité, qui représente la charge cognitive globale nécessaire pour comprendre la méthode.

### Éléments qui augmentent la complexité directement (Annex B1)

| Construction | Incrément |
|---|---|
| `if` | +1 |
| `else if` | +1 |
| `switch` (le constructeur lui-même) | +1 |
| `case` / `default` (chaque branche) | +1 |
| `for`, `while`, `do...while` | +1 |
| `foreach` | +1 |
| `break` ou `continue` (si pas le dernier de la boucle) | +1 |
| Opérateurs logiques `&&` et `\|\|` dans un contexte de condition (transition d'opérateur) | +1 |
| `catch` | ✗ **NE compte PAS** |
| `try` | ✗ **NE compte PAS** |
| `return` | si imbriqué |
| `throw` | pas dans un catch et imbriqué  |

### Multiplicateur d'imbrication (Annex B2 & B3)

Chaque construction de contrôle imbriquée à l'intérieur d'une autre augmente le facteur de nesting :

| Profondeur de nesting | Multiplicateur |
|---|---|
| Nesting 0 (toplevel de la méthode) | ×1 |
| Nesting 1 | ×2 |
| Nesting 2 | ×3 |
| Nesting N | ×(N+1) |

Chaque incrément structurel est multiplié par ce facteur. Par exemple, un `if` au nesting 0 ajoute +1, mais le même `if` au nesting 2 ajoute +3.

### Point clé : `catch` et `try` augmentent le nesting mais pas la complexité

- `try` **n'augmente pas** le nesting
- `catch` **augmente le nesting** pour les constructions qui s'y trouvent, mais **n'ajoute pas** de complexité elle-même
- `finally` **n'augmente pas** le nesting

Cela diffère de McCabe, où `try`/`catch`/`finally` sont ignorés complètement.

### Séquences d'opérateurs logiques

Les opérateurs logiques `&&` et `||` sont comptés différemment selon le contexte :

- **Dans une condition (if, while, etc.)** : seules les **transitions** entre opérateurs différents sont comptées
  - `a && b && c` = 0 transition, incrément = 0
  - `a && b || c` = 1 transition (&&→||), incrément = 1
  - `a && b || c && d` = 2 transitions, incrément = 2

- **Dans une assignation ou autre contexte** : une séquence d'opérateurs logiques ajoute +1
  - `var x = a || b || c;` = incrément = 1 (une séquence)
  - `var x = a && b && c;` = incrément = 1 (une séquence)

### Cache par arbre syntaxique

Un `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` met en cache la complexité par méthode. Un second cache (`_methodSpanCache`) pré-calcule les spans de méthodes pour un lookup O(1) par ligne.

### WrappingSyntaxTreeCache

Cognitive utilise le même cache spécial que McCabe pour envelopper le code dans une classe si aucune `MethodDeclarationSyntax` n'est trouvée, permettant de traiter des snippets de test.

## Exemples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;
	}
}
```

**Complexité : 1** (1 base + 1 `if` au nesting 0)

### `if` / `else` imbriqués

```csharp
public void Example() {
	if (x > 0) {
		if (y > 0) {
			var a = 1;
		} else {
			var b = 2;
		}
	} else {
		var c = 3;
	}
}
```

**Complexité : 3**
- 1 `if` externe au nesting 0 = +1
- 1 `if` interne au nesting 1 = +2 (1 × multiplicateur 2)
- **Total : 1 + 2 = 3**

### Conditions multiples dans un `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;
	}
}
```

**Complexité : 2**
- 1 `if` au nesting 0 = +1
- Transition d'opérateur `&&` → `||` = +1 incrément
- **Total : 1 + 1 = 2**

### `try` / `catch` / `finally`

```csharp
public void Example() {
	try {
		var a = DoSomething();
	} catch (Exception ex) {
		Log(ex);
	} finally {
		Cleanup();
	}
}
```

**Complexité : 0**
- `try` n'ajoute rien et n'augmente pas le nesting
- `catch` n'ajoute pas de complexité (augmente juste le nesting pour les instructions à l'intérieur)
- `finally` n'ajoute rien
- **Total : 0**

### Séquence d'opérateurs logiques en assignation

```csharp
public void Example() {
	var isSolution = Path.EndsWith(".sln") || Path.EndsWith(".slnx");
}
```

**Complexité : 1**
- Séquence d'opérateurs `||` dans une assignation = +1 incrément
- **Total : 1**

## Comparaison avec McCabe

| Aspect | McCabe | Cognitive |
|---|---|---|
| Niveau | Méthode | Méthode |
| Base | +1 | +1 |
| Multiplicateur de nesting | Aucun | Oui (N+1) |
| `if` simple | +1 | +1 |
| `if` au nesting 2 | +1 | +3 |
| `try`/`catch`/`finally` | 0 | 0 (mais augmente nesting pour `catch`) |
| Opérateurs logiques | Comptés individuellement | Séquences ou transitions |
| Objectif | Chemins indépendants | Charge cognitive |

## Quand utiliser Cognitive Complexity

- **Revue de code** — corrèle mieux avec la difficulté réelle de lecture et de maintenance
- **Standards modernes** — utilisé par SonarQube, recommandé pour la qualité logicielle
- **Détection de code difficile à comprendre** — pénalise plus fortement l'imbrication que McCabe
- **Corrélation avec les bugs** — études montrent une meilleure corrélation avec les défauts réels que McCabe
- **Refactoring guidé** — pointe vers les refactorisations les plus utiles (dénestification)

## Limitations

- Plus complexe à calculer et à expliquer que McCabe
- Les seuils de complexité acceptables peuvent varier selon les projets
- Nécessite une compréhension du multiplicateur de nesting pour interpréter les résultats

