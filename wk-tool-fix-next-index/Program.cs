using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static System.Environment;

Regex EXPORT_RGX = new(@"export (?'star'\*) from '(?'path'[\.\w].+)'");
Regex EXPORTED_RGX = new(@"export (const|enum|class) (?'name'\w*)");
Regex EXPORTED_TYPE_RGX = new(@"export (interface|type) (?'name'\w*)");
Regex SUB_INDEX_RGX = new(@"export {(?'targets'.*)}");
Regex SUB_INDEX_TYPE_RGX = new(@"export type {(?'targets'.*)}");

string basePath = Path.Combine(Environment.CurrentDirectory, "..");

Execute(Path.Combine(basePath, "@proxies"));
Execute(Path.Combine(basePath, "app-states"));
Execute(Path.Combine(basePath, "packages"));
Execute(Path.Combine(basePath, "pages"));
Execute(Path.Combine(basePath, "pages-parts"));
Execute(Path.Combine(basePath, "store"));

Console.WriteLine("DONE");
Console.ReadKey();


void Execute(string path, int deep = 0)
{
    Console.Write($"{NewLine}{new string(' ', deep * 2)}{Path.GetFileName(path)}");
    foreach (var subPath in Directory.GetDirectories(path)
                                .Where(p => p != path))
    {
        Execute(subPath, deep + 1);
    }
    string filePath = Path.Combine(path, "index.ts");
    if (File.Exists(filePath))
    {
        StringBuilder builder = new();
        foreach (var line in File.ReadLines(filePath))
        {
            Console.Write(".");
            var match = EXPORT_RGX.Match(line);
            if (match.Success)
            {
                List<string> exported = new();
                List<string> exportedTypes = new();
                string target = match.Groups["path"].Value;
                string[] targets = Directory.GetFiles(path, $"{target}.ts*");
                foreach (string targetPath in targets)
                {
                    string fileContent = File.ReadAllText(targetPath);
                    foreach (Match targetMatch in EXPORTED_RGX.Matches(fileContent))
                    {
                        string targetExport = targetMatch.Groups["name"].Value;
                        exported.Add(targetExport);
                    }
                    foreach (Match targetMatch in EXPORTED_TYPE_RGX.Matches(fileContent))
                    {
                        string targetExport = targetMatch.Groups["name"].Value;
                        exportedTypes.Add(targetExport);
                    }
                }
                string targetDirPath = Path.Combine(path, target);
                if (targets.Length == 0 && Directory.Exists(targetDirPath))
                {
                    string subIndexPath = Path.Combine(targetDirPath, "index.ts");
                    if (File.Exists(subIndexPath))
                    {
                        string subIndexContent = File.ReadAllText(subIndexPath);
                        foreach (Match subMatch in SUB_INDEX_RGX.Matches(subIndexContent))
                        {
                            string subTarget = subMatch.Groups["targets"].Value;
                            exported.Add(subTarget);
                        }
                        foreach (Match subMatch in SUB_INDEX_TYPE_RGX.Matches(subIndexContent))
                        {
                            string subTarget = subMatch.Groups["targets"].Value;
                            exportedTypes.Add(subTarget);
                        }
                    }
                }

                if (exported.Count != 0)
                {
                    string expo = string.Join(" ,", exported);
                    builder.AppendLine($@"export {{ {expo} }} from '{target}';");
                }
                if (exportedTypes.Count != 0)
                {
                    string expo = string.Join(" ,", exportedTypes);
                    builder.AppendLine($@"export type {{ {expo} }} from '{target}';");
                }
            }
            else 
                builder.AppendLine(line); // already fine format
        }
        if (builder.Length != 0)
        {
            File.WriteAllText(filePath, builder.ToString());
        }
    }
}