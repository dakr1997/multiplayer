using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to fix deprecated FindObjectOfType/FindObjectsOfType calls
/// </summary>
public class FindObjectsDeprecationFixer : EditorWindow
{
    private string rootDirectory = "Assets";
    private string[] excludeFolders = new string[] { "Library", "Packages", "Temp" };
    private string logOutput = "";
    private Vector2 scrollPosition;
    private bool dryRun = true;

    [MenuItem("Tools/Fix FindObjectOfType Deprecation")]
    public static void ShowWindow()
    {
        GetWindow<FindObjectsDeprecationFixer>("Fix Deprecated Find Methods");
    }

    private void OnGUI()
    {
        GUILayout.Label("Fix Deprecated FindObjectOfType Methods", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This utility replaces deprecated FindObjectOfType/FindObjectsOfType calls with their recommended alternatives.", MessageType.Info);
        
        GUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Root Directory");
        rootDirectory = EditorGUILayout.TextField(rootDirectory);
        if (GUILayout.Button("Browse", GUILayout.Width(80)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Root Directory", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                rootDirectory = path.Replace(Application.dataPath, "Assets");
            }
        }
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(5);
        dryRun = EditorGUILayout.Toggle("Dry Run (Preview Only)", dryRun);
        
        GUILayout.Space(10);
        if (GUILayout.Button("Fix Deprecated Find Methods"))
        {
            ProcessScripts();
        }
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Results:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
        EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void ProcessScripts()
    {
        logOutput = "";
        int totalFilesProcessed = 0;
        int totalReplacements = 0;
        
        // Find all C# script files
        string[] scriptFiles = Directory.GetFiles(rootDirectory, "*.cs", SearchOption.AllDirectories);
        
        foreach (string file in scriptFiles)
        {
            if (ShouldSkipFile(file)) continue;
            
            string content = File.ReadAllText(file);
            string originalContent = content;
            int replacementsInFile = 0;
            
            // Fix single object find
            // Replace FindAnyObjectByType<T>() with FindFirstObjectByType<T>() or FindAnyObjectByType<T>()
            content = Regex.Replace(content, @"FindObjectOfType\s*<([^>]+)>\s*\(\s*\)", match => {
                replacementsInFile++;
                return $"FindAnyObjectByType<{match.Groups[1].Value}>()";
            });
            
            // Fix FindObjectOfType with boolean param
            content = Regex.Replace(content, @"FindObjectOfType\s*<([^>]+)>\s*\(\s*([^)]+)\s*\)", match => {
                replacementsInFile++;
                return $"FindAnyObjectByType<{match.Groups[1].Value}>({match.Groups[2].Value})";
            });
            
            // Fix FindObjectsOfType
            content = Regex.Replace(content, @"FindObjectsOfType\s*<([^>]+)>\s*\(\s*\)", match => {
                replacementsInFile++;
                return $"FindObjectsByType<{match.Groups[1].Value}>(FindObjectsSortMode.None)";
            });
            
            // Fix FindObjectsOfType with boolean param
            content = Regex.Replace(content, @"FindObjectsOfType\s*<([^>]+)>\s*\(\s*([^)]+)\s*\)", match => {
                replacementsInFile++;
                return $"FindObjectsByType<{match.Groups[1].Value}>(FindObjectsSortMode.None, {match.Groups[2].Value})";
            });
            
            if (replacementsInFile > 0)
            {
                totalReplacements += replacementsInFile;
                LogOutput($"File: {file} - Replacements: {replacementsInFile}");
                
                // Save the modified content if not in dry run mode
                if (!dryRun && content != originalContent)
                {
                    File.WriteAllText(file, content);
                }
            }
            
            totalFilesProcessed++;
        }
        
        LogOutput($"\nTotal files processed: {totalFilesProcessed}");
        LogOutput($"Total replacements: {totalReplacements}");
        if (dryRun)
        {
            LogOutput("Dry run completed. No files were modified.");
        }
        else
        {
            LogOutput("All replacements completed successfully.");
            AssetDatabase.Refresh();
        }
    }
    
    private bool ShouldSkipFile(string filePath)
    {
        foreach (string folder in excludeFolders)
        {
            if (filePath.Contains($"/{folder}/"))
                return true;
        }
        return false;
    }
    
    private void LogOutput(string message)
    {
        logOutput += message + "\n";
        Debug.Log(message);
    }
}