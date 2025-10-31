using System.Collections.Generic;
using System.Linq;
using FormulaKit.Runtime;
using UnityEditor;
using UnityEngine;

namespace FormulaKit.Editor.Tools.Tools
{
    /// <summary>
    /// Simple and clean formula editor
    /// Tools -> Formula Framework -> Formula Builder
    /// </summary>
    public class FormulaBuilderWindow : EditorWindow
    {
        // Formula data
        private string _formulaId = "";
        private string _formulaExpression = "";
        
        // Test data
        private readonly Dictionary<string, float> _testInputs = new Dictionary<string, float>();
        private float _testResult = 0f;
        private string _statusMessage = "";
        private MessageType _statusType = MessageType.None;
        
        // Scroll positions
        private Vector2 _expressionScroll;
        private Vector2 _inputsScroll;
        
        // Test components
        private FormulaLoader _tempLoader;
        private FormulaRunner _tempRunner;
        
        [MenuItem("Tools/Formula Framework/Formula Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaBuilderWindow>("Formula Builder");
            window.minSize = new Vector2(500, 600);
            window.Show();
        }
        
        private void OnEnable()
        {
            _tempLoader = new FormulaLoader();
            _tempRunner = new FormulaRunner(_tempLoader);
        }
        
        private void OnGUI()
        {
            GUILayout.Space(10);
            
            DrawHeader();
            GUILayout.Space(15);
            
            DrawFormulaEditor();
            GUILayout.Space(15);
            
            DrawTestSection();
            GUILayout.Space(15);
            
           // DrawActions();
           // GUILayout.Space(10);
            
            DrawStatus();
        }
        
        // ============== HEADER ==============
        
        private void DrawHeader()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            
            GUILayout.Label("Formula Builder", style);
            
            GUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Create and test formulas with live feedback",
                MessageType.None);
        }
        
        // ============== FORMULA EDITOR ==============
        
        private void DrawFormulaEditor()
        {
            EditorGUILayout.LabelField("Create Formula", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Formula ID
            EditorGUILayout.LabelField("ID:", EditorStyles.miniBoldLabel);
            _formulaId = EditorGUILayout.TextField(_formulaId);
            
            GUILayout.Space(10);
            
            // Formula Expression
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Expression:", EditorStyles.miniBoldLabel);
            
            if (GUILayout.Button("Examples ▼", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                ShowExamplesMenu();
            }
            
            if (GUILayout.Button("Help", EditorStyles.miniButton, GUILayout.Width(50)))
            {
                ShowSyntaxHelp();
            }
            
            EditorGUILayout.EndHorizontal();
            
            _expressionScroll = EditorGUILayout.BeginScrollView(_expressionScroll, GUILayout.Height(120));
            
            var textStyle = new GUIStyle(EditorStyles.textArea)
            {
                wordWrap = true,
                fontSize = 12
            };
            
            _formulaExpression = EditorGUILayout.TextArea(_formulaExpression, textStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.EndVertical();
        }
        
        // ============== TEST SECTION ==============
        
        private void DrawTestSection()
        {
            EditorGUILayout.LabelField("Test Formula", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            // Auto-detect inputs button
            if (GUILayout.Button("🔄 Auto-Detect Inputs", GUILayout.Height(25)))
            {
                AutoDetectInputs();
            }
            
            GUILayout.Space(10);
            
            // Input fields
            if (_testInputs.Count == 0)
            {
                EditorGUILayout.HelpBox("No inputs. Click 'Auto-Detect' or add manually.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField("Inputs:", EditorStyles.miniBoldLabel);
                
                _inputsScroll = EditorGUILayout.BeginScrollView(_inputsScroll, GUILayout.Height(150));
                
                var keys = _testInputs.Keys.ToList();
                foreach (var key in keys)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    EditorGUILayout.LabelField(key, GUILayout.Width(120));
                    _testInputs[key] = EditorGUILayout.FloatField(_testInputs[key]);
                    
                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        _testInputs.Remove(key);
                        GUIUtility.ExitGUI();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.Space(5);
            
            // Add/Clear buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("+ Add Input"))
            {
                _testInputs["newInput"] = 0f;
            }
            
            if (GUILayout.Button("Clear All"))
            {
                _testInputs.Clear();
            }
            
            if (GUILayout.Button("Random Values"))
            {
                foreach (var key in _testInputs.Keys.ToList())
                {
                    _testInputs[key] = Random.Range(0f, 100f);
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Evaluate button
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            
            if (GUILayout.Button("▶ EVALUATE", GUILayout.Height(35)))
            {
                EvaluateFormula();
            }
            
            GUI.backgroundColor = oldColor;
            
            GUILayout.Space(10);
            
            // Result display
            EditorGUILayout.LabelField("Result:", EditorStyles.miniBoldLabel);
            
            var resultStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
            };
            
            GUILayout.Box(_testResult.ToString("F4"), resultStyle, GUILayout.Height(40));
            
            EditorGUILayout.EndVertical();
        }
        
        // ============== ACTIONS ==============
        
        private void DrawActions()
        {
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            
            // Save to manager
            if (GUILayout.Button("💾 Save to Manager", GUILayout.Height(30)))
            {
                SaveToManager();
            }
            
            // Load from manager
            if (GUILayout.Button("📂 Load from Manager", GUILayout.Height(30)))
            {
                LoadFromManager();
            }
            
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            // Clear
            if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All", "Clear formula and inputs?", "Yes", "No"))
                {
                    ClearAll();
                }
            }
            
            // View library
            if (GUILayout.Button("📚 View Library", GUILayout.Height(30)))
            {
                FormulaLibraryWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        // ============== STATUS ==============
        
        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
            {
                EditorGUILayout.HelpBox(_statusMessage, _statusType);
            }
        }
        
        // ============== HELPER METHODS ==============
        
        private void AutoDetectInputs()
        {
            _testInputs.Clear();
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                return;
            }
            
            // Parse formula
            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            
            if (success)
            {
                var inputs = _tempLoader.GetRequiredInputs(_formulaId);
                
                // Add new inputs, keep existing values
                foreach (var input in inputs)
                {
                    if (!_testInputs.ContainsKey(input))
                    {
                        _testInputs[input] = 0f;
                    }
                }
                
                ShowStatus($"✓ Detected {inputs.Count} inputs: {string.Join(", ", inputs)}", MessageType.Info);
            }
            else
            {
                ShowStatus("❌ Formula has errors", MessageType.Error);
            }
        }
        
        private void EvaluateFormula()
        {
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                return;
            }
            
            // Register/update formula
            bool success = _tempLoader.RegisterFormula(_formulaId, _formulaExpression);
            
            if (!success)
            {
                ShowStatus("❌ Formula has syntax errors", MessageType.Error);
                return;
            }
            
            // Evaluate
            try
            {
                _testResult = _tempRunner.Evaluate(_formulaId, _testInputs);
                ShowStatus("✓ Evaluation successful!", MessageType.Info);
            }
            catch (System.Exception e)
            {
                ShowStatus($"❌ Error: {e.Message}", MessageType.Error);
                _testResult = 0f;
            }
        }
        
        private void SaveToManager()
        {
            if (!Application.isPlaying)
            {
                ShowStatus("⚠️ Enter Play Mode to save to FormulaManager", MessageType.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(_formulaId) || string.IsNullOrEmpty(_formulaExpression))
            {
                ShowStatus("Enter formula ID and expression first", MessageType.Warning);
                return;
            }
            
            if (FormulaManager.Instance == null)
            {
                ShowStatus("❌ FormulaManager not found in scene", MessageType.Error);
                return;
            }
            
            bool success = FormulaManager.Instance.RegisterFormula(_formulaId, _formulaExpression);
            
            if (success)
            {
                ShowStatus($"✓ Formula '{_formulaId}' saved to FormulaManager!", MessageType.Info);
            }
            else
            {
                ShowStatus("❌ Failed to save formula", MessageType.Error);
            }
        }
        
        private void LoadFromManager()
        {
            if (!Application.isPlaying)
            {
                ShowStatus("⚠️ Enter Play Mode to load from FormulaManager", MessageType.Warning);
                return;
            }
            
            if (FormulaManager.Instance == null)
            {
                ShowStatus("❌ FormulaManager not found in scene", MessageType.Error);
                return;
            }
            
            var allIds = FormulaManager.Instance.GetAllFormulaIds().ToList();
            
            if (allIds.Count == 0)
            {
                ShowStatus("No formulas in FormulaManager", MessageType.Info);
                return;
            }
            
            // Show selection menu
            GenericMenu menu = new GenericMenu();
            
            foreach (var id in allIds)
            {
                menu.AddItem(new GUIContent(id), false, () => LoadFormula(id));
            }
            
            menu.ShowAsContext();
        }
        
        private void LoadFormula(string id)
        {
            var formula = FormulaManager.Instance.GetFormula(id);
            
            if (formula != null)
            {
                _formulaId = id;
                _formulaExpression = formula.Expression;
                
                AutoDetectInputs();
                
                ShowStatus($"✓ Loaded formula '{id}'", MessageType.Info);
            }
        }
        
        private void ClearAll()
        {
            _formulaId = "";
            _formulaExpression = "";
            _testInputs.Clear();
            _testResult = 0f;
            _statusMessage = "";
        }
        
        private void ShowStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
        
        private void ShowExamplesMenu()
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Simple/Basic Damage"), false, () => 
            {
                _formulaId = "damage";
                _formulaExpression = "baseDamage * (1 + strength * 0.1)";
            });
            
            menu.AddItem(new GUIContent("Simple/Health Regeneration"), false, () => 
            {
                _formulaId = "healthRegen";
                _formulaExpression = "baseRegen + vitality * 0.5";
            });
            
            menu.AddItem(new GUIContent("Simple/Experience Required"), false, () => 
            {
                _formulaId = "expRequired";
                _formulaExpression = "100 * pow(level, 1.5)";
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Advanced/Critical Hit"), false, () => 
            {
                _formulaId = "critDamage";
                _formulaExpression = "let isCrit = random < critChance;\nisCrit ? baseDamage * 2 : baseDamage";
            });
            
            menu.AddItem(new GUIContent("Advanced/Damage with Armor"), false, () => 
            {
                _formulaId = "damageWithArmor";
                _formulaExpression = "let dmg = baseDamage * (1 + strength * 0.1);\nlet reduction = armor / (armor + 100);\ndmg * (1 - reduction)";
            });
            
            menu.AddItem(new GUIContent("Advanced/Tiered Bonus"), false, () => 
            {
                _formulaId = "tieredBonus";
                _formulaExpression = "let mult;\nif (score >= 1000) { mult = 3 }\nelse if (score >= 500) { mult = 2 }\nelse { mult = 1 }\nbaseReward * mult";
            });
            
            menu.AddSeparator("");
            
            menu.AddItem(new GUIContent("Complex/Full Damage System"), false, () => 
            {
                _formulaId = "fullDamage";
                _formulaExpression = @"let weaponDmg = baseDamage * (1 + strength * 0.1);
let isCrit = random < critChance;

if (isCrit) {
    weaponDmg *= 2
}

let reduction = armor / (armor + 100);
weaponDmg * (1 - reduction)";
            });
            
            menu.AddItem(new GUIContent("Math/Quadratic Formula+"), false, () => 
            {
                _formulaId = "quadraticSolution";
                _formulaExpression = "((-1*b)+sqrt((b^2)-(4*a*c)))/(2*a)";
            });
            menu.AddItem(new GUIContent("Math/Quadratic Formula-"), false, () => 
            {
                _formulaId = "quadraticSolutionNeg";
                _formulaExpression = "((-1*b)-sqrt((b^2)-(4*a*c)))/(2*a)";
            });
            
            
            menu.ShowAsContext();
        }
        
        private void ShowSyntaxHelp()
        {
            string help = @"FORMULA SYNTAX QUICK REFERENCE

Variables:
  let bonus = damage * 0.5;

Conditionals:
  if (health < 50) { bonus = 10 }

Ternary:
  result = score > 100 ? 10 : 5;

Operators:
  +  -  *  /  ^  (math)
  <  >  <=  >=  ==  !=  (comparison)
  &&  ||  !  (logical)

Functions:
  sqrt, abs, pow, min, max
  clamp, lerp, floor, ceil
  sin, cos, tan

Examples:
  baseDamage * (1 + strength * 0.1)
  clamp(speed, 0, maxSpeed)
  pow(level, 1.5)";
            
            EditorUtility.DisplayDialog("Syntax Help", help, "OK");
        }
    }
    
    // ============== LIBRARY WINDOW ==============
    
    /// <summary>
    /// Simple formula library viewer
    /// </summary>
    public class FormulaLibraryWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string searchFilter = "";
        
        public static void ShowWindow()
        {
            var window = GetWindow<FormulaLibraryWindow>("Formula Library");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Formula Library", EditorStyles.boldLabel);
            
            GUILayout.Space(5);
            
            // Search
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            if (!Application.isPlaying || FormulaManager.Instance == null)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view formulas from FormulaManager", MessageType.Info);
                return;
            }
            
            // Formula list
            var allIds = FormulaManager.Instance.GetAllFormulaIds().ToList();
            var filteredIds = string.IsNullOrEmpty(searchFilter)
                ? allIds
                : allIds.Where(id => id.ToLower().Contains(searchFilter.ToLower())).ToList();
            
            EditorGUILayout.LabelField($"Formulas ({filteredIds.Count}):", EditorStyles.miniBoldLabel);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            foreach (var id in filteredIds)
            {
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(id, EditorStyles.boldLabel);
                
                if (GUILayout.Button("Edit", GUILayout.Width(60)))
                {
                    LoadIntoBuilder(id);
                }
                
                if (GUILayout.Button("×", GUILayout.Width(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete", $"Delete '{id}'?", "Yes", "No"))
                    {
                        FormulaManager.Instance.RemoveFormula(id);
                        GUIUtility.ExitGUI();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Show expression preview
                var formula = FormulaManager.Instance.GetFormula(id);
                if (formula != null)
                {
                    var preview = formula.Expression.Length > 100
                        ? formula.Expression.Substring(0, 100) + "..."
                        : formula.Expression;
                    
                    EditorGUILayout.LabelField(preview, EditorStyles.wordWrappedMiniLabel);
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            // Actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Import JSON", GUILayout.Height(30)))
            {
                ImportJSON();
            }
            
            if (GUILayout.Button("Export JSON", GUILayout.Height(30)))
            {
                ExportJSON();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void LoadIntoBuilder(string id)
        {
            var builder = GetWindow<FormulaBuilderWindow>("Formula Builder");
            builder.Focus();
            
           
        }
        
        private void ImportJSON()
        {
            string path = EditorUtility.OpenFilePanel("Import Formulas", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                int count = FormulaManager.Instance.LoadFormulasFromFile(path);
                EditorUtility.DisplayDialog("Import", $"Imported {count} formulas", "OK");
            }
        }
        
        private void ExportJSON()
        {
            string path = EditorUtility.SaveFilePanel("Export Formulas", "", "formulas.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                bool success = FormulaManager.Instance.ExportToFile(path);
                EditorUtility.DisplayDialog("Export", 
                    success ? "Export successful!" : "Export failed", "OK");
            }
        }
    }
}