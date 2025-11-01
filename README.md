# Formula Kit

Formula Kit is a Unity package that provides runtime and editor tooling for defining and executing formulas. The repository contains the distributable package (located at the repository root) and a bundled sample that can be imported through the Unity Package Manager.

## Repository layout

- `package.json`, `Runtime/`, `Editor/`, `Tests~/` — Unity package contents that can be consumed via Git.
- `Samples~/Formula Builder Demo/` — Importable sample scene and data that demonstrate the editor tooling and runtime APIs.

## Installing the package from Git

Add the package to your Unity project by editing `Packages/manifest.json` and including a Git dependency:

```json
{
  "dependencies": {
    "com.barnaff.formulakit": "https://github.com/<your-org>/FormulaKit.git#v1.0.0"
  }
}
```

Replace `<your-org>` with the account hosting this repository and adjust the tag as required. Unity will download the package contents from the specified revision.

## Samples

After installing the package you can import the **Formula Builder Demo** from the Package Manager window to explore the editor window and runtime evaluation workflow inside your project.

## License

See the [LICENSE](LICENSE) file for details.
