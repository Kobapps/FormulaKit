# Formula Kit

Formula Kit is a Unity package that provides runtime and editor tooling for defining and executing formulas. The repository contains both the distributable package and a sample Unity project that references it for development.

## Repository layout

- `Packages/com.formulakit.formulakit` — Unity package that can be consumed via Git.
- `FormulaKit/` — Sample Unity project used to develop and validate the package. The project references the package through a relative path in its `Packages/manifest.json` file.

## Installing the package from Git

Add the package to your Unity project by editing `Packages/manifest.json` and including a Git dependency:

```json
{
  "dependencies": {
    "com.formulakit.formulakit": "https://github.com/<your-org>/FormulaKit.git?path=Packages/com.formulakit.formulakit#v1.0.0"
  }
}
```

Replace `<your-org>` with the account hosting this repository and adjust the tag as required. Unity will download the package contents from the specified revision.

## Local development

When working inside this repository, open the `FormulaKit` Unity project. Unity will import the package via the relative file reference defined in `Packages/manifest.json`, allowing you to iterate on the package while running the sample scenes.

## License

See the [LICENSE](LICENSE) file for details.
