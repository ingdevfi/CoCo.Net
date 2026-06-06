# McCabe Cyclomatic Complexity Strategy

## Formula

McCabe cyclomatic complexity is formally defined by:

```
M = E - N + 2P
```

where:
- **E** = number of edges in the control flow graph
- **N** = number of nodes in the control flow graph
- **P** = number of connected components (always 1 for a method)

**Equivalent simplified formula**: `CC = 1 + number of decision points`

## Implementation Choice

### Method-level

All lines in a method share the same complexity value. This represents the minimum number of independent paths through the method — it's the classical McCabe definition used by Visual Studio and SonarQube.

### Decision Points Counted

| Construction | Contribution |
|---|---|
| `if` / `else if` | +1 |
| `for`, `while`, `foreach` | +1 |
| `case` in a `switch` (each section) | +1 |
| Ternary operator `?:` | +1 |
| Logical operators `&&`, `||` in condition | +1 each |

### Decision Points NOT Counted

| Construction | Reason |
|---|---|
| `else` | Not a new decision point (covered by `if`) |
| `return` | Simple exit, not a conditional branch |
| `try` / `catch` / `finally` | Exception handling, not classic control flow branching |

### Syntax Tree Caching

A `ConcurrentDictionary<SyntaxTree, Dictionary<MethodDeclarationSyntax, double>>` caches complexity per method to avoid recalculating when processing each line. A second cache (`_methodSpanCache`) pre-calculates method spans for O(1) lookup per line.

### WrappingSyntaxTreeCache

McCabe uses a special cache that wraps code in a class if no `MethodDeclarationSyntax` is found. This allows processing test snippets not in a class.

## Examples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;
	}
}
```

**Weight: 2** (1 base + 1 `if`)

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

**Weight: 2** (1 base + 1 `if` — `else` doesn't add decision point)

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

**Weight: 3** (1 base + 1 external `if` + 1 internal `if`)

### Multiple conditions in an `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;
	}
}
```

**Weight: 4** (1 base + 1 `if` + 1 `&&` + 1 `||`)

### Ternary operator

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;
}
```

**Weight: 2** (1 base + 1 ternary)

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

**Weight: 4** (1 base + 3 `case`/`default` sections)

### Method with multiple `return`

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

**Weight: 3** (1 base + 2 `if` — `return` are not decision points)

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

**Weight: 1** (1 base — `try`/`catch`/`finally` are not decision points in classical McCabe implementation)

## When to Use McCabe

- Traditional complexity analysis
- Compliance with industry standards (ISO 26262, MISRA)
- Estimating minimum number of tests needed to cover all paths
- Comparison with classic thresholds (≤10 = good, 11-20 = moderate, >20 = complex)
