# Formula Kit — Screenshot / Key Image Capture Guide

The Asset Store listing form needs a set of images. They're uploaded separately in the Publisher Portal — none of these go in the package itself.

## Required dimensions

| Image | Dimensions | Notes |
|---|---|---|
| **Key image** (main thumbnail) | 1200 × 630 px | Shown on search results and the listing header. Make this one count. |
| **Icon** | 160 × 160 px (square) | Shown next to the name in lists. Use the existing `FormulaKitIcon.png` upscaled if needed. |
| **Screenshots** | 1920 × 1080 px (16:9) | 5 minimum recommended. Asset Store accepts up to ~9. |

## Suggested screenshot set (capture in this order)

1. **Formula Builder — basic damage formula evaluating cleanly.**
   - Open `Tools → Formula Framework → Formula Builder`.
   - Type `baseDamage * (1 + strength * 0.1)`.
   - Set `baseDamage = 25`, `strength = 12`.
   - Click Evaluate (result reads `55.0000`).
   - Capture the whole window. Title: "Author and evaluate formulas live".

2. **Formula Builder — inline error marker.**
   - Type `let x = baseDamage *` (incomplete — trailing operator).
   - The right-side error message + colored row tint should be visible.
   - Capture. Title: "Live diagnostics — never wonder where the syntax broke".

3. **Formula Builder — auto-detected inputs after picking an Example.**
   - From the Examples menu pick `Advanced / Critical Hit`.
   - All required variables (`critChance`, `baseDamage`, etc.) appear automatically as input rows.
   - Capture. Title: "Inputs detected automatically as you type".

4. **Formula Reference — function detail with evaluated example.**
   - Open `Tools → Formula Framework → Formula Reference`.
   - Pick `Math → lerp` (or similar).
   - Right panel shows signature, summary, description, the example code, and the live evaluated result.
   - Capture. Title: "Built-in reference for every supported expression".

5. **Formula Reference — About page with logo + version + links.**
   - Click the **About** entry at the top of the sidebar.
   - Capture the panel showing the Kobapps logo, "Formula Kit", `v1.2.x`, links.
   - Title: "Single source of truth — all reference content lives in JSON you can extend".

### Optional bonus screenshot (if you want a 6th)

6. **C# usage snippet rendered in your IDE.**
   - Take a screenshot of `FormulaAPI.Run(...)` being called in a small `MonoBehaviour` in your IDE of choice (Rider / VS) with syntax highlighting.
   - Title: "Pure C# runtime — no Unity-specific dependencies in the core nodes".

## Capture tips

- Use Unity's **Game View** docked alongside the editor windows so the screenshots show real Unity context, not just floating windows.
- Set the editor window to a clean theme (Dark theme matches the Kobapps design tokens best).
- Resize windows to match the 16:9 aspect of 1920×1080 before screenshotting; Windows-key+Shift+S or built-in `Edit → Capture Screen` (Unity 6 has this).
- Crop tightly — leave only ~20px of padding around the relevant UI.

## Key image (1200×630) composition idea

Split the canvas vertically:
- **Left half**: the Formula Builder window with a colorized formula visible.
- **Right half**: the Formula Reference detail panel with a function signature.
- **Bottom strip**: title text "Formula Kit — Author readable text formulas, evaluate at runtime" with the Kobapps logo.

Tools: Figma / Photoshop / GIMP / Canva. Export as PNG.
