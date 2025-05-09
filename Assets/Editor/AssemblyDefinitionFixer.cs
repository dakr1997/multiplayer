using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class AssemblyDefinitionFixer
{
    [MenuItem("Tools/Fix Assembly Definition References")]
    public static void FixAsmdefReferences()
    {
        // First, gather all asmdef files and their namespaces
        Dictionary<string, string> asmdefPathsToNames = new Dictionary<string, string>();
        Dictionary<string, List<string>> asmdefNameToReferences = new Dictionary<string, List<string>>();
        
        // Find all asmdef files
        string[] asmdefPaths = Directory.GetFiles("Assets/", "*.asmdef", SearchOption.AllDirectories);
        
        foreach (string asmdefPath in asmdefPaths)
        {
            string content = File.ReadAllText(asmdefPath);
            Match nameMatch = Regex.Match(content, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            if (nameMatch.Success)
            {
                string asmdefName = nameMatch.Groups[1].Value;
                asmdefPathsToNames[asmdefPath] = asmdefName;
                asmdefNameToReferences[asmdefName] = new List<string>();
            }
        }
        
        // Find all CS files and extract their using statements
        Dictionary<string, HashSet<string>> folderToUsingNamespaces = new Dictionary<string, HashSet<string>>();
        
        string[] csFilePaths = Directory.GetFiles("Assets/", "*.cs", SearchOption.AllDirectories);
        foreach (string csFilePath in csFilePaths)
        {
            string folderPath = Path.GetDirectoryName(csFilePath);
            if (!folderToUsingNamespaces.ContainsKey(folderPath))
                folderToUsingNamespaces[folderPath] = new HashSet<string>();
                
            string content = File.ReadAllText(csFilePath);
            MatchCollection usingMatches = Regex.Matches(content, @"using\s+([^;]+);");
            foreach (Match match in usingMatches)
            {
                string ns = match.Groups[1].Value.Trim();
                folderToUsingNamespaces[folderPath].Add(ns);
            }
        }
        
        // Map namespaces to asmdef names
        Dictionary<string, string> namespaceToAsmdef = new Dictionary<string, string>();
        foreach (var asmdefEntry in asmdefPathsToNames)
        {
            string asmdefFolder = Path.GetDirectoryName(asmdefEntry.Key).Replace("\\", "/");
            
            // Find all CS files in this folder tree
            string[] csFiles = Directory.GetFiles(asmdefFolder, "*.cs", SearchOption.AllDirectories);
            foreach (string csFile in csFiles)
            {
                string content = File.ReadAllText(csFile);
                Match namespaceMatch = Regex.Match(content, @"namespace\s+([^\s{]+)");
                if (namespaceMatch.Success)
                {
                    string ns = namespaceMatch.Groups[1].Value.Trim();
                    namespaceToAsmdef[ns] = asmdefEntry.Value;
                }
            }
        }
        
        // Special case for Unity's built-in namespaces
        List<string> unityNamespaces = new List<string> {
            "Unity.Netcode",
            "Unity.Collections",
            "Unity.Mathematics"
        };
        
        // Now, determine references for each asmdef
        foreach (var asmdefEntry in asmdefPathsToNames)
        {
            string asmdefFolder = Path.GetDirectoryName(asmdefEntry.Key).Replace("\\", "/");
            HashSet<string> requiredReferences = new HashSet<string>();
            
            // Check each CS file in this folder
            string[] csFiles = Directory.GetFiles(asmdefFolder, "*.cs", SearchOption.AllDirectories);
            foreach (string csFile in csFiles)
            {
                string folderPath = Path.GetDirectoryName(csFile);
                if (folderToUsingNamespaces.ContainsKey(folderPath))
                {
                    foreach (string ns in folderToUsingNamespaces[folderPath])
                    {
                        // Handle Unity special namespaces
                        if (ns.StartsWith("Unity.Netcode"))
                        {
                            requiredReferences.Add("Unity.Netcode.Runtime");
                        }
                        else if (namespaceToAsmdef.ContainsKey(ns))
                        {
                            string referencedAsmdef = namespaceToAsmdef[ns];
                            if (referencedAsmdef != asmdefEntry.Value) // Don't add self-reference
                                requiredReferences.Add(referencedAsmdef);
                        }
                        // Handle other namespace root references
                        else
                        {
                            string nsRoot = ns.Split('.')[0];
                            foreach (var namespaceEntry in namespaceToAsmdef)
                            {
                                if (namespaceEntry.Key.StartsWith(nsRoot + "."))
                                {
                                    requiredReferences.Add(namespaceEntry.Value);
                                }
                            }
                        }
                    }
                }
            }
            
            // Update the asmdef references
            asmdefNameToReferences[asmdefEntry.Value] = requiredReferences.ToList();
        }
        
        // Update all asmdef files with the correct references
        foreach (var asmdefEntry in asmdefPathsToNames)
        {
            string content = File.ReadAllText(asmdefEntry.Key);
            List<string> references = asmdefNameToReferences[asmdefEntry.Value];
            
            // Generate the references JSON array
            string referencesJson = string.Join(",\n    ", references.Select(r => $"\"com.unity.{r}\"").ToArray());
            if (!string.IsNullOrEmpty(referencesJson))
                referencesJson = "\n    " + referencesJson + "\n  ";
                
            // Replace the references in the asmdef file
            content = Regex.Replace(content, 
                "\"references\"\\s*:\\s*\\[(.*?)\\]", 
                $"\"references\": [{referencesJson}]",
                RegexOptions.Singleline);
                
            File.WriteAllText(asmdefEntry.Key, content);
            Debug.Log($"Updated references for {asmdefEntry.Value}");
        }
        
        AssetDatabase.Refresh();
    }
}