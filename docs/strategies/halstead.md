# Halstead Volume Complexity Strategy

## Formula

```
HV = N × log₂(n)
```

where:
- **N** (length) = total number of operators + total number of operands on the line
- **n** (vocabulary) = number of unique operators + number of unique operands on the line

## Implementation Choice

### Line-level

Halstead volume is calculated **per line**. Each line receives its own operator/operand count and volume. This allows distinguishing simple lines from complex ones within the same method.

### Token Classification

#### Operators (« actions »)

| Category | Examples |
|---|---|
| Keywords | `if`, `for`, `while`, `return`, `int`, `var`, `public`, `class`, etc. |
| Arithmetic | `+`, `-`, `*`, `/`, `%` |
| Assignment | `=`, `+=`, `-=`, `*=`, `/=`, `%=`, etc. |
| Comparison | `==`, `!=`, `<`, `>`, `<=`, `>=` |
| Logical | `&&`, `||`, `!` |
| Bitwise | `&`, `|`, `^`, `~`, `<<`, `>>` |
| Punctuation | `(`, `)`, `{`, `}`, `[`, `]`, `;`, `,`, `.`, `:`, `?` |
| Incrementation | `++`, `--` |
| Lambda/arrow | `=>` |
| Null coalescing | `??`, `??=` |

#### Operands (« data »)

| Category | Examples |
|---|---|
| Identifiers | variable names, method names, types |
| Numeric literals | `1`, `3.14`, `0xFF` |
| String literals | `"hello"`, `'c'` |
| Booleans | `true`, `false` |
| Null | `null` |

### Syntax Tree Caching

A `ConcurrentDictionary<SyntaxTree, Dictionary<int, double>>` pre-calculates volume for each line of the tree in a single pass (`BuildLineComplexityMap`). Subsequent calls for other lines from the same file are O(1) lookups.

### Special Case: Minimal Vocabulary

If `n ≤ 1`, volume is simply `N` (avoids `log₂(0)` or `log₂(1) = 0`).

## Examples

### Simple `if`

```csharp
public void Example() {
	if (x > 0) {       // ← measured line
		var a = 1;
	}
}
```

**Line `if (x > 0) {`:**
- Operators: `if`, `(`, `)`, `>`, `{` → N_op = 5, unique = 5
- Operands: `x`, `0` → N_opd = 2, unique = 2
- N = 7, n = 7
- **HV = 7 × log₂(7) ≈ 19.6**

### `if` / `else`

```csharp
public void Example() {
	if (x > 0) {
		var a = 1;     // ← measured line
	} else {
		var b = 2;
	}
}
```

**Line `var a = 1;`:**
- Operators: `var`, `=`, `;` → 3
- Operands: `a`, `1` → 2
- N = 5, n = 5
- **HV = 5 × log₂(5) ≈ 11.6**

### Multiple Conditions

```csharp
public void Example() {
	if (x > 0 && y > 0 || z == 1) {   // ← measured line
		var a = 1;
	}
}
```

**Line with multiple conditions:** rich vocabulary of operators (`if`, `(`, `)`, `>`, `&&`, `||`, `==`, `{`) and operands (`x`, `y`, `z`, `0`, `1`).

Significantly higher volume than simple `if`.

### Ternary operator

```csharp
public void Example() {
	var result = x > 0 ? 1 : 0;   // ← measured line
}
```

**Ternary line:**
- Operators: `var`, `=`, `>`, `?`, `:`, `;` → 6
- Operands: `result`, `x`, `0`, `1`, `0` → 5 total, 3 unique
- N = 11, n = 9
- **HV ≈ 11 × log₂(9) ≈ 34.8**

### `switch`

```csharp
public void Example() {
	switch (x) {
		case 1:
			var a = 1;   // ← measured line
			break;
		...
	}
}
```

Each switch line has its own volume. Lines like `case 1:` have keywords (`case`, `:`) and one operand (`1`).

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

**Line `var a = DoSomething();`:**
- Operators: `var`, `=`, `(`, `)`, `;` → 5
- Operands: `a`, `DoSomething` → 2
- N = 7, n = 7
- **HV = 7 × log₂(7) ≈ 19.6**

## When to Use Halstead

- Development and maintenance effort estimation
- Code quality/density evaluation
- Detection of overly verbose or dense code
- Complement to control flow metrics (McCabe, Nesting) that don't see "informational" complexity of a line

## Limitations

- Different implementations may classify tokens differently
- Does not account for control flow patterns
- Formatting and spacing can affect results (indirectly via line splitting)
- May not correlate as well with defect rates as cyclomatic complexity
