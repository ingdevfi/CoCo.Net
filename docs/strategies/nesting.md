# Nesting Complexity Strategy

## Formula

```
Weight = 1 + nesting_depth + logical_operators_count
```

where:
- **1** = base weight (every line has at least this weight)
- **nesting_depth** = number of control flow ancestors (at what depth the line is nested)
- **logical_operators_count** = +1 for each `&&` or `||` in parent conditions

## Implementation Choice

### Line-level

Unlike McCabe (method-level), Nesting is calculated **per line**. Each line receives a weight based on its own depth in the syntax tree.

### Depth Calculation

For each syntax node present on a line, the algorithm climbs up ancestors and adds contributions from each control flow node encountered. The maximum weight among all nodes on the line is retained.

### Contributions by Construction

| Construction | Contribution |
|---|---|
| `if` | +1 + number of `&&`/`||` in condition |
| `while`, `for`, `foreach` | +1 |
| Ternary operator `?:` | +1 |
| `switch` section (`case`/`default`) | +N (total number of switch sections) |

### Untracked Constructions

| Construction | Reason |
|---|---|
| `else` | Covered by parent `if` — same depth |
| `try` / `catch` / `finally` | Not considered control flow nesting |
| `return` | Simple statement, not a control node |

### Advantages over McCabe

- Directly correlates with readability problems
- Penalizes deeply nested code (harder to maintain)
- Simple and intuitive formula
- Detects "pyramids of doom"

### Limitations
- Does not count total paths through function
- `switch` with many `case` can be over-penalized

## Examples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // ← measured line
	}
}
```

**Weight of `var a = 1` line: 2** (1 base + 1 `if` depth)

### `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;   // weight: 2
	} else {
		var b = 2;   // weight: 2
	}
}
```

**Weight: 2** for both branches (same nesting depth)

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

**Weight of `var a = 1`: 3** (1 base + 2 levels of nesting)

### Multiple conditions in an `if`

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {
		var a = 1;   // ← measured line
	}
}
```

**Weight: 4** (1 base + 1 `if` + 1 `&&` + 1 `||`)

### Ternary operator

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← measured line
}
```

**Weight: 2** (1 base + 1 ternary)

### `switch`

```csharp
public void Example() {
	switch (x) {
		case 1:
			var a = 1;   // ← measured line
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

**Weight of `var a = 1`: 4** (1 base + 3 sections in switch)

> Note: Nesting counts total number of switch sections, not just current section. A switch with 5 cases penalizes more than a switch with 2 cases.

### Method with multiple `return`

```csharp
public int Example() {
	if (x > 10) {
		return 1;   // ← measured line
	}
	if (x > 5) {
		return 2;
	}
	return 0;
}
```

**Weight of `return 1`: 2** (1 base + 1 depth from first `if`)

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

**Weight: 1** (1 base — `try`/`catch`/`finally` don't add nesting depth)

## When to Use Nesting

- Focus on code readability and maintainability
- Detection of "pyramids of doom" (deep nesting)
- Complementary to McCabe for identifying code that's hard to read but not necessarily complex in terms of paths
- Structure-oriented code reviews
