using System.Collections.Generic;
using FormulaFramework;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Random float node - randf(max)
    /// Returns random float between 0 (inclusive) and max (exclusive)
    /// </summary>
    public class RandomFloatNode : IFormulaNode
    {
        private readonly IFormulaNode _maxNode;
        private readonly IRandomProvider _randomProvider;

        public RandomFloatNode(IFormulaNode maxNode, IRandomProvider randomProvider)
        {
            _maxNode = maxNode;
            _randomProvider = randomProvider;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var maxValue = _maxNode.Evaluate(inputs);
            return _randomProvider.NextFloat(maxValue);
        }
    }
}