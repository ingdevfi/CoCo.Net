# Nesting Complexity Strategy

## Formula

```
Poids = 1 + profondeur_d_imbrication + nombre_operateurs_logiques
```

où :
- **1** = poids de base (toute ligne a au minimum ce poids)
- **profondeur_d_imbrication** = nombre d'ancêtres de flux de contrôle (à quelle profondeur la ligne est imbriquée)
- **nombre_operateurs_logiques** = +1 pour chaque `&&` ou `||` dans les conditions parentes

## Choix d'implémentation

### Niveau ligne

Contrairement à McCabe (méthode-level), le Nesting est calculé **par ligne**. Chaque ligne reçoit un poids basé sur sa propre profondeur dans l'arbre syntaxique.

### Calcul de la profondeur

Pour chaque nœud syntaxique présent sur une ligne, l'algorithme remonte dans les ancêtres et additionne les contributions de chaque nœud de contrôle de flux rencontré. Le poids maximum parmi tous les nœuds de la ligne est retenu.

### Contributions par construction

| Construction | Contribution |
|---|---|
| `if` | +1 + nombre de `&&`/`||` dans la condition |
| `while`, `for`, `foreach` | +1 |
| Opérateur ternaire `?:` | +1 |
| `switch` section (`case`/`default`) | +N (nombre total de sections du `switch`) |

### Constructions non comptées

| Construction | Raison |
|---|---|
| `else` | Couvert par le `if` parent — même profondeur |
| `try` / `catch` / `finally` | Pas considéré comme de l'imbrication de flux de contrôle |
| `return` | Simple instruction, pas un nœud de contrôle |

### Avantages par rapport à McCabe

- Corrèle directement avec les problèmes de lisibilité
- Pénalise le code profondément imbriqué (plus difficile à maintenir)
- Formule simple et intuitive
- Détecte les "pyramides de doom"

### Limitations
- Ne compte pas le nombre total de chemins à travers la fonction
- Les `switch` avec beaucoup de `case` peuvent être sur-pénalisés

## Exemples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // ← ligne mesurée
	}
}
```

**Poids de la ligne `var a = 1` : 2** (1 base + 1 profondeur `if`)

### `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // poids: 2
	} else {
		var b = 2;   // poids: 2
	}
}
```

**Poids : 2** pour les deux branches (même profondeur d'imbrication)

### `if` / `else` imbriqués

```csharp
public void Example() {
	if (x > 0) {
		if (y > 0) {
			var a = 1;   // ← ligne mesurée
		} else {
			var b = 2;
		}
	} else {
		var c = 3;
	}
}
```

**Poids de `var a = 1` : 3** (1 base + 2 niveaux d'imbrication)

### Conditions multiples dans un `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;   // ← ligne mesurée
	}
}
```

**Poids : 4** (1 base + 1 `if` + 1 `&&` + 1 `||`)

### Opérateur ternaire

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← ligne mesurée
}
```

**Poids : 2** (1 base + 1 ternaire)

### `switch`

```csharp
public void Example() {
	switch (x) {
		case 1:
			var a = 1;   // ← ligne mesurée
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

**Poids de `var a = 1` : 4** (1 base + 3 sections dans le switch)

> Note : le Nesting compte le nombre total de sections du switch, pas juste la section courante. Un switch avec 5 cas pénalise plus qu'un switch avec 2 cas.

### Méthode avec plusieurs `return`

```csharp
public int Example() {
	if (x > 10) {
		return 1;   // ← ligne mesurée
	}
	if (x > 5) {
		return 2;
	}
	return 0;
}
```

**Poids de `return 1` : 2** (1 base + 1 profondeur du premier `if`)

### `try` / `catch` / `finally`

```csharp
public void Example() {
	try {
		var a = DoSomething();   // ← ligne mesurée
	} catch (Exception ex) {
		Log(ex);
	} finally {
		Cleanup();
	}
}
```

**Poids : 1** (1 base — `try`/`catch`/`finally` n'ajoutent pas de profondeur d'imbrication)

## Quand utiliser Nesting

- Focus sur la lisibilité et la maintenabilité du code
- Détection des "pyramides de doom" (imbrications profondes)
- Complémentaire à McCabe pour identifier du code difficile à lire mais pas nécessairement complexe en termes de chemins
- Revues de code orientées structure
