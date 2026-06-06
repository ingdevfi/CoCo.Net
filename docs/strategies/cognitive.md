# Cognitive Complexity Strategy

## Formula

Cognitive Complexity (Cognitive Complexity) is a measure introduced by Sonar© to quantify the **difficulty of understanding control flow** in a method, beyond simple path counting.

For more information you can read this [documentation](https://www.sonarsource.com/docs/CognitiveComplexity.pdf).
We drew inspiration from it, but our implementation differs from theirs. Indeed, in our implementation we also count `return` or `throw` under certain conditions because they can also break linear code reading.

```
Cognitive Complexity = 1 (base) + Σ(control element increment) + Σ(nesting increment)
```

where:
- **1 (base)** = every method starts with minimum complexity
- **control element increment** = some constructions add complexity directly
- **nesting increment** = each nesting level multiplies complexity increments by the nesting factor

## Implementation Choice

### Method-level

Like McCabe, Cognitive Complexity is calculated **at method level**. All lines in a method share the same complexity value, which represents overall cognitive load needed to understand the method.

### Elements that increase complexity directly (Annex B1)

| Construction | Increment |
|---|---|
| `if` | +1 |
| `else if` | +1 |
| `switch` (the constructor itself) | +1 |
| `case` / `default` (each branch) | +1 |
| `for`, `while`, `do...while` | +1 |
| `foreach` | +1 |
| `break` or `continue` (if not last in loop) | +1 |
| Logical operators `&&` and `\|\|` in condition context (operator transition) | +1 |
| `catch` | ✗ **DOES NOT count** |
| `try` | ✗ **DOES NOT count** |
| `return` | if nested |
| `throw` | not inside catch and nested  |

### Nesting Multiplier (Annex B2 & B3)

Each control construction nested inside another increases the nesting factor:

| Nesting depth | Multiplier |
|---|---|
| Nesting 0 (method top-level) | ×1 |
| Nesting 1 | ×2 |
| Nesting 2 | ×3 |
| Nesting N | ×(N+1) |

Each structural increment is multiplied by this factor. For example, an `if` at nesting 0 adds +1, but the same `if` at nesting 2 adds +3.

### Key Point: `catch` and `try` increase nesting but not complexity

- `try` **does not increase** nesting
- `catch` **increases nesting** for constructions inside it, but **does not add** complexity itself
- `finally` **does not increase** nesting

This differs from McCabe, where `try`/`catch`/`finally` are ignored completely.

### Logical Operator Sequences

Logical operators `&&` and `||` are counted differently depending on context:

- **In a condition (if, while, etc.)**: only **transitions** between different operators are counted
  - `a && b && c` = 0 transitions, increment = 0
  - `a && b || c` = 1 transition (&&→||), increment = 1
  - `a && b || c && d` = 2 transitions, increment = 2

- **In an assignment or other context**: a sequence of logical operators adds +1
  - `var x = a || b || c;` = increment = 1 (one sequence)
  - `var x = a && b && c;` = increment = 1 (one sequence)

### Syntax Tree Caching

A `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` caches complexity per method. A second cache (`_methodSpanCache`) pre-calculates method spans for O(1) lookup per line.

### WrappingSyntaxTreeCache

Cognitive uses the same special cache as McCabe to wrap code in a class if no `MethodDeclarationSyntax` is found, allowing processing of test snippets.

## Examples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;
	}
}
```

**Complexity: 1** (1 base + 1 `if` at nesting 0)

### Nested `if` / `else`

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

**Complexity: 3**
- 1 external `if` at nesting 0 = +1
- 1 internal `if` at nesting 1 = +2 (1 × multiplier 2)
- **Total: 1 + 2 = 3**

### Multiple conditions in an `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;
	}
}
```

**Complexity: 2**
- 1 `if` at nesting 0 = +1
- Operator transition `&&` → `||` = +1 increment
- **Total: 1 + 1 = 2**

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

**Complexity: 0**
- `try` adds nothing and doesn't increase nesting
- `catch` doesn't add complexity (just increases nesting for instructions inside)
- `finally` adds nothing
- **Total: 0**

### Logical operator sequence in assignment

```csharp
public void Example() {
	var isSolution = Path.EndsWith(".sln") || Path.EndsWith(".slnx");
}
```

**Complexity: 1**
- Logical operator sequence `||` in assignment = +1 increment
- **Total: 1**

## Comparison with McCabe

| Aspect | McCabe | Cognitive |
|---|---|---|
| Level | Method | Method |
| Base | +1 | +1 |
| Nesting multiplier | None | Yes (N+1) |
| Simple `if` | +1 | +1 |
| `if` at nesting 2 | +1 | +3 |
| `try`/`catch`/`finally` | 0 | 0 (but increases nesting for `catch`) |
| Logical operators | Counted individually | Sequences or transitions |
| Goal | Independent paths | Cognitive load |

## When to Use Cognitive Complexity

- **Code review** — correlates better with actual reading and maintenance difficulty
- **Modern standards** — used by SonarQube, recommended for software quality
- **Detecting hard-to-understand code** — penalizes nesting more strongly than McCabe
- **Bug correlation** — studies show better correlation with real defects than McCabe
- **Guided refactoring** — points to most useful refactorings (denesting)

## Limitations

- More complex to calculate and explain than McCabe
- Acceptable complexity thresholds may vary by project
- Requires understanding of nesting multiplier to interpret results
