using System.Collections.Generic;
using FormulaKit.Runtime;
using NUnit.Framework;

namespace FormulaKit.Editor.Tests
{
    [TestFixture]
    public class FormulaTests
    {

        private FormulaLoader _loader;
        private FormulaRunner _runner;
        
        [SetUp]
        public void Setup()
        {
            _loader = new FormulaLoader();
            _runner = new FormulaRunner(_loader);
        }
        
        [Test]
        public void FormulaRegister()
        {
            string formulaId = "testFormula";
            string expression = "a + b * 2";

            bool registered = _loader.RegisterFormula(formulaId, expression);
            Assert.IsTrue(registered, "Formula should register successfully");

            Formula formula = _loader.GetFormula(formulaId);
            Assert.IsNotNull(formula, "Registered formula should be retrievable");
            Assert.AreEqual(expression, formula.Expression, "Formula expression should match");
        }
        
        [Test]
        public void FormulaEvaluate()
        {
            string formulaId = "testFormula";
            string expression = "a + b * 2";

            _loader.RegisterFormula(formulaId, expression);

            var inputs = new Dictionary<string, float>
            {
                { "a", 3f },
                { "b", 4f }
            };

            float result = _runner.Evaluate(formulaId, inputs);
            Assert.AreEqual(11f, result, "Formula evaluation should produce correct result");
        }
        
        
    }
}