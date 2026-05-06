using System.Collections.Generic;

namespace FormulaKit.Runtime.Nodes
{
    /// <summary>
    /// Sequence of statements that execute in order, returns last value
    /// </summary>
    public class SequenceNode : IFormulaNode
    {
        private readonly IFormulaNode[] _statements;

        public SequenceNode(IFormulaNode[] statements)
        {
            this._statements = statements;
        }

        public float Evaluate(Dictionary<string, float> inputs)
        {
            var result = 0f;
            foreach (var statement in _statements)
            {
                result = statement.Evaluate(inputs);
            }
            return result;
        }
    }
}