using MicroserviceAnalyzer.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;

namespace MicroserviceAnalyzer;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Microservice Analyzer");
        Console.WriteLine("====================");

        // Check for test commands
        if (args.Length > 0)
        {
            if (args[0].Equals("test", StringComparison.OrdinalIgnoreCase))
            {
                string testOutputPath = args.Length > 1 ? args[1] : "test_diagram.drawio.xml";
                SampleTest.CreateSampleDiagram(testOutputPath);
                return;
            }
            else if (args[0].Equals("simple", StringComparison.OrdinalIgnoreCase))
            {
                string simpleOutputPath = args.Length > 1 ? args[1] : "simple_diagram.drawio.xml";
                TestSimple.CreateSimpleDiagram(simpleOutputPath);
                return;
            }
            else if (args[0].Equals("iftest", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Running if statement detection test");
                
                string testCode = @"
                public class TestClass
                {
                    public void TestMethod()
                    {
                        var file = GetFile();
                        if (file == null || file.Length == 0)
                        {
                            Observer.LogData(""No file found"");
                            return;
                        }
                        
                        ProcessFile(file);
                    }
                }";
                
                // Save to temporary file
                string tempFile = "iftest.cs";
                File.WriteAllText(tempFile, testCode);
                
                Console.WriteLine("Test code written to temporary file");
                
                // Analyze with our CodeAnalyzer
                var codeAnalyzer = new CodeAnalyzer();
                var syntaxTree = CSharpSyntaxTree.ParseText(testCode);
                var root = await syntaxTree.GetRootAsync();
                
                Console.WriteLine("Syntax tree parsed. Looking for if statements...");
                
                // Find the method
                var methodDeclarations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();
                foreach (var method in methodDeclarations)
                {
                    Console.WriteLine($"Found method: {method.Identifier.ValueText}");
                    
                    // Look for if statements
                    var ifStatements = method.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IfStatementSyntax>();
                    foreach (var ifStmt in ifStatements)
                    {
                        Console.WriteLine($"Found if statement: {ifStmt.Condition}");
                        Console.WriteLine($"Contains Observer.LogData: {ifStmt.ToString().Contains("Observer.LogData")}");
                        Console.WriteLine($"Contains null check: {ifStmt.Condition.ToString().Contains("null")}");
                        Console.WriteLine($"Contains Length check: {ifStmt.Condition.ToString().Contains("Length")}");
                        Console.WriteLine($"Contains return: {ifStmt.ToString().Contains("return")}");
                    }
                }
                
                Console.WriteLine("If test completed");
                return;
            }
        }

        if (args.Length < 3)
        {
            Console.WriteLine("Usage: MicroserviceAnalyzer <project_path> <class_name> <method_name> [output_file]");
            Console.WriteLine("  project_path: Path to the C# project directory");
            Console.WriteLine("  class_name: Name of the class where the flow starts");
            Console.WriteLine("  method_name: Name of the method where the flow starts");
            Console.WriteLine("  output_file: (Optional) Path to save the draw.io XML file (default: flow_diagram.drawio.xml)");
            Console.WriteLine("");
            Console.WriteLine("For testing:");
            Console.WriteLine("  dotnet run test [output_file]  - Run with sample diagram");
            Console.WriteLine("  dotnet run simple [output_file] - Run with simple diagram (minimal format)");
            return;
        }

        string projectPath = args[0];
        string className = args[1];
        string methodName = args[2];
        string outputPath = args.Length > 3 ? args[3] : "flow_diagram.drawio.xml";

        if (!Directory.Exists(projectPath))
        {
            Console.WriteLine($"Error: Project directory '{projectPath}' does not exist.");
            return;
        }

        try
        {
            Console.WriteLine($"Analyzing project at: {projectPath}");
            Console.WriteLine($"Starting from: {className}.{methodName}");
            Console.WriteLine($"Output will be saved to: {outputPath}");
            Console.WriteLine("Enabling verbose debug output to diagnose if statement detection issues");

            var codeAnalyzer = new CodeAnalyzer();
            var diagram = await codeAnalyzer.AnalyzeProjectAsync(projectPath, className, methodName);

            Console.WriteLine($"Analysis complete. Found {diagram.Nodes.Count} nodes and {diagram.Connections.Count} connections.");
            Console.WriteLine($"If statement detection: {diagram.Nodes.Count(n => n.Label.StartsWith("If "))} if statements found");

            var diagramGenerator = new DiagramGenerator();
            diagramGenerator.GenerateDiagram(diagram, outputPath);

            Console.WriteLine($"Draw.io diagram saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
