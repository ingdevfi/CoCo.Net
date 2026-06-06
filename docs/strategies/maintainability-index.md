# Maintainability Index (MI) Strategy

## Formula

```
MI = 171 - 5.2 × ln(HALVOL) - 0.23 × CYCLO - 10.2 × ln(SLOC)
```

Normalization: `MI' = max(0, (MI × 100) / 171)`

Inversion for weight: `weight = 100 - MI'`

Result: 0–100, where higher weight = harder to maintain.

## Components

| Metric | Level | Source |
|---|---|---|
| **HALVOL** (Halstead Volume) | Line | Halstead volume of specific line |
| **CYCLO** (Cyclomatic Complexity) | Method | McCabe of method containing the line |
| **SLOC** (Source Lines of Code) | Method | Source lines (excluding comments/whitespace) |

## Implementation Choice

### Per-line Calculation (Hybrid)

Maintainability Index is calculated **per line** by combining:
- **McCabe of the method** (shared by all method lines) — reflects overall structural complexity
- **Halstead of the line** (line-specific) — reflects local information density
- **SLOC of the method** (shared) — reflects size

This produces a unique weight per line: lines with more operators/operands within a complex method receive higher weight than simple lines from the same method.

### Why This Choice?

A purely method-level approach (like in Visual Studio's original implementation) gives the same weight to all lines in a method. By integrating per-line Halstead, we achieve finer granularity that allows:
- Identifying most critical lines within a complex method
- More precise test coverage weighting

### McCabe Caching by Method

A `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` caches McCabe value per method to avoid recalculating per line.

### WrappingSyntaxTreeCache

Like McCabe, MI uses `WrappingSyntaxTreeCache` to handle code snippets without enclosing class.

### Boundary Value Handling

- `HALVOL`, `CYCLO`, `SLOC` are floored at minimum 1 to avoid `ln(0)`
- Final weight is bounded in `[0, 100]`

### MI Original Interpretation

| Normalized MI | Interpretation |
|---|---|
| 85–100 | Highly maintainable (green) |
| 50–84 | Maintainable with concerns (yellow) |
| < 50 | Hard to maintain (red) |

Since the project expects **high weight = high complexity**, MI is inverted: `weight = 100 - MI'`.

## Examples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // ← measured line
	}
}
```

For this line:
- **CYCLO** = 2 (method with one `if`)
- **HALVOL** = Halstead volume of `var a = 1;` ≈ 11.6
- **SLOC** = 3

MI = 171 - 5.2 × ln(11.6) - 0.23 × 2 - 10.2 × ln(3) ≈ 147.9
MI' = max(0, 147.9 × 100 / 171) ≈ 86.5
**Weight ≈ 13.5**

### Nested `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		if (y > 0) {
			var a = 1;   // ← measured line
		} else {
			var b = 2;
		}
	} else {
		var c = 3;
	}
}
```

For this line:
- **CYCLO** = 3 (method with two `if`)
- **HALVOL** ≈ 11.6 (same assignment)
- **SLOC** = 9

Lower MI → **higher weight** than simple example.

### Multiple Conditions

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {   // ← measured line
		var a = 1;
	}
}
```

For the `if` line:
- **CYCLO** = 4 (1 + if + && + ||)
- **HALVOL** = high (many operators and operands)
- **SLOC** = 3

Combination of high McCabe AND high Halstead on this line produces **significant weight**.

### Ternary operator

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← measured line
}
```

- **CYCLO** = 2 (ternary = 1 decision point)
- **HALVOL** ≈ 34.8 (rich vocabulary)
- **SLOC** = 1

High Halstead of this single line compensates for moderate McCabe.

### `try` / `catch` / `finally`

```csharp
public void Example() {
	try {
		var a = DoSomething();   // ← measured line
	} catch (Exception ex) {
		Log(ex);
	} finally {
		Cleanup();
	}
}
```

- **CYCLO** = 1 (try/catch are not decision points)
- **HALVOL** ≈ 19.6 (method call + assignment)
- **SLOC** = 7

Low McCabe + moderate Halstead = **relatively low weight** despite try/catch structure.

## When to Use MI

- Overall code health evaluation
- Maintenance effort prediction
- Problem area identification
- Combining structural (McCabe) and informational (Halstead) aspects
- Comparison with Visual Studio / SonarQube metrics

## Limitations

- Depends on Halstead Volume and McCabe calculation accuracy
- SLOC counting may vary by style and formatting
- Logarithmic formula may produce anomalies with very small values
- Does not account for documentation/readability quality
