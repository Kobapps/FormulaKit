using System.Collections.Generic;

namespace FormulaKit.Runtime
{
    /// <summary>
    /// Represents a compiled formula that can be evaluated with different input values
    /// </summary>
    public class Formula
    {
        public string Expression { get; private set; }
        public HashSet<string> RequiredInputs { get; private set; }
        
        private readonly IFormulaNode _rootNode;

        public Formula(string expression, IFormulaNode rootNode, HashSet<string> requiredInputs)
        {
            Expression = expression;
            _rootNode = rootNode;
            RequiredInputs = requiredInputs;
        }

        /// <summary>
        /// Evaluates the formula with the given input values
        /// </summary>
        public float Evaluate(Dictionary<string, float> inputs)
        {
            return _rootNode.Evaluate(inputs);
        }
    }

    /// <summary>
    /// Base interface for formula nodes in the expression tree
    /// </summary>
    public interface IFormulaNode
    {
        float Evaluate(Dictionary<string, float> inputs);
    }

    
   
    

   

    

    // ============== ADVANCED NODES ==============

   

   

  

    

   

   

    
    
    
    

    

   

    
}