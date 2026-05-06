# Formula Kit

Formula Kit is a Unity package for defining, validating, and evaluating arbitrary numeric formulas at runtime. It ships with a Unity 6 UI Toolkit editor toolchain — a builder window for authoring and testing formulas, and a reference window documenting every supported function, keyword, and operator.

- **Runtime evaluator** — parse formulas once, evaluate many times. Pure C#, no Unity-specific dependencies in the core nodes.
- **Formula language** — arithmetic, comparisons, boolean logic, ternary, `let` locals, `if`/`else`, `return`, and a built-in math/random function library.
- **Formula Builder window** — UI Toolkit editor with syntax highlighting, snapshot-based undo, live input auto-detection, inline parse-error markers, and one-click evaluation.
- **Formula Reference window** — searchable, categorized reference for every supported expression with inline runnable examples.

## Installing the package from Git

Add the package to your Unity project by editing `Packages/manifest.json` and including a Git dependency:

```json
{
  "dependencies": {
    "com.barnaff.formulakit": "https://github.com/Barnaff/FormulaKit.git#v1.1.0"
  }
}
```

Adjust the tag as required for the release you want. Unity will download the package contents from the specified revision.

## Editor tools

Both windows live under **Tools → Formula Framework** in the Unity menu bar:

- **Formula Builder** — write a formula, watch detected inputs appear automatically, set per-input values, click **Evaluate** for a result. Syntax errors are marked inline.
- **Formula Reference** — left-side category list of every function, keyword, and operator; right-side details with a working, evaluated example. Includes an About page with the package version.

## Runtime API quick start

```csharp
using FormulaKit.Runtime;
using System.Collections.Generic;

var parser = new FormulaParser();
var formula = parser.Parse("baseDamage * (1 + strength * 0.1)");

var inputs = new Dictionary<string, float>
{
    ["baseDamage"] = 25f,
    ["strength"]   = 12f,
};

float damage = formula.Evaluate(inputs); // 55
```

For repeated evaluations against the same formula, use `FormulaLoader` + `FormulaRunner` (cached + pooled inputs). For tokenization or to enumerate the built-in function catalog, see `FormulaTokenizer` and `FormulaFunctions`.

## Repository layout

- `package.json`, `Runtime/`, `Editor/`, `Tests~/` — Unity package contents distributed via Git.
- `Samples~/Formula Builder Demo/` — Importable sample scene and data demonstrating the editor tooling and runtime APIs.
- `DevelopmentProject/` — Minimal Unity project for developing and testing the package locally. Open this folder from Unity Hub if you want to work on Formula Kit directly; it references the package via a local file dependency so it stays isolated from the distributable contents.

## Samples

After installing the package, import the **Formula Builder Demo** from the Package Manager window to explore the editor windows and runtime evaluation workflow inside your project.

## Versioning

This package uses semantic versioning. See [CHANGELOG.md](CHANGELOG.md) for the full release history.

## License

See the [LICENSE](LICENSE) file for details.
