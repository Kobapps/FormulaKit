using System.Collections.Generic;
using FormulaFramework;
using NUnit.Framework;

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
