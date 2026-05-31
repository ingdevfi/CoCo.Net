# McCabe Cyclomatic Complexity Strategy

## Formula

La complexité cyclomatique de McCabe est formellement définie par :

```
M = E - N + 2P
```

où :
- **E** = nombre d'arêtes dans le graphe de flux de contrôle
- **N** = nombre de nœuds dans le graphe de flux de contrôle
- **P** = nombre de composantes connexes (toujours 1 pour une méthode)

**Formule simplifiée équivalente** : `CC = 1 + nombre de points de décision`

## Choix d'implémentation

### Niveau méthode

Toutes les lignes d'une méthode partagent la même valeur de complexité. Cela représente le nombre minimum de chemins indépendants à travers la méthode — c'est la définition classique de McCabe utilisée par Visual Studio et SonarQube.

### Points de décision comptés

| Construction | Contribution |
|---|---|
| `if` / `else if` | +1 |
| `for`, `while`, `foreach` | +1 |
| `case` dans un `switch` (chaque section) | +1 |
| Opérateur ternaire `?:` | +1 |
| Opérateurs logiques `&&`, `||` dans une condition | +1 chacun |

### Points de décision NON comptés

| Construction | Raison |
|---|---|
| `else` | Pas un nouveau chemin de décision (couvert par le `if`) |
| `return` | Simple sortie, pas un branchement conditionnel |
| `try` / `catch` / `finally` | Gestion d'exceptions, pas un branchement de flux de contrôle classique |

### Cache par arbre syntaxique

Un `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` met en cache la complexité par méthode pour éviter de recalculer lors du traitement de chaque ligne. Un second cache (`_methodSpanCache`) pré-calcule les spans de méthodes pour un lookup O(1) par ligne.

### WrappingSyntaxTreeCache

McCabe utilise un cache spécial qui enveloppe le code dans une classe si aucune `MethodDeclarationSyntax` n'est trouvée. Cela permet de traiter des snippets de test qui ne sont pas dans une classe.

## Exemples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;
	}
}
```

**Poids : 2** (1 base + 1 `if`)

### `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;
	} else {
		var b = 2;
	}
}
```

**Poids : 2** (1 base + 1 `if` — le `else` n'ajoute pas de point de décision)

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

**Poids : 3** (1 base + 1 `if` externe + 1 `if` interne)

### Conditions multiples dans un `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;
	}
}
```

**Poids : 4** (1 base + 1 `if` + 1 `&&` + 1 `||`)

### Opérateur ternaire

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;
}
```

**Poids : 2** (1 base + 1 ternaire)

### `switch`

```csharp
public void Example() {
	switch (x) {
		case 1:
			var a = 1;
			break;
		case 2:
			var b = 2;
			break;
		default:
			var c = 3;
			break;
	}
}
```

**Poids : 4** (1 base + 3 sections `case`/`default`)

### Méthode avec plusieurs `return`

```csharp
public int Example() {
	if (x > 10) {
		return 1;
	}
	if (x > 5) {
		return 2;
	}
	return 0;
}
```

**Poids : 3** (1 base + 2 `if` — les `return` ne sont pas des points de décision)

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

**Poids : 1** (1 base — `try`/`catch`/`finally` ne sont pas des points de décision dans l'implémentation McCabe classique)

## Quand utiliser McCabe

- Analyse de complexité traditionnelle
- Conformité aux standards industriels (ISO 26262, MISRA)
- Estimation du nombre minimum de tests nécessaires pour couvrir tous les chemins
- Comparaison avec les seuils classiques (≤10 = bon, 11-20 = modéré, >20 = complexe)
