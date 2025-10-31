# Formula Kit

Formula Kit provides runtime components and editor tooling for defining and evaluating formulas inside Unity projects. The package exposes a set of nodes, parsers and helper APIs that can be used to author deterministic or randomised expressions.

## Installation

You can install the package from a Git URL by adding the following entry to your Unity project's `Packages/manifest.json` file:

```json
{
  "dependencies": {
    "com.barnaff.formulakit": "https://github.com/<your-org>/FormulaKit.git?path=Packages/com.barnaff.formulakit#v1.0.0"
  }
}
```

Replace `<your-org>` with the organisation or user name that hosts this repository and adjust the tag to the version you want to consume.

For local development in this repository, the sample project already references the package via a relative file path. Unity will automatically resolve the package when you open the project.

## Contents

- **Runtime** — Core Formula Kit APIs and nodes for parsing, validating and executing expressions.
- **Editor** — The Formula Builder editor window and related utilities to help author formulas visually.
- **Tests** — Optional play-mode tests located in the `Tests~` folder. Unity only imports these when working directly from the repository.

## Requirements

- Unity 6000.1 or newer.

## License

Formula Kit is distributed under the terms of the accompanying [LICENSE](LICENSE.md).
