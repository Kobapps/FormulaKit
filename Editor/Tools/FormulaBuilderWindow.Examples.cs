using System.Collections.Generic;

namespace FormulaKit.Editor.Tools
{
    public partial class FormulaBuilderWindow
    {
        private static readonly FormulaExample[] CompiledExamples =
        {
            new FormulaExample("Simple", "Basic Damage", "damage", "baseDamage * (1 + strength * 0.1)"),
            new FormulaExample("Simple", "Health Regeneration", "healthRegen", "baseRegen + vitality * 0.5"),
            new FormulaExample("Simple", "Experience Required", "expRequired", "100 * pow(level, 1.5)"),
            new FormulaExample(
                "Advanced",
                "Critical Hit",
                "critDamage",
                "let isCrit = random() < critChance;\nisCrit ? baseDamage * 2 : baseDamage"),
            new FormulaExample(
                "Advanced",
                "Damage with Armor",
                "damageWithArmor",
                "let dmg = baseDamage * (1 + strength * 0.1);\nlet reduction = armor / (armor + 100);\ndmg * (1 - reduction)"),
            new FormulaExample(
                "Advanced",
                "Tiered Bonus",
                "tieredBonus",
                "let mult;\nif (score >= 1000) { mult = 3 }\nelse if (score >= 500) { mult = 2 }\nelse { mult = 1 }\nbaseReward * mult"),
            new FormulaExample(
                "Complex",
                "Full Damage System",
                "fullDamage",
                "let weaponDmg = baseDamage * (1 + strength * 0.1);\nlet isCrit = random() < critChance;\n\nif (isCrit) {\n    weaponDmg *= 2\n}\n\nlet reduction = armor / (armor + 100);\nweaponDmg * (1 - reduction)"),
            new FormulaExample(
                "Math",
                "Quadratic Formula +",
                "quadraticSolution",
                "((-1*b)+sqrt((b^2)-(4*a*c)))/(2*a)"),
            new FormulaExample(
                "Math",
                "Quadratic Formula -",
                "quadraticSolutionNeg",
                "((-1*b)-sqrt((b^2)-(4*a*c)))/(2*a)")
        };

        private static IEnumerable<FormulaExample> GetCompiledExamples()
        {
            return CompiledExamples;
        }
    }
}
