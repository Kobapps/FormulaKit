using System;
using System.Collections;
using System.Collections.Generic;
using FormulaKit.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace FormulaFramework
{
    /// <summary>
    /// Unity adapter for the formula system
    /// Provides Unity-specific features while using pure C# core
    /// </summary>
    public class FormulaManager : MonoBehaviour
    {
        private static FormulaManager instance;
        public static FormulaManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("FormulaManager");
                    instance = go.AddComponent<FormulaManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Configuration")]
        [SerializeField] private string remoteFormulaURL = "https://yourserver.com/formulas.json";
        [SerializeField] private bool useLocalFallback = true;
        [SerializeField] private bool verboseLogging = true;

        // Pure C# components
        private FormulaLoader loader;
        private FormulaRunner runner;
        private FormulaJsonLoader jsonLoader;

        // Public accessors for direct access to components
        public FormulaLoader Loader => loader;
        public FormulaRunner Runner => runner;
        public FormulaJsonLoader JsonLoader => jsonLoader;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Initialize pure C# components
            loader = new FormulaLoader();
            runner = new FormulaRunner(loader);
            
            #if UNITY_2019_1_OR_NEWER
            jsonLoader = new FormulaJsonLoader(loader, new UnityJsonParser());
            #else
            jsonLoader = new FormulaJsonLoader(loader);
            #endif

            // Wire up logging if verbose
            if (verboseLogging)
            {
                loader.OnLog += Debug.Log;
                loader.OnError += Debug.LogError;
                runner.OnError += Debug.LogError;
                jsonLoader.OnError += Debug.LogError;
            }
        }

        // ============== FORMULA REGISTRATION ==============

        /// <summary>
        /// Register a formula
        /// </summary>
        public bool RegisterFormula(string id, string expression)
        {
            return loader.RegisterFormula(id, expression);
        }

        /// <summary>
        /// Register multiple formulas
        /// </summary>
        public int RegisterFormulas(IEnumerable<FormulaDefinition> definitions)
        {
            return loader.RegisterFormulas(definitions);
        }

        // ============== FORMULA EVALUATION ==============

        /// <summary>
        /// Evaluate a formula with dictionary inputs
        /// </summary>
        public float Evaluate(string formulaId, Dictionary<string, float> inputs)
        {
            return runner.Evaluate(formulaId, inputs);
        }

        /// <summary>
        /// Evaluate a formula with params inputs
        /// </summary>
        public float Evaluate(string formulaId, params (string key, float value)[] inputs)
        {
            return runner.Evaluate(formulaId, inputs);
        }

        /// <summary>
        /// Try to evaluate a formula
        /// </summary>
        public bool TryEvaluate(string formulaId, Dictionary<string, float> inputs, out float result)
        {
            return runner.TryEvaluate(formulaId, inputs, out result);
        }

        /// <summary>
        /// Batch evaluate
        /// </summary>
        public float[] EvaluateBatch(string formulaId, List<Dictionary<string, float>> batchInputs)
        {
            return runner.EvaluateBatch(formulaId, batchInputs);
        }

        /// <summary>
        /// Evaluate multiple formulas
        /// </summary>
        public Dictionary<string, float> EvaluateMultiple(IEnumerable<string> formulaIds, Dictionary<string, float> inputs)
        {
            return runner.EvaluateMultiple(formulaIds, inputs);
        }

        // ============== FORMULA ACCESS ==============

        /// <summary>
        /// Get a formula object for cached evaluation
        /// </summary>
        public Formula GetFormula(string formulaId)
        {
            return loader.GetFormula(formulaId);
        }

        /// <summary>
        /// Check if formula exists
        /// </summary>
        public bool HasFormula(string formulaId)
        {
            return loader.HasFormula(formulaId);
        }

        /// <summary>
        /// Get required inputs
        /// </summary>
        public HashSet<string> GetRequiredInputs(string formulaId)
        {
            return loader.GetRequiredInputs(formulaId);
        }

        /// <summary>
        /// Get all formula IDs
        /// </summary>
        public IEnumerable<string> GetAllFormulaIds()
        {
            return loader.GetAllFormulaIds();
        }

        /// <summary>
        /// Remove a formula
        /// </summary>
        public bool RemoveFormula(string formulaId)
        {
            return loader.RemoveFormula(formulaId);
        }

        /// <summary>
        /// Clear all formulas
        /// </summary>
        public void ClearAll()
        {
            loader.ClearAll();
            runner.ClearPools();
        }

        // ============== LOADING (Unity-specific) ==============

        /// <summary>
        /// Load formulas from Unity Resources folder
        /// </summary>
        public int LoadFormulasFromResources(string resourcePath)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset != null)
            {
                return jsonLoader.LoadFromJson(textAsset.text);
            }
            else
            {
                Debug.LogError($"Could not find formula resource at {resourcePath}");
                return 0;
            }
        }

        /// <summary>
        /// Load formulas from remote URL (Unity coroutine)
        /// </summary>
        public IEnumerator LoadFormulasFromRemote(Action<bool> onComplete = null)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(remoteFormulaURL))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    int count = jsonLoader.LoadFromJson(request.downloadHandler.text);
                    bool success = count > 0;
                    onComplete?.Invoke(success);
                }
                else
                {
                    Debug.LogError($"Failed to load formulas: {request.error}");
                    
                    if (useLocalFallback)
                    {
                        LoadFormulasFromResources("formulas");
                    }
                    
                    onComplete?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// Load formulas from file path
        /// </summary>
        public int LoadFormulasFromFile(string filePath)
        {
            return jsonLoader.LoadFromFile(filePath);
        }

        /// <summary>
        /// Export formulas to JSON string
        /// </summary>
        public string ExportToJson(IEnumerable<string> formulaIds = null)
        {
            return jsonLoader.ExportToJson(formulaIds);
        }

        /// <summary>
        /// Export formulas to file
        /// </summary>
        public bool ExportToFile(string filePath, IEnumerable<string> formulaIds = null)
        {
            return jsonLoader.ExportToFile(filePath, formulaIds);
        }

        // ============== OPTIMIZATION ==============

        /// <summary>
        /// Prepare a formula for optimized evaluation
        /// </summary>
        public void PrepareFormula(string formulaId)
        {
            runner.PrepareFormula(formulaId);
        }

        /// <summary>
        /// Enable or disable input pooling
        /// </summary>
        public void SetInputPooling(bool enabled)
        {
            runner.UseInputPooling = enabled;
        }

        // ============== UTILITIES ==============

        /// <summary>
        /// Get system statistics
        /// </summary>
        public string GetStats()
        {
            int formulaCount = loader.GetFormulaCount();
            var runnerStats = runner.GetStats();
            return $"Formulas: {formulaCount}, {runnerStats}";
        }

        /// <summary>
        /// Enable/disable verbose logging
        /// </summary>
        public void SetVerboseLogging(bool enabled)
        {
            verboseLogging = enabled;
            
            if (enabled)
            {
                loader.OnLog += Debug.Log;
                loader.OnError += Debug.LogError;
                runner.OnError += Debug.LogError;
                jsonLoader.OnError += Debug.LogError;
            }
            else
            {
                loader.OnLog -= Debug.Log;
                loader.OnError -= Debug.LogError;
                runner.OnError -= Debug.LogError;
                jsonLoader.OnError -= Debug.LogError;
            }
        }
    }
}