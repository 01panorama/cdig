using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MicroserviceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MicroserviceAnalyzer.Services;

public class CodeAnalyzer
{
    private readonly Dictionary<string, string> _microserviceMap = new Dictionary<string, string>();
    
    public async Task<Diagram> AnalyzeProjectAsync(string projectPath, string startingClassName, string startingMethodName)
    {
        Console.WriteLine($"Starting analysis for {startingClassName}.{startingMethodName}");
        var diagram = new Diagram
        {
            SourceClassName = startingClassName,
            SourceMethodName = startingMethodName
        };

        // First, discover microservices in the solution
        DiscoverMicroservices(projectPath);

        var csharpFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);
        Console.WriteLine($"Found {csharpFiles.Length} C# files in {projectPath}");
        
        var sourceNodes = new List<DiagramNode>();
        var connections = new List<DiagramConnection>();

        // Create start node
        var startNode = new DiagramNode
        {
            Label = $"{startingClassName}.{startingMethodName}",
            Type = NodeType.Start,
            X = 80,
            Y = 100,
            OriginalClassName = startingClassName,
            OriginalMethodName = startingMethodName,
            MicroserviceName = GetMicroserviceForClass(startingClassName, csharpFiles)
        };

        sourceNodes.Add(startNode);
        int yOffset = 100;
        
        foreach (var file in csharpFiles)
        {
            Console.WriteLine($"Examining file: {Path.GetFileName(file)}");
            
            var sourceText = await File.ReadAllTextAsync(file);
            if (sourceText.Contains($"class {startingClassName}") || sourceText.Contains($"class {startingClassName} :"))
            {
                Console.WriteLine($"Found class {startingClassName} in {Path.GetFileName(file)}");
            }
            
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
            var root = await syntaxTree.GetRootAsync();

            // Count if statements in the file to debug
            var allIfStatements = root.DescendantNodes().OfType<IfStatementSyntax>();
            Console.WriteLine($"Found {allIfStatements.Count()} if statements in {Path.GetFileName(file)}");
            
            // Find class declarations
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            
            foreach (var classDeclaration in classDeclarations)
            {
                string className = classDeclaration.Identifier.ValueText;
                Console.WriteLine($"Examining class: {className}");
                
                // If this is the class we're looking for
                if (className == startingClassName)
                {
                    Console.WriteLine($"Found target class: {className}");
                    
                    // Find the target method
                    var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>();
                    
                    foreach (var methodDeclaration in methodDeclarations)
                    {
                        string methodName = methodDeclaration.Identifier.ValueText;
                        
                        // If this is the method we're looking for
                        if (methodName == startingMethodName)
                        {
                            Console.WriteLine($"Found target method: {methodName}");
                            
                            // Check if the method contains if statements
                            var methodIfStatements = methodDeclaration.DescendantNodes().OfType<IfStatementSyntax>();
                            foreach (var ifStmt in methodIfStatements)
                            {
                                Console.WriteLine($"Method contains if statement: {ifStmt.Condition}");
                                Console.WriteLine($"    Has return: {ifStmt.ToString().Contains("return")}");
                                Console.WriteLine($"    Contains Observer.LogData: {ifStmt.ToString().Contains("Observer.LogData")}");
                                Console.WriteLine($"    Contains file check: {ifStmt.Condition.ToString().Contains("file")}");
                            }
                            
                            // Analyze method body
                            int currentYOffset = yOffset;
                            AnalyzeMethodBody(methodDeclaration, startNode, sourceNodes, connections, projectPath, currentYOffset, csharpFiles);
                            yOffset = currentYOffset;
                        }
                    }
                }
            }
        }

        diagram.Nodes = sourceNodes;
        diagram.Connections = connections;
        
        return diagram;
    }

    private void DiscoverMicroservices(string projectPath)
    {
        var projectFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
        foreach (var projectFile in projectFiles)
        {
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectDir = Path.GetDirectoryName(projectFile) ?? string.Empty;
            
            // Map all projects found, not just ones with specific suffixes
            _microserviceMap[projectDir] = projectName;
        }
    }

    private string GetMicroserviceForClass(string className, string[] csharpFiles)
    {
        // Find the file that contains this class
        foreach (var file in csharpFiles)
        {
            string content = File.ReadAllText(file);
            if (Regex.IsMatch(content, $@"\bclass\s+{className}\b"))
            {
                string directory = Path.GetDirectoryName(file) ?? string.Empty;
                
                // Find the microservice that contains this directory
                foreach (var kvp in _microserviceMap)
                {
                    if (directory.StartsWith(kvp.Key))
                    {
                        return kvp.Value;
                    }
                }
            }
        }
        
        return "Unknown";
    }

    private void AnalyzeMethodBody(
        MethodDeclarationSyntax methodDeclaration, 
        DiagramNode sourceNode, 
        List<DiagramNode> nodes, 
        List<DiagramConnection> connections,
        string projectPath,
        int yOffset,
        string[] csharpFiles)
    {
        var methodBody = methodDeclaration.Body;
        if (methodBody == null) return;

        DiagramNode lastNode = sourceNode;
        int xPos = 250;
        yOffset += 100;
        
        // Analyze statements in method body
        foreach (var statement in methodBody.Statements)
        {
            Console.WriteLine($"Processing statement: {statement.GetType().Name}");
            
            // Check statement type and create appropriate nodes
            if (statement is IfStatementSyntax ifStatement)
            {
                Console.WriteLine($"Found if statement with condition: {ifStatement.Condition}");
                
                // Create a decision node for the if statement
                var conditionNode = new DiagramNode
                {
                    Label = "If " + GetConditionText(ifStatement.Condition),
                    Type = NodeType.Decision,
                    X = xPos,
                    Y = yOffset,
                    Width = 100,
                    Height = 100,
                    MicroserviceName = sourceNode.MicroserviceName
                };
                
                nodes.Add(conditionNode);
                
                // Connect source to condition
                connections.Add(new DiagramConnection
                {
                    SourceNodeId = lastNode.Id,
                    TargetNodeId = conditionNode.Id
                });
                
                lastNode = conditionNode;
                
                // Check if the statement contains a file check or null check
                bool isFileNullCheck = ifStatement.Condition.ToString().Contains("file") && 
                    (ifStatement.Condition.ToString().Contains("null") || 
                     ifStatement.Condition.ToString().Contains("Length") || 
                     ifStatement.Condition.ToString().Contains("==") ||
                     ifStatement.Condition.ToString().Contains("!="));
                     
                if (isFileNullCheck)
                {
                    Console.WriteLine("Found file null check condition");
                    
                    // Directly add a return node for these specific conditions
                    var returnNode = new DiagramNode
                    {
                        Label = "Return (Early Exit)",
                        Type = NodeType.Process,
                        X = xPos + 150,
                        Y = yOffset + 150,
                        MicroserviceName = sourceNode.MicroserviceName
                    };
                    
                    nodes.Add(returnNode);
                    
                    connections.Add(new DiagramConnection
                    {
                        SourceNodeId = conditionNode.Id,
                        TargetNodeId = returnNode.Id,
                        Label = "true"
                    });
                    
                    // Continue to the next statement after the if
                    lastNode = conditionNode;
                    yOffset += 200;
                    continue;
                }
                
                // Check if the true branch contains a return statement or only logging
                bool hasReturn = false;
                bool hasImportantOperations = false;
                
                if (ifStatement.Statement is BlockSyntax trueBlock)
                {
                    // Check if the block contains a return statement
                    hasReturn = trueBlock.Statements.Any(s => s is ReturnStatementSyntax);
                    
                    // Check if the block contains any operations other than logging
                    foreach (var blockStatement in trueBlock.Statements)
                    {
                        // If this statement is a return, mark hasReturn as true
                        if (blockStatement is ReturnStatementSyntax)
                        {
                            hasReturn = true;
                            continue;
                        }
                        
                        var invocations = blockStatement.DescendantNodes().OfType<InvocationExpressionSyntax>();
                        foreach (var invocation in invocations)
                        {
                            var invokedMethod = invocation.Expression.ToString();
                            
                            // Non-logging methods are considered important operations
                            if (!invokedMethod.Contains("Observer.LogData") && 
                                !invokedMethod.Contains("Observer.NewTrace") && 
                                !invokedMethod.Contains("Activity") && 
                                !invokedMethod.Contains("MethodBase"))
                            {
                                hasImportantOperations = true;
                                break;
                            }
                        }
                        
                        if (hasImportantOperations)
                            break;
                    }
                }
                else if (ifStatement.Statement is ReturnStatementSyntax)
                {
                    // Direct return statement without a block
                    hasReturn = true;
                }
                
                // If the true branch contains a return statement
                if (hasReturn)
                {
                    var returnNode = new DiagramNode
                    {
                        Label = "Return (Early Exit)",
                        Type = NodeType.Process,
                        X = xPos + 150,
                        Y = yOffset + 150,
                        MicroserviceName = sourceNode.MicroserviceName
                    };
                    
                    nodes.Add(returnNode);
                    
                    connections.Add(new DiagramConnection
                    {
                        SourceNodeId = conditionNode.Id,
                        TargetNodeId = returnNode.Id,
                        Label = "true"
                    });
                    
                    // If there are no important operations in this branch, we can skip further processing
                    if (!hasImportantOperations)
                    {
                        // Continue to the next statement after the if
                        lastNode = conditionNode;
                        yOffset += 200;
                        continue;
                    }
                }
                
                // Analyze true branch
                var trueBranchNode = new DiagramNode
                {
                    Label = "If branch",
                    Type = NodeType.Process,
                    X = xPos + 150,
                    Y = yOffset,
                    MicroserviceName = sourceNode.MicroserviceName
                };
                
                nodes.Add(trueBranchNode);
                
                connections.Add(new DiagramConnection
                {
                    SourceNodeId = conditionNode.Id,
                    TargetNodeId = trueBranchNode.Id,
                    Label = "true"
                });
                
                // Process statements in the true branch
                DiagramNode lastTrueNode = trueBranchNode;
                int trueBranchYOffset = yOffset + 100;
                
                // Process each statement in if block
                if (ifStatement.Statement is BlockSyntax trueBlockStmts)
                {
                    foreach (var trueStatement in trueBlockStmts.Statements)
                    {
                        // Skip return statements as they are handled separately
                        if (trueStatement is ReturnStatementSyntax)
                            continue;
                            
                        var invocations = trueStatement.DescendantNodes().OfType<InvocationExpressionSyntax>();
                        
                        // Skip pure logging statements
                        bool onlyContainsLogging = true;
                        bool hasAnyInvocation = false;
                        
                        foreach (var invocation in invocations)
                        {
                            hasAnyInvocation = true;
                            var invokedMethod = invocation.Expression.ToString();
                            
                            if (!invokedMethod.Contains("Observer.LogData") && 
                                !invokedMethod.Contains("Observer.NewTrace") && 
                                !invokedMethod.Contains("Activity") && 
                                !invokedMethod.Contains("MethodBase"))
                            {
                                onlyContainsLogging = false;
                                break;
                            }
                        }
                        
                        // Skip if it only contains logging
                        if (hasAnyInvocation && onlyContainsLogging)
                            continue;
                            
                        // Process important invocations
                        foreach (var invocation in invocations)
                        {
                            var invokedMethod = invocation.Expression.ToString();
                            
                            // Skip logging calls
                            if (invokedMethod.Contains("Observer.LogData") || 
                                invokedMethod.Contains("Observer.NewTrace") || 
                                invokedMethod.Contains("Activity") || 
                                invokedMethod.Contains("MethodBase"))
                            {
                                continue;
                            }
                            
                            // Process the method invocation
                            NodeType nodeType = NodeType.Process;
                            string endpointUrl = string.Empty;
                            string httpMethod = string.Empty;
                            string targetMicroservice = sourceNode.MicroserviceName;
                            
                            // Extract method parameters for inclusion in the label
                            string parametersText = string.Empty;
                            if (invocation.ArgumentList != null && invocation.ArgumentList.Arguments.Count > 0)
                            {
                                var parameters = invocation.ArgumentList.Arguments
                                    .Select(arg => arg.Expression.ToString().Trim('"'))
                                    .ToList();
                                parametersText = string.Join(", ", parameters);
                            }
                            
                            // Check for special operation types
                            if (IsDatabaseOperation(invocation))
                            {
                                nodeType = NodeType.DatabaseOperation;
                                targetMicroservice = "Database";
                            }
                            else if (IsHttpClientCall(invocation, out endpointUrl, out httpMethod))
                            {
                                nodeType = NodeType.ApiCall;
                                targetMicroservice = DetermineTargetMicroservice(endpointUrl);
                            }
                            else if (IsMessagePublishingCall(invocation))
                            {
                                nodeType = NodeType.EventPublish;
                            }
                            
                            var methodNode = new DiagramNode
                            {
                                Label = GetMethodLabel(invokedMethod, parametersText, nodeType),
                                Type = nodeType,
                                X = xPos + 150,  // Offset to show in the true branch
                                Y = trueBranchYOffset,
                                OriginalMethodName = invokedMethod,
                                MicroserviceName = targetMicroservice,
                                EndpointUrl = endpointUrl,
                                HttpMethod = httpMethod
                            };
                            
                            nodes.Add(methodNode);
                            
                            connections.Add(new DiagramConnection
                            {
                                SourceNodeId = lastTrueNode.Id,
                                TargetNodeId = methodNode.Id
                            });
                            
                            lastTrueNode = methodNode;
                            trueBranchYOffset += 100;
                        }
                    }
                }
                
                // Analyze false branch if it exists
                if (ifStatement.Else != null)
                {
                    var falseBranchNode = new DiagramNode
                    {
                        Label = "Else branch",
                        Type = NodeType.Process,
                        X = xPos,
                        Y = yOffset + 100,
                        MicroserviceName = sourceNode.MicroserviceName
                    };
                    
                    nodes.Add(falseBranchNode);
                    
                    connections.Add(new DiagramConnection
                    {
                        SourceNodeId = conditionNode.Id,
                        TargetNodeId = falseBranchNode.Id,
                        Label = "false"
                    });
                    
                    // Process statements in the false branch
                    DiagramNode lastFalseNode = falseBranchNode;
                    int falseBranchYOffset = yOffset + 200;
                    
                    if (ifStatement.Else.Statement is BlockSyntax falseBlock)
                    {
                        // Process each statement in the false branch block
                        foreach (var falseStatement in falseBlock.Statements)
                        {
                            var invocations = falseStatement.DescendantNodes().OfType<InvocationExpressionSyntax>();
                            foreach (var invocation in invocations)
                            {
                                var invokedMethod = invocation.Expression.ToString();
                                
                                // Skip irrelevant invocations
                                if (invokedMethod.Contains("Observer.LogData") || 
                                    invokedMethod.Contains("Observer.NewTrace") || 
                                    invokedMethod.Contains("Activity") || 
                                    invokedMethod.Contains("MethodBase"))
                                {
                                    continue;
                                }
                                
                                // Process the method invocation (similar to the main loop)
                                NodeType nodeType = NodeType.Process;
                                string endpointUrl = string.Empty;
                                string httpMethod = string.Empty;
                                string targetMicroservice = sourceNode.MicroserviceName;
                                string calledClassName = string.Empty;
                                
                                // Extract method parameters for inclusion in the label
                                string parametersText = string.Empty;
                                if (invocation.ArgumentList != null && invocation.ArgumentList.Arguments.Count > 0)
                                {
                                    var parameters = invocation.ArgumentList.Arguments
                                        .Select(arg => arg.Expression.ToString().Trim('"'))
                                        .ToList();
                                    parametersText = string.Join(", ", parameters);
                                }
                                
                                // Check for repository operations and other special types
                                if (IsDatabaseOperation(invocation))
                                {
                                    nodeType = NodeType.DatabaseOperation;
                                    targetMicroservice = "Database";
                                }
                                else if (IsHttpClientCall(invocation, out endpointUrl, out httpMethod))
                                {
                                    nodeType = NodeType.ApiCall;
                                    targetMicroservice = DetermineTargetMicroservice(endpointUrl);
                                }
                                else if (IsMessagePublishingCall(invocation))
                                {
                                    nodeType = NodeType.EventPublish;
                                }
                                
                                var methodNode = new DiagramNode
                                {
                                    Label = GetMethodLabel(invokedMethod, parametersText, nodeType),
                                    Type = nodeType,
                                    X = xPos,  // Align under the false branch
                                    Y = falseBranchYOffset,
                                    OriginalMethodName = invokedMethod,
                                    MicroserviceName = targetMicroservice,
                                    EndpointUrl = endpointUrl,
                                    HttpMethod = httpMethod
                                };
                                
                                nodes.Add(methodNode);
                                
                                connections.Add(new DiagramConnection
                                {
                                    SourceNodeId = lastFalseNode.Id,
                                    TargetNodeId = methodNode.Id
                                });
                                
                                lastFalseNode = methodNode;
                                falseBranchYOffset += 100;
                            }
                        }
                    }
                    
                    // Update last node to the last one processed in either branch
                    // Choose the one with the highest Y position
                    if (falseBranchYOffset > trueBranchYOffset)
                    {
                        lastNode = lastFalseNode;
                        yOffset = falseBranchYOffset;
                    }
                    else
                    {
                        lastNode = lastTrueNode;
                        yOffset = trueBranchYOffset;
                    }
                }
                else
                {
                    // If no else branch, update lastNode to the last node in the true branch
                    lastNode = lastTrueNode;
                    yOffset = trueBranchYOffset;
                }
            }
            else
            {
                // Check for method invocations within the statement
                var invocations = statement.DescendantNodes().OfType<InvocationExpressionSyntax>();
                
                // Skip statements that contain ONLY logging calls and no other important operations
                bool onlyContainsLogging = true;
                bool hasAnyInvocation = false;
                
                foreach (var invocation in invocations)
                {
                    hasAnyInvocation = true;
                    var invokedMethod = invocation.Expression.ToString();
                    
                    if (!invokedMethod.Contains("Observer.LogData") && 
                        !invokedMethod.Contains("Observer.NewTrace") && 
                        !invokedMethod.Contains("Activity") && 
                        !invokedMethod.Contains("MethodBase"))
                    {
                        onlyContainsLogging = false;
                        break;
                    }
                }
                
                // Skip if it only contains logging and no other invocations or expressions
                if (hasAnyInvocation && onlyContainsLogging && 
                    !(statement is IfStatementSyntax) && 
                    !(statement is ReturnStatementSyntax) &&
                    !(statement is ThrowStatementSyntax))
                {
                    continue;
                }
                
                // Now process each invocation in the statement normally
                foreach (var invocation in invocations)
                {
                    var invokedMethod = invocation.Expression.ToString();
                    
                    // Skip Observer.LogData, Observer.NewTrace, Activity, or MethodBase invocations
                    if (invokedMethod.Contains("Observer.LogData") || 
                        invokedMethod.Contains("Observer.NewTrace") || 
                        invokedMethod.Contains("Activity") || 
                        invokedMethod.Contains("MethodBase"))
                    {
                        continue;
                    }
                    
                    NodeType nodeType = NodeType.Process;
                    string endpointUrl = string.Empty;
                    string httpMethod = string.Empty;
                    string targetMicroservice = sourceNode.MicroserviceName;

                    // The class part of the method call (before the dot) - repository name
                    string calledClassName = string.Empty;
                    
                    // Extract full class name for methods like _userRepository.GetAsync
                    if (invokedMethod.Contains('.'))
                    {
                        int dotIndex = invokedMethod.LastIndexOf('.');
                        if (dotIndex > 0)
                        {
                            calledClassName = invokedMethod.Substring(0, dotIndex);
                            
                            // Remove underscores from variable names (like _userRepository)
                            if (calledClassName.StartsWith("_"))
                            {
                                calledClassName = calledClassName.Substring(1);
                            }
                            
                            // Try to expand abbreviations or partial names to full names
                            if (calledClassName.EndsWith("Repo") && !calledClassName.EndsWith("Repository"))
                            {
                                calledClassName = calledClassName.Replace("Repo", "Repository");
                            }
                        }
                    }

                    // Extract method parameters for inclusion in the label
                    string parametersText = string.Empty;
                    if (invocation.ArgumentList != null && invocation.ArgumentList.Arguments.Count > 0)
                    {
                        var parameters = invocation.ArgumentList.Arguments
                            .Select(arg => arg.Expression.ToString().Trim('"'))
                            .ToList();
                        parametersText = string.Join(", ", parameters);
                    }

                    // Check for repository operations first
                    if (calledClassName.Contains("Repository") ||
                        (invokedMethod.EndsWith("Async") && 
                         (invokedMethod.Contains("Get") || 
                          invokedMethod.Contains("Save") ||
                          invokedMethod.Contains("Find") ||
                          invokedMethod.Contains("Update") ||
                          invokedMethod.Contains("Delete") ||
                          invokedMethod.Contains("Insert"))))
                    {
                        nodeType = NodeType.DatabaseOperation;
                        targetMicroservice = "Database";
                    }
                    // Check for HTTP client calls (common patterns in microservices)
                    else if (IsHttpClientCall(invocation, out endpointUrl, out httpMethod))
                    {
                        nodeType = NodeType.ApiCall;
                        targetMicroservice = DetermineTargetMicroservice(endpointUrl);
                    }
                    // Check for message publishing
                    else if (IsMessagePublishingCall(invocation))
                    {
                        nodeType = NodeType.EventPublish;
                    }
                    // Check for regular database operations
                    else if (IsDatabaseOperation(invocation))
                    {
                        nodeType = NodeType.DatabaseOperation;
                        targetMicroservice = "Database";
                    }
                    
                    var methodNode = new DiagramNode
                    {
                        // Include parameters in the label if available
                        Label = GetMethodLabel(invokedMethod, parametersText, nodeType),
                        Type = nodeType,
                        X = xPos,
                        Y = yOffset,
                        OriginalMethodName = invokedMethod,
                        MicroserviceName = targetMicroservice,
                        EndpointUrl = endpointUrl,
                        HttpMethod = httpMethod
                    };
                    
                    nodes.Add(methodNode);
                    
                    connections.Add(new DiagramConnection
                    {
                        SourceNodeId = lastNode.Id,
                        TargetNodeId = methodNode.Id,
                        Label = nodeType == NodeType.ApiCall ? httpMethod : string.Empty
                    });
                    
                    lastNode = methodNode;
                }
            }
            
            yOffset += 100;
        }
        
        // Add end node
        var endNode = new DiagramNode
        {
            Label = "End",
            Type = NodeType.End,
            X = xPos,
            Y = yOffset,
            MicroserviceName = sourceNode.MicroserviceName
        };
        
        nodes.Add(endNode);
        
        connections.Add(new DiagramConnection
        {
            SourceNodeId = lastNode.Id,
            TargetNodeId = endNode.Id
        });
    }

    private bool IsHttpClientCall(InvocationExpressionSyntax invocation, out string endpointUrl, out string httpMethod)
    {
        endpointUrl = string.Empty;
        httpMethod = string.Empty;
        
        string methodName = invocation.Expression.ToString();
        
        // Check common HTTP client method patterns
        if (methodName.Contains("GetAsync"))
        {
            httpMethod = "GET";
        }
        else if (methodName.Contains("PostAsync"))
        {
            httpMethod = "POST";
        }
        else if (methodName.Contains("PutAsync"))
        {
            httpMethod = "PUT";
        }
        else if (methodName.Contains("DeleteAsync"))
        {
            httpMethod = "DELETE";
        }
        else if (methodName.Contains("PatchAsync"))
        {
            httpMethod = "PATCH";
        }
        else
        {
            return false;
        }
        
        // Try to extract URL from arguments
        if (invocation.ArgumentList?.Arguments.Count > 0)
        {
            var firstArg = invocation.ArgumentList.Arguments[0].Expression.ToString();
            // Remove quotes if present
            endpointUrl = firstArg.Trim('"');
        }
        
        return true;
    }

    private bool IsMessagePublishingCall(InvocationExpressionSyntax invocation)
    {
        string methodName = invocation.Expression.ToString();
        
        // Check common message broker publishing patterns
        return methodName.Contains("Publish") || 
               methodName.Contains("SendMessage") || 
               methodName.Contains("Enqueue") ||
               methodName.Contains("SendAsync");
    }

    private bool IsDatabaseOperation(InvocationExpressionSyntax invocation)
    {
        string methodName = invocation.Expression.ToString();
        
        // Check common database operation patterns and repository patterns
        return methodName.Contains("SaveChanges") || 
               methodName.Contains("ExecuteQuery") || 
               methodName.Contains("ExecuteNonQuery") ||
               methodName.Contains("ExecuteReader") ||
               methodName.Contains("ExecuteScalar") ||
               methodName.EndsWith("Add") ||
               methodName.EndsWith("Update") ||
               methodName.EndsWith("Delete") ||
               methodName.EndsWith("Remove") ||
               methodName.Contains("Repository") ||
               // Add specific repository method patterns
               methodName.EndsWith("SaveAsync") ||
               methodName.EndsWith("GetAsync") ||
               methodName.EndsWith("FindAsync") ||
               methodName.EndsWith("Query") ||
               methodName.EndsWith("GetById") ||
               methodName.EndsWith("GetAll");
    }

    private string DetermineTargetMicroservice(string url)
    {
        // Simple heuristic to determine target microservice from URL
        // In a real scenario, this would be more sophisticated
        if (string.IsNullOrEmpty(url))
        {
            return "Unknown";
        }
        
        // Extract service name from URL segments
        var segments = url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (segment.EndsWith("service", StringComparison.OrdinalIgnoreCase) ||
                segment.EndsWith("api", StringComparison.OrdinalIgnoreCase))
            {
                return segment;
            }
        }
        
        return "External Service";
    }

    private string GetMethodLabel(string methodName, string parametersText, NodeType nodeType)
    {
        // Don't truncate method names to ensure full names are shown
        string label = methodName;
        
        // Add parameter information for database operations and other nodes where available
        if (!string.IsNullOrEmpty(parametersText) && nodeType == NodeType.DatabaseOperation)
        {
            // Extract just the method name part (before any existing parameters in the string)
            int paramStart = methodName.IndexOf('(');
            if (paramStart > 0)
            {
                label = methodName.Substring(0, paramStart);
            }
            
            // Add the extracted parameters
            label += $" ({parametersText})";
        }
        
        // Add prefix based on node type (except for database operations)
        return nodeType switch
        {
            NodeType.ApiCall => $"API: {label}",
            NodeType.DatabaseOperation => label, // No prefix for database operations
            NodeType.EventPublish => $"Publish: {label}",
            NodeType.EventSubscribe => $"Subscribe: {label}",
            _ => label
        };
    }

    private string GetConditionText(ExpressionSyntax condition)
    {
        string conditionText = condition.ToString();
        
        // Format based on condition type for better readability
        if (condition is BinaryExpressionSyntax binaryExpression)
        {
            // For binary expressions like a == b, a > b, etc.
            string op = binaryExpression.OperatorToken.ValueText;
            string left = binaryExpression.Left.ToString();
            string right = binaryExpression.Right.ToString();
            
            // Truncate long operands
            if (left.Length > 15) left = left.Substring(0, 12) + "...";
            if (right.Length > 15) right = right.Substring(0, 12) + "...";
            
            return $"{left} {op} {right}";
        }
        else if (condition is InvocationExpressionSyntax invocation)
        {
            // For method calls like IsValid()
            return invocation.Expression.ToString();
        }
        else if (condition is PrefixUnaryExpressionSyntax prefixUnary && 
                 prefixUnary.OperatorToken.ValueText == "!")
        {
            // For negations like !isValid
            string operand = prefixUnary.Operand.ToString();
            if (operand.Length > 15) operand = operand.Substring(0, 12) + "...";
            return $"!{operand}";
        }
        
        // Default case - truncate if too long
        return conditionText.Length > 30 
            ? conditionText.Substring(0, 27) + "..." 
            : conditionText;
    }
} 