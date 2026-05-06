# Formula Kit

Formula Kit is a Unity package for defining, validating, and evaluating arbitrary numeric formulas at runtime. Designers and engineers author readable expressions as plain strings sourced from any system — `ScriptableObject`s, `TextAsset`s, remote configs — and the runtime parses, caches, and evaluates them with full control over inputs and randomness. The package ships with a UI Toolkit editor toolchain: a builder window for authoring and live-testing formulas and a reference window documenting every supported function, keyword, and operator.

- **Runtime evaluator** — parse formulas once, evaluate many times. Pure C#, no Unity-specific dependencies in the core nodes.
- **Formula language** — arithmetic, comparisons, boolean logic, ternary, `let` locals, `if`/`else`, `return`, and a built-in math/random function library.
- **Formula Builder window** — UI Toolkit editor with syntax highlighting, snapshot-based undo, live input auto-detection, inline parse-error markers, and one-click evaluation.
- **Formula Reference window** — searchable, categorized reference for every supported expression with inline runnable examples.

<img width="705" height="758" alt="image" src="https://github.com/user-attachments/assets/4a557065-d11d-4558-a79a-b31b60e52d5a" />

## Table of Contents

- [Installation](#installation)
- [API Overview](#api-overview)
  - [Inline Evaluation](#inline-evaluation)
  - [Static API Examples](#static-api-examples)
  - [Fluent Builder](#fluent-builder)
- [Advanced Usage](#advanced-usage)
  - [Using FormulaLoader and FormulaRunner](#using-formulaloader-and-formularunner)
  - [Caching Strategies](#caching-strategies)
  - [Tokenizer and Function Catalog](#tokenizer-and-function-catalog)
- [Supported Operations](#supported-operations)
  - [Expression Syntax](#expression-syntax)
  - [Built-in Functions](#built-in-functions)
  - [Random Helpers](#random-helpers)
- [Editor Tooling](#editor-tooling)
- [Repository Layout](#repository-layout)
- [Versioning](#versioning)
- [License](#license)

## Installation

Formula Kit ships as a Unity package installable from a Git URL. Unity **2022.3 LTS** or newer is required (the editor windows are built on UI Toolkit).

### Add via Unity Package Manager

1. Open your Unity project.
2. **Window → Package Manager**.
3. Click **+** in the top-left corner.
4. Choose **Add package from git URL...**.
5. Paste:

   ```
   https://github.com/Kobapps/FormulaKit.git#v1.2.2
   ```

6. Press **Add**. Unity downloads the package and registers it.

### Install by Editing `manifest.json`

Add an entry under `dependencies` in `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.kobapps.formulakit": "https://github.com/Kobapps/FormulaKit.git#v1.2.2"
  }
}
```

Drop the `#v1.2.2` suffix to track the latest commit on `main`. Unity will fetch the package on the next refresh.

## API Overview

Formula Kit ships with a static `FormulaAPI` that offers the quickest way to evaluate expressions. Expressions can be evaluated inline or via a fluent builder that caches parsed formulas for reuse.

### Inline Evaluation

```csharp
using FormulaKit.Runtime;
using System.Collections.Generic;

var inputs = new Dictionary<string, float>
{
    ["baseDamage"] = 10f,
    ["strength"] = 5f
};

float total = FormulaAPI.Run("baseDamage * (1 + strength * 0.1)", inputs);
```

- Formulas return `float` values.
- Input variables that are not supplied default to `0`.
- A deterministic cache identifier is generated automatically so repeated calls reuse the parsed formula.
- Expressions are plain strings, so you can source them from any Unity asset, configuration system, or even download them at runtime.

### Static API Examples

`FormulaAPI.Run(expression, inputs)` covers a wide range of gameplay scenarios. Each snippet below supplies the inputs that matter for one specific expression:

```csharp
// Arithmetic scaling
var damageInputs = new Dictionary<string, float>
{
    ["baseDamage"] = 18f,
    ["strength"] = 12f
};
float scaledDamage = FormulaAPI.Run("baseDamage * (1 + strength * 0.05)", damageInputs);
```

```csharp
// Built-in helpers and clamping
var energyInputs = new Dictionary<string, float>
{
    ["currentEnergy"] = 45f,
    ["regen"] = 10f,
    ["deltaTime"] = 0.5f,
    ["maxEnergy"] = 100f
};
float energyTick = FormulaAPI.Run("clamp(currentEnergy + regen * deltaTime, 0, maxEnergy)", energyInputs);
```

```csharp
// Branching with the ternary operator and random rolls
var critInputs = new Dictionary<string, float>
{
    ["critChance"] = 0.25f,
    ["critMultiplier"] = 2f
};
float critRoll = FormulaAPI.Run("randf(1) < critChance ? critMultiplier : 1", critInputs);
```

```csharp
// Function composition for vector math
var travelInputs = new Dictionary<string, float>
{
    ["dx"] = 3f,
    ["dy"] = 4f,
    ["distanceWeight"] = 0.6f
};
float travelCost = FormulaAPI.Run("sqrt(dx * dx + dy * dy) * distanceWeight", travelInputs);
```

```csharp
// Scoped variables with `let`, then early return on a guard condition
var supportInputs = new Dictionary<string, float>
{
    ["baseDamage"] = 18f,
    ["spirit"] = 30f,
    ["targetSpirit"] = 24f,
    ["isSilenced"] = 0f
};
float supportHeal = FormulaAPI.Run(
    @"if (isSilenced) { return 0 }
      let bonus = max(0, (spirit - targetSpirit) * 0.25);
      baseDamage + bonus",
    supportInputs);
```

### Fluent Builder

```csharp
using FormulaKit.Runtime;

float critical = FormulaAPI
    .Run("(baseDamage + bonus) * crit")
    .Set("baseDamage", 12f)
    .Set("bonus", 3f)
    .Set("crit", 1.5f)
    .Evaluate();
```

Populate inputs incrementally. Call `WithCache("myKey")` instead of `Evaluate()` to provide a custom cache identifier when you want to share the parsed expression across systems.

## Advanced Usage

### Using FormulaLoader and FormulaRunner

For projects that manage many expressions, instantiate `FormulaLoader` and `FormulaRunner` directly. Register formulas in code or feed string expressions from whichever content pipeline you use, then evaluate them by ID.

```csharp
using FormulaKit.Runtime;
using System.Collections.Generic;

var loader = new FormulaLoader();
loader.RegisterFormula("damage", "baseDamage * (1 + strength * 0.1)");
loader.RegisterFormula("heal",   "baseHeal + spirit * 0.5");
loader.RegisterFormula("bossPhase", bossPhaseFormulaTextAsset.text);

var runner = new FormulaRunner(loader);
var damage = runner.Evaluate("damage", new Dictionary<string, float>
{
    ["baseDamage"] = 10f,
    ["strength"] = 5f
});
```

Mix and match formulas registered in code with formulas sourced from external content (TextAssets, ScriptableObjects, remote configs). Each formula is referenced by the string ID used during registration.

### Caching Strategies

- `FormulaAPI.Run(expression).WithCache("id")` stores the parsed expression under a custom key for cross-system reuse.
- `FormulaRunner.PrepareFormula("damage")` pools the input dictionary for hot paths before entering a tight loop.
- `FormulaRunner.UseInputPooling = false` allocates fresh dictionaries per evaluation if you prefer that semantics.
- `FormulaAPI.ClearCache()` removes all cached expressions and pooled inputs.

### Tokenizer and Function Catalog

For editor tooling, code completion, syntax-highlight, or static analysis you can reuse the same lexer the editor windows use:

```csharp
using FormulaKit.Runtime;

foreach (var token in FormulaTokenizer.Tokenize("clamp(value, 0, 1)"))
{
    // token.Kind is Identifier / Keyword / FunctionName / Number / Operator / Punctuation / ...
}

// Enumerate every built-in function with its signature and human-readable summary.
foreach (var info in FormulaFunctions.All.Values)
{
    Debug.Log($"{info.Signature}  — {info.Summary}");
}
```

`FormulaParser.TryParse(expression)` returns a structured `FormulaParseResult` (`IsSuccess`, `ErrorMessage`, `ErrorLine`, `ErrorColumn`) without writing to the Unity Console — useful when you want to validate expressions silently in the background or surface errors in your own UI.

## Supported Operations

### Expression Syntax

- Arithmetic: `+`, `-`, `*`, `/`, `%`, exponentiation `^`.
- Unary operations: prefix `+`, prefix `-`, logical negation `!`.
- Comparisons: `<`, `<=`, `>`, `>=`, `==`, `!=`.
- Logical operators: `&&`, `||`.
- Conditional operator: `condition ? whenTrue : whenFalse`.
- Statements: `let` declarations, assignments (`=`, `+=`, `-=`, `*=`, `/=`), `if`/`else`, block scopes `{ ... }`, and `return <expr>` for early exit.

### Built-in Functions

Unary helpers:

`sqrt`, `abs`, `floor`, `ceil`, `round`, `sin`, `cos`, `tan`, `log`, `exp`, `clamp01`, `sign`, `negative`, `acos`, `asin`, `atan`.

Multi-argument helpers:

`min(a, b)`, `max(a, b)`, `clamp(value, min, max)`, `lerp(a, b, t)`, `pow(a, b)`.

### Random Helpers

- `random()` returns a float in `[0, 1)`.
- `rand(max)` returns an integer from `0` to `max - 1`.
- `randf(max)` returns a float in `[0, max)`.

## Editor Tooling

Two editor windows live under **Tools → Formula Framework** in the Unity menu bar:

### Formula Builder

The Formula Builder is a UI Toolkit code editor for authoring and testing formulas without leaving Unity:

- Tokenizer-driven syntax highlighting (keywords, function names, numbers, operators).
- Snapshot-based undo / redo (`Cmd/Ctrl+Z`, `Cmd/Ctrl+Shift+Z`, `Cmd/Ctrl+Y`).
- Inline parse-error markers — right-aligned message + a column caret pointing at the failure position. No Console spam while typing.
- Live input auto-detection: every variable referenced in the expression appears as a `FloatField` row; values you set are preserved when you edit names elsewhere.
- One-click **Evaluate** uses the current inputs to produce a result.
- **Examples** menu with curated formulas for common gameplay systems; **Functions** menu inserts a function snippet at the caret as a single undo step.
- Floating window auto-fits its height to the content (320–1200 px range).

### Formula Reference

A quick lookup for every supported function, keyword, and operator. Categorized list on the left, detailed view on the right with signature, summary, description, and a runnable example whose result is shown live. The reference content is data-driven from `Editor/Tools/UI/FormulaReference.json` — extend it as you extend the language. Includes an **About** page with the installed package version and links.

Both windows respect a shared design-token stylesheet (`KobappsEditorTokens.uss`) so future Kobapps editor tools stay visually consistent.

## Repository Layout

- `package.json` — Unity package manifest.
- `Runtime/` — Pure C# runtime: parser, runner, formula nodes, tokenizer, function catalog, random providers.
- `Editor/` — UI Toolkit editor windows (Formula Builder, Formula Reference) and shared design tokens.
- `Tests/` — Edit-mode tests for the runtime and API.

## Versioning

This package uses semantic versioning. See [CHANGELOG.md](CHANGELOG.md) for the full release history.

## License

Formula Kit is released under the MIT License. See [LICENSE.md](LICENSE.md) for details.
