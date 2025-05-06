using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that analyzes your Unity project and creates a summary file
/// with Mermaid diagrams and key project information for sharing.
/// </summary>
public class ProjectSummarizer : EditorWindow
{
    private Vector2 scrollPosition;
    private bool includeComments = true;
    private bool includeTodos = true;
    private bool includeInterfaces = true;
    private bool includePrivateFields = false;
    private bool includeInheritance = true;
    private bool limitToNamespaces = true;
    private string specificNamespace = "";
    private string outputPath = "ProjectSummary.md";
    private int maxClassesToAnalyze = 50;
    private int daysOfChangesToTrack = 7;
    
    private string lastRunSummary = "";
    private string mermaidDiagram = "";
    
    [MenuItem("Tools/Project Summarizer")]
    public static void ShowWindow()
    {
        GetWindow<ProjectSummarizer>("Project Summarizer");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Project Summarizer Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        GUILayout.Label("Output Settings:", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("Output File Path:", outputPath);
        maxClassesToAnalyze = EditorGUILayout.IntSlider("Max Classes to Analyze:", maxClassesToAnalyze, 10, 200);
        daysOfChangesToTrack = EditorGUILayout.IntSlider("Days of Changes to Track:", daysOfChangesToTrack, 1, 30);
        
        EditorGUILayout.Space();
        GUILayout.Label("Class Analysis Options:", EditorStyles.boldLabel);
        includeComments = EditorGUILayout.Toggle("Include Comments", includeComments);
        includeTodos = EditorGUILayout.Toggle("Include TODOs", includeTodos);
        includeInterfaces = EditorGUILayout.Toggle("Include Interfaces", includeInterfaces);
        includePrivateFields = EditorGUILayout.Toggle("Include Private Fields", includePrivateFields);
        includeInheritance = EditorGUILayout.Toggle("Include Inheritance", includeInheritance);
        
        EditorGUILayout.Space();
        GUILayout.Label("Filtering Options:", EditorStyles.boldLabel);
        limitToNamespaces = EditorGUILayout.Toggle("Limit to Specific Namespace", limitToNamespaces);
        
        if (limitToNamespaces)
        {
            specificNamespace = EditorGUILayout.TextField("Namespace Filter:", specificNamespace);
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Project Summary"))
        {
            GenerateProjectSummary();
        }
        
        EditorGUILayout.Space();
        if (!string.IsNullOrEmpty(lastRunSummary))
        {
            GUILayout.Label("Summary Preview:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(lastRunSummary, MessageType.Info);
            
            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = lastRunSummary;
            }
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private void GenerateProjectSummary()
    {
        try
        {
            StringBuilder summary = new StringBuilder();
            
            // Add header information
            summary.AppendLine("# Unity Project Summary");
            summary.AppendLine($"Generated on: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
            summary.AppendLine($"Project: {Application.productName}");
            summary.AppendLine($"Version: {Application.version}");
            summary.AppendLine("");
            
            // Generate project structure
            summary.AppendLine("## Project Structure");
            GenerateProjectStructure(summary);
            
            // Find and analyze C# scripts
            summary.AppendLine("## Code Analysis");
            AnalyzeCodebase(summary);
            
            // Recent changes
            summary.AppendLine("## Recent Changes");
            TrackRecentChanges(summary);
            
            // TODO items
            if (includeTodos)
            {
                summary.AppendLine("## TODO Items");
                FindTodoItems(summary);
            }
            
            // Generate Mermaid diagram
            summary.AppendLine("## Class Diagram");
            summary.AppendLine("```mermaid");
            summary.AppendLine("classDiagram");
            summary.AppendLine(mermaidDiagram);
            summary.AppendLine("```");
            
            // Write to file
            File.WriteAllText(outputPath, summary.ToString());
            
            // Save preview
            lastRunSummary = $"Summary generated successfully!\nOutput saved to: {outputPath}\n\nPreview (limited):\n" +
                            $"Found {mermaidDiagram.Split('\n').Length} classes for diagram.\n" +
                            $"See the full summary in the output file.";
            
            Debug.Log($"Project summary generated successfully! Output saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error generating project summary: {ex.Message}\n{ex.StackTrace}");
            EditorUtility.DisplayDialog("Error", $"Failed to generate summary: {ex.Message}", "OK");
        }
    }
    
    private void GenerateProjectStructure(StringBuilder summary)
    {
        summary.AppendLine("Key project folders:");
        
        string[] mainFolders = Directory.GetDirectories("Assets", "*", SearchOption.TopDirectoryOnly);
        foreach (string folder in mainFolders)
        {
            string folderName = Path.GetFileName(folder);
            summary.AppendLine($"- {folderName}");
            
            // Count script files in this folder and subfolders
            string[] scripts = Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories);
            if (scripts.Length > 0)
            {
                summary.AppendLine($"  - Contains {scripts.Length} scripts");
            }
            
            // Add some key subfolders (limit to depth 1)
            string[] subFolders = Directory.GetDirectories(folder, "*", SearchOption.TopDirectoryOnly);
            foreach (string subFolder in subFolders.Take(5)) // Limit to 5 subfolders
            {
                summary.AppendLine($"  - {Path.GetFileName(subFolder)}");
            }
            
            if (subFolders.Length > 5)
            {
                summary.AppendLine($"  - ...and {subFolders.Length - 5} more subfolder(s)");
            }
        }
        
        summary.AppendLine("");
    }
    
    private void AnalyzeCodebase(StringBuilder summary)
    {
        string[] scriptFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        
        // Filter by namespace if needed
        if (limitToNamespaces && !string.IsNullOrEmpty(specificNamespace))
        {
            List<string> filteredScripts = new List<string>();
            foreach (string script in scriptFiles)
            {
                string content = File.ReadAllText(script);
                if (content.Contains($"namespace {specificNamespace}") || 
                    content.Contains($"namespace {specificNamespace}."))
                {
                    filteredScripts.Add(script);
                }
            }
            scriptFiles = filteredScripts.ToArray();
        }
        
        // List script statistics
        summary.AppendLine($"Total scripts: {scriptFiles.Length}");
        
        // Limit the number of scripts to analyze
        string[] scriptsToAnalyze = scriptFiles.Take(maxClassesToAnalyze).ToArray();
        
        // Mermaid diagram building
        StringBuilder mermaidBuilder = new StringBuilder();
        Dictionary<string, List<string>> classDependencies = new Dictionary<string, List<string>>();
        Dictionary<string, List<string>> classInheritance = new Dictionary<string, List<string>>();
        
        // Count statistics 
        int monoBehaviourCount = 0;
        int scriptableObjectCount = 0;
        int interfaceCount = 0;
        int staticClassCount = 0;
        
        // Analyze each script
        foreach (string scriptFile in scriptsToAnalyze)
        {
            string fileName = Path.GetFileNameWithoutExtension(scriptFile);
            string content = File.ReadAllText(scriptFile);
            
            // Check if it's a MonoBehaviour
            if (content.Contains(" : MonoBehaviour") || content.Contains(":MonoBehaviour"))
            {
                monoBehaviourCount++;
                
                // Add to mermaid diagram
                mermaidBuilder.AppendLine($"  class {fileName} {{");
                mermaidBuilder.AppendLine($"    +MonoBehaviour");
                
                // Extract public methods
                MatchCollection methodMatches = Regex.Matches(content, @"public\s+\w+\s+(\w+)\s*\(");
                foreach (Match match in methodMatches)
                {
                    string methodName = match.Groups[1].Value;
                    // Skip Unity standard methods
                    if (!IsUnityStandardMethod(methodName))
                    {
                        mermaidBuilder.AppendLine($"    +{methodName}()");
                    }
                }
                
                // Add inheritance
                if (includeInheritance)
                {
                    mermaidBuilder.AppendLine("  }");
                    mermaidBuilder.AppendLine($"  MonoBehaviour <|-- {fileName}");
                    
                    if (!classInheritance.ContainsKey("MonoBehaviour"))
                    {
                        classInheritance["MonoBehaviour"] = new List<string>();
                    }
                    classInheritance["MonoBehaviour"].Add(fileName);
                }
                else
                {
                    mermaidBuilder.AppendLine("  }");
                }
            }
            // Check if it's a ScriptableObject
            else if (content.Contains(" : ScriptableObject") || content.Contains(":ScriptableObject"))
            {
                scriptableObjectCount++;
                
                // Add to mermaid diagram
                mermaidBuilder.AppendLine($"  class {fileName} {{");
                mermaidBuilder.AppendLine($"    +ScriptableObject");
                
                // Extract public methods
                MatchCollection methodMatches = Regex.Matches(content, @"public\s+\w+\s+(\w+)\s*\(");
                foreach (Match match in methodMatches)
                {
                    string methodName = match.Groups[1].Value;
                    mermaidBuilder.AppendLine($"    +{methodName}()");
                }
                
                // Add inheritance
                if (includeInheritance)
                {
                    mermaidBuilder.AppendLine("  }");
                    mermaidBuilder.AppendLine($"  ScriptableObject <|-- {fileName}");
                    
                    if (!classInheritance.ContainsKey("ScriptableObject"))
                    {
                        classInheritance["ScriptableObject"] = new List<string>();
                    }
                    classInheritance["ScriptableObject"].Add(fileName);
                }
                else
                {
                    mermaidBuilder.AppendLine("  }");
                }
            }
            // Check if it's an interface
            else if (content.Contains("interface "))
            {
                if (includeInterfaces)
                {
                    interfaceCount++;
                    
                    // Add to mermaid diagram
                    mermaidBuilder.AppendLine($"  class {fileName} {{");
                    mermaidBuilder.AppendLine($"    <<interface>>");
                    
                    // Extract interface methods
                    MatchCollection methodMatches = Regex.Matches(content, @"\s+\w+\s+(\w+)\s*\(");
                    foreach (Match match in methodMatches)
                    {
                        string methodName = match.Groups[1].Value;
                        mermaidBuilder.AppendLine($"    +{methodName}()");
                    }
                    
                    mermaidBuilder.AppendLine("  }");
                }
            }
            // Other classes
            else if (content.Contains("class "))
            {
                // Check if static
                if (content.Contains("static class "))
                {
                    staticClassCount++;
                }
                
                // Add to mermaid diagram
                mermaidBuilder.AppendLine($"  class {fileName} {{");
                
                // Extract public methods
                MatchCollection methodMatches = Regex.Matches(content, @"public\s+\w+\s+(\w+)\s*\(");
                foreach (Match match in methodMatches)
                {
                    string methodName = match.Groups[1].Value;
                    mermaidBuilder.AppendLine($"    +{methodName}()");
                }
                
                // Check inheritance
                Match inheritanceMatch = Regex.Match(content, @"class\s+\w+\s+:\s+(\w+)");
                if (inheritanceMatch.Success && includeInheritance)
                {
                    string baseClass = inheritanceMatch.Groups[1].Value;
                    mermaidBuilder.AppendLine("  }");
                    mermaidBuilder.AppendLine($"  {baseClass} <|-- {fileName}");
                    
                    if (!classInheritance.ContainsKey(baseClass))
                    {
                        classInheritance[baseClass] = new List<string>();
                    }
                    classInheritance[baseClass].Add(fileName);
                }
                else
                {
                    mermaidBuilder.AppendLine("  }");
                }
            }
            
            // Find dependencies (using keyword, field declarations)
            MatchCollection dependencyMatches = Regex.Matches(content, @"\[SerializeField\]\s+(?:private|protected|public)?\s+(\w+)\s+\w+");
            foreach (Match match in dependencyMatches)
            {
                string dependencyType = match.Groups[1].Value;
                
                // Skip primitive types and common Unity types
                if (!IsPrimitiveOrCommonType(dependencyType))
                {
                    if (!classDependencies.ContainsKey(fileName))
                    {
                        classDependencies[fileName] = new List<string>();
                    }
                    
                    if (!classDependencies[fileName].Contains(dependencyType))
                    {
                        classDependencies[fileName].Add(dependencyType);
                        mermaidBuilder.AppendLine($"  {fileName} --> {dependencyType} : references");
                    }
                }
            }
        }
        
        mermaidDiagram = mermaidBuilder.ToString();
        
        // Add statistics
        summary.AppendLine($"- MonoBehaviour scripts: {monoBehaviourCount}");
        summary.AppendLine($"- ScriptableObject scripts: {scriptableObjectCount}");
        summary.AppendLine($"- Interfaces: {interfaceCount}");
        summary.AppendLine($"- Static classes: {staticClassCount}");
        summary.AppendLine("");
        
        // Top base classes and their implementations
        if (includeInheritance && classInheritance.Count > 0)
        {
            summary.AppendLine("### Key inheritance hierarchies:");
            foreach (var baseClass in classInheritance.OrderByDescending(x => x.Value.Count).Take(5))
            {
                summary.AppendLine($"- {baseClass.Key}: {baseClass.Value.Count} implementations");
                foreach (var implementation in baseClass.Value.Take(5))
                {
                    summary.AppendLine($"  - {implementation}");
                }
                
                if (baseClass.Value.Count > 5)
                {
                    summary.AppendLine($"  - ...and {baseClass.Value.Count - 5} more");
                }
            }
            summary.AppendLine("");
        }
        
        // Most referenced classes
        if (classDependencies.Count > 0)
        {
            summary.AppendLine("### Most referenced classes:");
            var dependencyCount = new Dictionary<string, int>();
            
            foreach (var dependency in classDependencies)
            {
                foreach (var target in dependency.Value)
                {
                    if (!dependencyCount.ContainsKey(target))
                    {
                        dependencyCount[target] = 0;
                    }
                    dependencyCount[target]++;
                }
            }
            
            foreach (var item in dependencyCount.OrderByDescending(x => x.Value).Take(10))
            {
                summary.AppendLine($"- {item.Key}: referenced by {item.Value} classes");
            }
            summary.AppendLine("");
        }
    }
    
    private void TrackRecentChanges(StringBuilder summary)
    {
        string[] scriptFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        
        List<FileInfo> recentlyModifiedFiles = new List<FileInfo>();
        DateTime cutoffDate = DateTime.Now.AddDays(-daysOfChangesToTrack);
        
        foreach (string script in scriptFiles)
        {
            FileInfo fileInfo = new FileInfo(script);
            if (fileInfo.LastWriteTime > cutoffDate)
            {
                recentlyModifiedFiles.Add(fileInfo);
            }
        }
        
        // Sort by most recently modified
        recentlyModifiedFiles = recentlyModifiedFiles.OrderByDescending(f => f.LastWriteTime).ToList();
        
        summary.AppendLine($"Files modified in the last {daysOfChangesToTrack} days:");
        
        if (recentlyModifiedFiles.Count == 0)
        {
            summary.AppendLine("- No files have been modified in this period");
        }
        else
        {
            foreach (var file in recentlyModifiedFiles.Take(20)) // Limit to 20 files
            {
                string relativePath = file.FullName.Replace(Directory.GetCurrentDirectory(), "").TrimStart('\\', '/');
                summary.AppendLine($"- {relativePath} (modified {file.LastWriteTime.ToString("yyyy-MM-dd")})");
            }
            
            if (recentlyModifiedFiles.Count > 20)
            {
                summary.AppendLine($"- ...and {recentlyModifiedFiles.Count - 20} more files");
            }
        }
        
        summary.AppendLine("");
    }
    
    private void FindTodoItems(StringBuilder summary)
    {
        string[] scriptFiles = Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories);
        
        List<TodoItem> todoItems = new List<TodoItem>();
        
        foreach (string scriptFile in scriptFiles)
        {
            string content = File.ReadAllText(scriptFile);
            string fileName = Path.GetFileName(scriptFile);
            
            // Find // TODO comments
            MatchCollection todoMatches = Regex.Matches(content, @"//\s*TODO:?\s*(.+)$", RegexOptions.Multiline);
            foreach (Match match in todoMatches)
            {
                todoItems.Add(new TodoItem
                {
                    FilePath = fileName,
                    Description = match.Groups[1].Value.Trim(),
                    IsCommented = true
                });
            }
            
            // Find /* TODO */ style comments
            todoMatches = Regex.Matches(content, @"/\*\s*TODO:?\s*(.+?)\*/", RegexOptions.Singleline);
            foreach (Match match in todoMatches)
            {
                todoItems.Add(new TodoItem
                {
                    FilePath = fileName,
                    Description = match.Groups[1].Value.Trim(),
                    IsCommented = true
                });
            }
        }
        
        if (todoItems.Count == 0)
        {
            summary.AppendLine("- No TODO items found");
        }
        else
        {
            foreach (var item in todoItems)
            {
                summary.AppendLine($"- [{item.FilePath}] {item.Description}");
            }
        }
        
        summary.AppendLine("");
    }
    
    private bool IsUnityStandardMethod(string methodName)
    {
        string[] standardMethods = new string[] 
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", 
            "OnEnable", "OnDisable", "OnDestroy", "OnApplicationQuit",
            "OnApplicationPause", "OnApplicationFocus", "OnValidate",
            "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
            "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
            "OnMouseDown", "OnMouseUp", "OnMouseOver", "OnMouseExit",
            "OnDrawGizmos", "OnDrawGizmosSelected"
        };
        
        return Array.IndexOf(standardMethods, methodName) >= 0;
    }
    
    private bool IsPrimitiveOrCommonType(string typeName)
    {
        string[] commonTypes = new string[]
        {
            "int", "float", "double", "bool", "string", "void",
            "Vector2", "Vector3", "Vector4", "Quaternion", "Color",
            "Transform", "GameObject", "RectTransform", "Object"
        };
        
        return Array.IndexOf(commonTypes, typeName) >= 0;
    }
    
    private class TodoItem
    {
        public string FilePath { get; set; }
        public string Description { get; set; }
        public bool IsCommented { get; set; }
    }
}