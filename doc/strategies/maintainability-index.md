# Maintainability Index (MI) Strategy

## Formula

```
MI = 171 - 5.2 × ln(HALVOL) - 0.23 × CYCLO - 10.2 × ln(SLOC)
```

Normalisation : `MI' = max(0, (MI × 100) / 171)`

Inversion pour le poids : `weight = 100 - MI'`

Résultat : 0–100, où un poids plus élevé = code plus difficile à maintenir.

## Composants

| Métrique | Niveau | Source |
|---|---|---|
| **HALVOL** (Halstead Volume) | Ligne | Volume Halstead de la ligne spécifique |
| **CYCLO** (Complexité cyclomatique) | Méthode | McCabe de la méthode contenant la ligne |
| **SLOC** (Source Lines of Code) | Méthode | Lignes de code source (hors commentaires/blancs) |

## Choix d'implémentation

### Calcul par ligne (hybride)

L'indice de maintenabilité est calculé **par ligne** en combinant :
- Le **McCabe de la méthode** (partagé par toutes les lignes de la méthode) — reflète la complexité structurelle globale
- Le **Halstead de la ligne** (propre à chaque ligne) — reflète la densité informationnelle locale
- Le **SLOC de la méthode** (partagé) — reflète la taille

Cela produit un poids unique par ligne : les lignes avec plus d'opérateurs/opérandes au sein d'une méthode complexe reçoivent un poids plus élevé que les lignes simples de la même méthode.

### Pourquoi ce choix ?

Une approche purement méthode-level (comme dans l'implémentation originale de Visual Studio) donne le même poids à toutes les lignes d'une méthode. En intégrant le Halstead par ligne, on obtient une granularité plus fine qui permet de :
- Identifier les lignes les plus critiques au sein d'une méthode complexe
- Pondérer la couverture de tests de manière plus précise

### Cache McCabe par méthode

Un `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` cache la valeur McCabe par méthode pour éviter de recalculer à chaque ligne.

### WrappingSyntaxTreeCache

Comme McCabe, MI utilise le `WrappingSyntaxTreeCache` pour gérer les snippets de code sans classe englobante.

### Gestion des valeurs limites

- `HALVOL`, `CYCLO`, `SLOC` sont plafonnés à minimum 1 pour éviter `ln(0)`
- Le poids final est borné dans `[0, 100]`

### Interprétation du MI original

| MI normalisé | Interprétation |
|---|---|
| 85–100 | Hautement maintenable (vert) |
| 50–84 | Maintenable avec préoccupations (jaune) |
| < 50 | Difficile à maintenir (rouge) |

Puisque le projet attend **poids élevé = complexité élevée**, le MI est inversé : `weight = 100 - MI'`.

## Exemples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // ← ligne mesurée
	}
}
```

Pour cette ligne :
- **CYCLO** = 2 (méthode avec un `if`)
- **HALVOL** = volume Halstead de `var a = 1;` ≈ 11.6
- **SLOC** = 3

MI = 171 - 5.2 × ln(11.6) - 0.23 × 2 - 10.2 × ln(3) ≈ 147.9
MI' = max(0, 147.9 × 100 / 171) ≈ 86.5
**Weight ≈ 13.5**

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

Pour cette ligne :
- **CYCLO** = 3 (méthode avec deux `if`)
- **HALVOL** ≈ 11.6 (même assignment)
- **SLOC** = 9

MI plus bas → **poids plus élevé** que l'exemple simple.

### Conditions multiples

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {   // ← ligne mesurée
		var a = 1;
	}
}
```

Pour la ligne du `if` :
- **CYCLO** = 4 (1 + if + && + ||)
- **HALVOL** = élevé (beaucoup d'opérateurs et opérandes)
- **SLOC** = 3

La combinaison d'un McCabe élevé ET d'un Halstead élevé sur cette ligne produit un **poids significatif**.

### Opérateur ternaire

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← ligne mesurée
}
```

- **CYCLO** = 2 (ternaire = 1 point de décision)
- **HALVOL** ≈ 34.8 (vocabulaire riche)
- **SLOC** = 1

Le Halstead élevé de cette ligne unique compense le McCabe modéré.

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

- **CYCLO** = 1 (try/catch ne sont pas des points de décision)
- **HALVOL** ≈ 19.6 (appel de méthode + assignment)
- **SLOC** = 7

McCabe bas + Halstead modéré = **poids relativement bas** malgré la structure try/catch.

## Quand utiliser MI

- Évaluation globale de la santé du code
- Prédiction de l'effort de maintenance
- Identification des zones problématiques
- Combinaison des aspects structurels (McCabe) et informationnels (Halstead)
- Comparaison avec les métriques Visual Studio / SonarQube

## Limitations

- Dépend de la précision des calculs Halstead Volume et McCabe
- Le comptage SLOC peut varier selon le style et le formatage
- La formule logarithmique peut produire des anomalies avec de très petites valeurs
- Ne prend pas en compte la qualité de la documentation/lisibilité
