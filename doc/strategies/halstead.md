# Halstead Volume Complexity Strategy

## Formula

```
HV = N × log₂(n)
```

où :
- **N** (longueur) = nombre total d'opérateurs + nombre total d'opérandes sur la ligne
- **n** (vocabulaire) = nombre d'opérateurs uniques + nombre d'opérandes uniques sur la ligne

## Choix d'implémentation

### Niveau ligne

Le volume Halstead est calculé **par ligne**. Chaque ligne reçoit son propre décompte d'opérateurs/opérandes et son propre volume. Cela permet de distinguer les lignes simples des lignes complexes au sein d'une même méthode.

### Classification des tokens

#### Opérateurs (« actions »)

| Catégorie | Exemples |
|---|---|
| Mots-clés | `if`, `for`, `while`, `return`, `int`, `var`, `public`, `class`, etc. |
| Arithmétiques | `+`, `-`, `*`, `/`, `%` |
| Affectation | `=`, `+=`, `-=`, `*=`, `/=`, `%=`, etc. |
| Comparaison | `==`, `!=`, `<`, `>`, `<=`, `>=` |
| Logiques | `&&`, `||`, `!` |
| Bitwise | `&`, `|`, `^`, `~`, `<<`, `>>` |
| Ponctuation | `(`, `)`, `{`, `}`, `[`, `]`, `;`, `,`, `.`, `:`, `?` |
| Incrémentation | `++`, `--` |
| Lambda/arrow | `=>` |
| Null coalescing | `??`, `??=` |

#### Opérandes (« données »)

| Catégorie | Exemples |
|---|---|
| Identifiants | noms de variables, méthodes, types |
| Littéraux numériques | `1`, `3.14`, `0xFF` |
| Littéraux chaînes | `"hello"`, `'c'` |
| Booléens | `true`, `false` |
| Null | `null` |

### Cache par arbre syntaxique

Un `ConcurrentDictionary<SyntaxTree, Dictionary<int, double>>` pré-calcule le volume pour chaque ligne de l'arbre en une seule passe (`BuildLineComplexityMap`). Les appels ultérieurs pour d'autres lignes du même fichier sont des lookups O(1).

### Cas particulier : vocabulaire minimal

Si `n ≤ 1`, le volume est simplement `N` (évite `log₂(0)` ou `log₂(1) = 0`).

## Exemples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {       // ← ligne mesurée
		var a = 1;
	}
}
```

**Ligne `if (x > 0) {`** :
- Opérateurs : `if`, `(`, `)`, `>`, `{` → N_op = 5, uniques = 5
- Opérandes : `x`, `0` → N_opd = 2, uniques = 2
- N = 7, n = 7
- **HV = 7 × log₂(7) ≈ 19.6**

### `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;     // ← ligne mesurée
	} else {
		var b = 2;
	}
}
```

**Ligne `var a = 1;`** :
- Opérateurs : `var`, `=`, `;` → 3
- Opérandes : `a`, `1` → 2
- N = 5, n = 5
- **HV = 5 × log₂(5) ≈ 11.6**

### Conditions multiples

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {   // ← ligne mesurée
		var a = 1;
	}
}
```

**Ligne avec conditions multiples** : vocabulaire riche en opérateurs (`if`, `(`, `)`, `>`, `&&`, `||`, `==`, `{`) et opérandes (`x`, `y`, `z`, `0`, `1`).

Volume significativement plus élevé qu'un simple `if`.

### Opérateur ternaire

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← ligne mesurée
}
```

**Ligne ternaire** :
- Opérateurs : `var`, `=`, `>`, `?`, `:`, `;` → 6
- Opérandes : `result`, `x`, `0`, `1`, `0` → 5 total, 3 uniques
- N = 11, n = 9
- **HV ≈ 11 × log₂(9) ≈ 34.8**

### `switch`

```csharp
public void Example() {
	switch (x) {
		case 1:
			var a = 1;   // ← ligne mesurée
			break;
		...
	}
}
```

Chaque ligne du switch a son propre volume. Les lignes `case 1:` ont des mots-clés (`case`, `:`) et un opérande (`1`).

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

**Ligne `var a = DoSomething();`** :
- Opérateurs : `var`, `=`, `(`, `)`, `;` → 5
- Opérandes : `a`, `DoSomething` → 2
- N = 7, n = 7
- **HV = 7 × log₂(7) ≈ 19.6**

## Quand utiliser Halstead

- Estimation de l'effort de développement et maintenance
- Évaluation de la qualité/densité du code
- Détection de code trop verbeux ou trop dense
- Complément aux métriques de flux de contrôle (McCabe, Nesting) qui ne voient pas la complexité « informationnelle » d'une ligne

## Limitations

- Différentes implémentations peuvent classer les tokens différemment
- Ne prend pas en compte les patterns de flux de contrôle
- Le formatage et les espaces peuvent affecter les résultats (indirectement via le découpage en lignes)
- Peut ne pas corréler aussi bien avec les taux de défauts que la complexité cyclomatique
