using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Random integer node - rand(max)
    /// Returns random integer between 0 (inclusive) and max (exclusive)
    /// </summary>
    public class RandomIntNode : IFormulaNode
    {
        private readonly IFormulaNode _maxNode;
        private readonly IRandomProvider _randomProvider;

        public RandomIntNode(IFormulaNode maxNode, IRandomProvider randomProvider)
        {
            _maxNode = maxNode;
            _randomProvider = randomProvider;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var maxValue = _maxNode.Evaluate(inputs);
            return _randomProvider.Next((int)maxValue);
        }
    }
}