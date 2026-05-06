using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Constant value node
    /// </summary>
    public class ConstantNode : IFormulaNode
    {
        private readonly float _value;

        public ConstantNode(float value)
        {
            _value = value;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _value;
        }
    }
}