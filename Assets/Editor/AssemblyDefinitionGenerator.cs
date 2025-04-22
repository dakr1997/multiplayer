using UnityEditor;
using UnityEngine;
using System.IO;

public class AssemblyDefinitionGenerator
{
    [MenuItem("Tools/Generate Assembly Definition Files")]
    public static void GenerateAsmdefFiles()
    {
        string assetsPath = "Assets/";
        string[] directories = Directory.GetDirectories(assetsPath, "*", SearchOption.AllDirectories);

        foreach (string dir in directories)
        {
            string[] scripts = Directory.GetFiles(dir, "*.cs");
            if (scripts.Length > 0)
            {
                string asmdefPath = Path.Combine(dir, $"{Path.GetFileName(dir)}.asmdef");
                if (!File.Exists(asmdefPath))
                {
                    File.WriteAllText(asmdefPath, GenerateAsmdefContent(Path.GetFileName(dir)));
                    Debug.Log($"Assembly Definition File created at: {asmdefPath}");
                }
            }
        }

        AssetDatabase.Refresh();
    }

    private static string GenerateAsmdefContent(string assemblyName)
    {
        return $"{{\n" +
               $"  \"name\": \"{assemblyName}\",\n" +
               $"  \"references\": [],\n" +
               $"  \"optionalUnityReferences\": [],\n" +
               $"  \"includePlatforms\": [],\n" +
               $"  \"excludePlatforms\": [],\n" +
               $"  \"allowUnsafeCode\": false,\n" +
               $"  \"overrideReferences\": false,\n" +
               $"  \"precompiledReferences\": [],\n" +
               $"  \"autoReferenced\": true,\n" +
               $"  \"defineConstraints\": [],\n" +
               $"  \"versionDefines\": []\n" +
               $"}}";
    }
}
