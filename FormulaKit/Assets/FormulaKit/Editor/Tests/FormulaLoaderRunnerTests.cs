using System.Collections.Generic;
using System.Text.RegularExpressions;
using FormulaKit.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FormulaKit.Editor.Tests
{
    [TestFixture]
    public class FormulaLoaderRunnerTests
    {
        private FormulaLoader _loader;
        private FormulaRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _loader = new FormulaLoader();
            _runner = new FormulaRunner(_loader);
        }

        [Test]
        public void RegisterFormula_ValidExpression_CachesFormula()
        {
            const string formulaId = "sum";
            const string expression = "a + b * 2";

            bool registered = _loader.RegisterFormula(formulaId, expression);

            Assert.That(registered, Is.True, "Registration should succeed for a valid expression");
            Formula formula = _loader.GetFormula(formulaId);
            Assert.That(formula, Is.Not.Null, "Formula should be cached and retrievable");
            Assert.That(formula.Expression, Is.EqualTo(expression));
            Assert.That(formula.RequiredInputs, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void RegisterFormula_InvalidExpression_ReportsErrorAndDoesNotCache()
        {
            string receivedError = null;
            _loader.OnError += message => receivedError = message;

            LogAssert.Expect(LogType.Error, new Regex(@"\[FormulaParser\] Parse error"));
            bool registered = _loader.RegisterFormula("invalid", "a +");

            Assert.That(registered, Is.False, "Registration should fail when the expression is invalid");
            Assert.That(_loader.HasFormula("invalid"), Is.False, "Invalid formulas should not be cached");
            Assert.That(receivedError, Does.Contain("invalid"));
        }

        [Test]
        public void RegisterFormulas_ProcessesValidAndInvalidDefinitions()
        {
            var definitions = new List<FormulaDefinition>
            {
                new FormulaDefinition("valid", "a * 2"),
                new FormulaDefinition("invalid", "if ("),
                new FormulaDefinition("another", "let tmp = a + 1; tmp + b")
            };

            LogAssert.Expect(LogType.Error, new Regex(@"\[FormulaParser\] Parse error"));
            int registeredCount = _loader.RegisterFormulas(definitions);

            Assert.That(registeredCount, Is.EqualTo(2));
            Assert.That(_loader.HasFormula("valid"), Is.True);
            Assert.That(_loader.HasFormula("another"), Is.True);
            Assert.That(_loader.HasFormula("invalid"), Is.False);
            Assert.That(_loader.GetFormulaCount(), Is.EqualTo(2));
        }

        [Test]
        public void RemoveFormula_RemovesCachedFormula()
        {
            _loader.RegisterFormula("sum", "a + b");

            bool removed = _loader.RemoveFormula("sum");

            Assert.That(removed, Is.True);
            Assert.That(_loader.HasFormula("sum"), Is.False);
        }

        [Test]
        public void ClearAll_RemovesEveryFormula()
        {
            _loader.RegisterFormula("sum", "a + b");
            _loader.RegisterFormula("diff", "a - b");

            _loader.ClearAll();

            Assert.That(_loader.GetFormulaCount(), Is.Zero);
            Assert.That(_loader.GetAllFormulaIds(), Is.Empty);
        }

        [Test]
        public void GetRequiredInputs_IgnoresLocalVariables()
        {
            _loader.RegisterFormula("locals", "let temp = a * 2; temp + b");

            HashSet<string> requiredInputs = _loader.GetRequiredInputs("locals");

            Assert.That(requiredInputs, Is.EquivalentTo(new[] { "a", "b" }));
        }

        [Test]
        public void Evaluate_WithDictionaryInputs_ReturnsExpectedResult()
        {
            _loader.RegisterFormula("sum", "a + b * 2");

            var inputs = new Dictionary<string, float>
            {
                { "a", 3f },
                { "b", 4f }
            };

            float result = _runner.Evaluate("sum", inputs);

            Assert.That(result, Is.EqualTo(11f).Within(0.0001f));
        }

        [Test]
        public void Evaluate_WithParamsInputs_ReturnsExpectedResult()
        {
            _loader.RegisterFormula("conditional", "if (a > b) { a } else { b }");

            float result = _runner.Evaluate("conditional", ("a", 5f), ("b", 2f));

            Assert.That(result, Is.EqualTo(5f).Within(0.0001f));
        }

        [Test]
        public void Evaluate_MissingFormula_ReturnsZeroAndReportsError()
        {
            string receivedError = null;
            _runner.OnError += message => receivedError = message;

            float result = _runner.Evaluate("missing", new Dictionary<string, float>());

            Assert.That(result, Is.EqualTo(0f));
            Assert.That(receivedError, Does.Contain("missing"));
        }

        [Test]
        public void Evaluate_WithMissingInput_InvokesErrorHandler()
        {
            _loader.RegisterFormula("sum", "a + b");
            string receivedError = null;
            _runner.OnError += message => receivedError = message;

            float result = _runner.Evaluate("sum", new Dictionary<string, float> { { "a", 2f } });

            Assert.That(result, Is.EqualTo(0f));
            Assert.That(receivedError, Does.Contain("Variable 'b'"));
        }

        [Test]
        public void EvaluateBatch_ReturnsResultsForEachInputSet()
        {
            _loader.RegisterFormula("sum", "a + b");

            var batch = new List<Dictionary<string, float>>
            {
                new Dictionary<string, float> { { "a", 1f }, { "b", 2f } },
                new Dictionary<string, float> { { "a", 3f }, { "b", 4f } }
            };

            float[] results = _runner.EvaluateBatch("sum", batch);

            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results[0], Is.EqualTo(3f).Within(0.0001f));
            Assert.That(results[1], Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public void EvaluateMultiple_ReturnsResultsForEachFormula()
        {
            _loader.RegisterFormula("double", "value * 2");
            _loader.RegisterFormula("triple", "value * 3");

            var inputs = new Dictionary<string, float> { { "value", 4f } };
            Dictionary<string, float> results = _runner.EvaluateMultiple(new[] { "double", "triple" }, inputs);

            Assert.That(results.Keys, Is.EquivalentTo(new[] { "double", "triple" }));
            Assert.That(results["double"], Is.EqualTo(8f).Within(0.0001f));
            Assert.That(results["triple"], Is.EqualTo(12f).Within(0.0001f));
        }

        [Test]
        public void TryEvaluate_ReturnsTrueWhenEvaluationSucceeds()
        {
            _loader.RegisterFormula("sum", "a + b");
            var inputs = new Dictionary<string, float> { { "a", 1f }, { "b", 2f } };

            bool success = _runner.TryEvaluate("sum", inputs, out float result);

            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void TryEvaluate_ReturnsFalseWhenInputsMissing()
        {
            _loader.RegisterFormula("sum", "a + b");
            var inputs = new Dictionary<string, float> { { "a", 1f } };

            bool success = _runner.TryEvaluate("sum", inputs, out float result);

            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0f));
        }

        [Test]
        public void PrepareFormula_CreatesInputPool()
        {
            _loader.RegisterFormula("sum", "a + b");

            _runner.PrepareFormula("sum");

            RunnerStats stats = _runner.GetStats();
            Assert.That(stats.PooledFormulaCount, Is.EqualTo(1));
            Assert.That(stats.IsPoolingEnabled, Is.True);
        }

        [Test]
        public void ClearPools_RemovesPreparedFormulas()
        {
            _loader.RegisterFormula("sum", "a + b");
            _runner.PrepareFormula("sum");

            _runner.ClearPools();

            RunnerStats stats = _runner.GetStats();
            Assert.That(stats.PooledFormulaCount, Is.EqualTo(0));
        }

        [Test]
        public void UseInputPoolingProperty_TogglesPoolingState()
        {
            _runner.UseInputPooling = false;

            RunnerStats stats = _runner.GetStats();
            Assert.That(stats.IsPoolingEnabled, Is.False);

            _runner.UseInputPooling = true;
            stats = _runner.GetStats();
            Assert.That(stats.IsPoolingEnabled, Is.True);
        }
    }
}
