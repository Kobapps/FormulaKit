using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Random value node - random()
    /// Returns random float between 0.0 and 1.0
    /// </summary>
    public class RandomValueNode : IFormulaNode
    {
        private readonly IRandomProvider _randomProvider;

        public RandomValueNode(IRandomProvider randomProvider)
        {
            _randomProvider = randomProvider;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _randomProvider.Value();
        }
    }
}