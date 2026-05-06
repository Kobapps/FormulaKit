# Asset Store submission checklist

Run through this once before clicking Submit. Items in **bold** are blockers.

## Package contents (verified in this branch)

- [x] **`package.json`** — name `com.kobapps.formulakit`, version `1.2.1`, displayName, description, unity `6000.1`, author, license `MIT`, documentationUrl, changelogUrl, licensesUrl.
- [x] **`README.md`** — quick-start, install, API guide, supported operations, editor tooling overview.
- [x] **`CHANGELOG.md`** — semantic-versioned, entries for 1.2.1, 1.2.0, 1.1.1, 1.1.0, 1.0.0.
- [x] **`LICENSE.md`** — MIT.
- [x] **`Editor/`** — UI Toolkit windows + design tokens + textures, all `.meta` present.
- [x] **`Runtime/`** — pure-C# runtime, all `.meta` present.
- [x] **`Tests/`** — edit-mode tests, registered in `testables[]`.
- [x] No `Samples~/`, `Documentation~/`, or `DevelopmentProject/` clutter that the user removed in 1.1.

## Asmdef wiring

- [x] `FormulaKit` (Runtime) — auto-referenced, no platform restrictions.
- [x] `FormulaKit.Editor.Tools` (Editor) — references `FormulaKit`, includes `Editor` only.
- [x] `FormulaKit.Editor.Tests` (Tests) — references `FormulaKit` + TestRunner, includes `Editor` only, defines `UNITY_INCLUDE_TESTS`.
- [x] No duplicate / conflicting asmdef names.

## Manual verification (in PackagesWorkspace, before submitting)

- [ ] Open `PackagesWorkspace` in Unity 6000.1.x. Wait for compilation. **Console clean** — no errors or warnings from FormulaKit.
- [ ] `Tools → Formula Framework → Formula Builder` opens. Type a formula, evaluate. No exceptions.
- [ ] `Tools → Formula Framework → Formula Reference` opens. Pages render. Logo loads. Links work (the About page should show clickable GitHub + Kobapps links).
- [ ] `Window → General → Test Runner → EditMode → Run All`. **All tests pass.**
- [ ] Verify the Formula Builder window's icon shows in the dock tab strip.

## Asset Store Tools workflow

1. **Asset Store Tools menu → Asset Store Uploader.**
2. Sign in with the Kobapps publisher account (or whichever publisher should own the listing).
3. **Create new package** (if first time) or pick the existing draft.
4. **Package source: UPM Package.** Pick `com.kobapps.formulakit` from the dropdown.
5. **Validate** — fix anything the validator flags before uploading.
6. **Upload.**

## Publisher Portal (browser side)

- [ ] Listing title and short description: copied from `listing-copy.md`.
- [ ] Long description: copied from `listing-copy.md` (rich text).
- [ ] Tags: from `listing-copy.md`.
- [ ] Category: Tools → Utilities.
- [ ] **Key image (1200×630)**: per `screenshot-guide.md` composition.
- [ ] **Icon (160×160)**: derived from `Editor/Textures/FormulaKitIcon.png`.
- [ ] **5+ screenshots (1920×1080)**: per `screenshot-guide.md` set.
- [ ] Documentation URL, changelog URL, source URL — all to `Kobapps/FormulaKit`.
- [ ] Pricing.
- [ ] Submit for review.

## Post-submission

- [ ] Tag the released commit: `git tag -a v1.2.1` already exists; if you ship a different version, tag matching.
- [ ] Watch Publisher email for the Asset Store reviewer's feedback (typical turnaround 5–10 business days).
- [ ] If the reviewer flags issues, fix them in the package, push to main, retag, and click "Resubmit" in the Publisher Portal.
