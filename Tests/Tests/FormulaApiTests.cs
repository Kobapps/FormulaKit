using System.Collections.Generic;
using FormulaKit.Runtime;
using NUnit.Framework;
using UnityEngine;

namespace FormulaKit.Editor.Tests
{
    [TestFixture]
    public class FormulaApiTests
    {
        [SetUp]
        public void SetUp()
        {
            FormulaAPI.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            FormulaAPI.ClearCache();
        }

        [Test]
        public void Run_WithDictionaryInputs_ReturnsExpectedResultAndCachesFormula()
        {
            var inputs = new Dictionary<string, float>
            {
                { "a", 2f },
                { "b", 3f }
            };

            float result = FormulaAPI.Run("a + b * 2", inputs);

            Assert.That(result, Is.EqualTo(8f).Within(0.0001f));

            IReadOnlyDictionary<string, string> cached = FormulaAPI.GetAllFormulas();
            Assert.That(cached.Count, Is.EqualTo(1));
            Assert.That(cached.Values, Does.Contain("a + b * 2"));
        }

        [Test]
        public void RunBuilder_SetInputs_EvaluatesExpression()
        {
            float result = FormulaAPI.Run("let temp = x * 2; temp + y")
                .Set("x", 2f)
                .Set("y", 3f)
                .Evaluate();

            Assert.That(result, Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public void RunBuilder_WithInputs_CopiesValuesBeforeEvaluation()
        {
            var providedInputs = new Dictionary<string, float>
            {
                { "value", 4f }
            };

            var request = FormulaAPI.Run("value * 2")
                .WithInputs(providedInputs);

            providedInputs["value"] = 10f;

            float result = request.Evaluate();

            Assert.That(result, Is.EqualTo(8f).Within(0.0001f));
        }

        [Test]
        public void RunBuilder_WithCache_RegistersFormulaUnderProvidedIdentifier()
        {
            float first = FormulaAPI.Run("a + b")
                .Set("a", 1f)
                .Set("b", 2f)
                .WithCache("customId");

            Assert.That(first, Is.EqualTo(3f).Within(0.0001f));

            float second = FormulaAPI.Run("a * b")
                .Set("a", 3f)
                .Set("b", 4f)
                .WithCache("customId");

            Assert.That(second, Is.EqualTo(12f).Within(0.0001f));

            IReadOnlyDictionary<string, string> cached = FormulaAPI.GetAllFormulas();
            Assert.That(cached.Count, Is.EqualTo(1));
            Assert.That(cached.ContainsKey("customId"), Is.True);
            Assert.That(cached["customId"], Is.EqualTo("a * b"));
        }

        [Test]
        public void Run_WithComplexFormula_ComputesAccurateValue()
        {
            const string expression = @"
let basePower = pow(strength + weaponBonus * multiplier, 2);
let trig = sin(angle) + cos(angle);
let clamped = clamp(basePower * trig, minLimit, maxLimit);
let adjusted = clamped > threshold ? clamped : threshold - abs(clamped - threshold);
let normalized = (adjusted + lerp(minLimit, maxLimit, 0.25)) / max(1, round(magnitude));
normalized";

            var inputs = new Dictionary<string, float>
            {
                { "strength", 4f },
                { "weaponBonus", 1.5f },
                { "multiplier", 2.25f },
                { "angle", 0.35f },
                { "minLimit", 10f },
                { "maxLimit", 50f },
                { "threshold", 30f },
                { "magnitude", 3.6f }
            };

            float result = FormulaAPI.Run(expression, inputs);

            float basePower = Mathf.Pow(inputs["strength"] + inputs["weaponBonus"] * inputs["multiplier"], 2f);
            float trig = Mathf.Sin(inputs["angle"]) + Mathf.Cos(inputs["angle"]);
            float clamped = Mathf.Clamp(basePower * trig, inputs["minLimit"], inputs["maxLimit"]);
            float adjusted = clamped > inputs["threshold"]
                ? clamped
                : inputs["threshold"] - Mathf.Abs(clamped - inputs["threshold"]);
            float lerpValue = Mathf.Lerp(inputs["minLimit"], inputs["maxLimit"], 0.25f);
            float denominator = Mathf.Max(1f, Mathf.Round(inputs["magnitude"]));
            float expected = (adjusted + lerpValue) / denominator;

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void RunBuilder_WithConditionalSequence_ComputesExpectedOutcome()
        {
            const string expression = @"
let normalizedChance = clamp01(criticalChance);
let baseValue = (primaryDamage + secondaryDamage * 0.5) / (normalizedChance + 0.1);
let penalty = 0;
if (resistance > 0.5) { penalty = resistance * 2; } else { penalty = resistance * 0.75; }
let total = baseValue;
total += min(penalty, 5);
total -= sign(total - target) * 1.25;
if (total > target) { total - (total - target) * 0.3 } else { total + (target - total) * 0.6 }";

            float result = FormulaAPI.Run(expression)
                .Set("primaryDamage", 18f)
                .Set("secondaryDamage", 6f)
                .Set("criticalChance", 0.65f)
                .Set("resistance", 0.7f)
                .Set("target", 12f)
                .Evaluate();

            float normalizedChance = Mathf.Clamp01(0.65f);
            float baseValue = (18f + 6f * 0.5f) / (normalizedChance + 0.1f);
            float penalty = 0.7f > 0.5f ? 0.7f * 2f : 0.7f * 0.75f;
            float total = baseValue;
            total += Mathf.Min(penalty, 5f);
            total -= Mathf.Sign(total - 12f) * 1.25f;
            float expected = total > 12f
                ? total - (total - 12f) * 0.3f
                : total + (12f - total) * 0.6f;

            Assert.That(result, Is.EqualTo(expected).Within(0.0001f));
        }

        [Test]
        public void ClearCache_RemovesAllCachedFormulas()
        {
            FormulaAPI.Run("a + b", new Dictionary<string, float>
            {
                { "a", 2f },
                { "b", 3f }
            });

            Assert.That(FormulaAPI.GetAllFormulas().Count, Is.EqualTo(1));

            FormulaAPI.ClearCache();

            Assert.That(FormulaAPI.GetAllFormulas(), Is.Empty);
        }
    }
}
