using MicroserviceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace MicroserviceAnalyzer.Services;

public class DiagramGenerator
{
    private readonly Dictionary<string, string> _microserviceColors = new Dictionary<string, string>();
    private readonly Random _random = new Random();
    private const string DATABASE_SWIMLANE_ID = "database_swimlane";

    public void GenerateDiagram(Diagram diagram, string outputPath)
    {
        // Assign colors to different microservices for better visualization
        AssignMicroserviceColors(diagram.Nodes);

        // Use StringBuilder and write raw XML to exactly match demo.drawio.xml format
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<mxfile host=\"Electron\" agent=\"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/26.2.15 Chrome/134.0.6998.205 Electron/35.2.1 Safari/537.36\" version=\"26.2.15\">");
        sb.AppendLine($"  <diagram name=\"{diagram.Name}\" id=\"{Guid.NewGuid().ToString("N").Substring(0, 24)}\">");
        sb.AppendLine("    <mxGraphModel dx=\"1106\" dy=\"783\" grid=\"1\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"827\" pageHeight=\"1169\" math=\"0\" shadow=\"0\">");
        sb.AppendLine("      <root>");
        sb.AppendLine("        <mxCell id=\"0\" />");
        sb.AppendLine("        <mxCell id=\"1\" parent=\"0\" />");

        // Group nodes by microservice
        var microserviceGroups = diagram.Nodes
            .GroupBy(n => n.MicroserviceName)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Extract database operations for a dedicated swimlane
        var databaseNodes = diagram.Nodes
            .Where(n => n.Type == NodeType.DatabaseOperation)
            .ToList();

        // Create vertical swimlane containers for each microservice
        int swimlaneY = 40;
        int swimlaneWidth = 250;
        int swimlaneSpacing = 20; // Space between swimlanes
        int initialX = 40; // Starting X position
        var swimlaneIds = new Dictionary<string, string>();
        var swimlanePositions = new Dictionary<string, int>(); // To track x positions
        
        // Calculate total height needed - ensure it's large enough for all nodes with proper spacing
        int maxNodesPerMicroservice = microserviceGroups.Values.Count > 0 
            ? microserviceGroups.Values.Max(list => list.Count) 
            : 5;
        // Add height for each node (100px per node) plus padding at top and bottom
        int totalHeight = Math.Max(maxNodesPerMicroservice * 100 + 300, 1100); 
        
        // Get all microservices excluding Database (which will be positioned separately)
        var microservices = microserviceGroups.Keys
            .Where(k => !string.IsNullOrEmpty(k) && k != "Unknown" && k != "Database")
            .ToList();
        
        // Count microservices (excluding Database)
        int microserviceCount = microservices.Count;
        
        // Create main microservice swimlane first 
        string serviceSwimlaneId = "project_swimlane_id";
        
        // Get the root project name (from the directory structure or class name)
        string rootProjectName = Path.GetFileName(Path.GetDirectoryName(outputPath));
        if (string.IsNullOrWhiteSpace(rootProjectName))
        {
            // Fallback if path doesn't have expected structure
            rootProjectName = diagram.SourceClassName ?? "Microservice Project";
        }
        
        // Calculate positions for each swimlane
        int currentX = initialX;
        
        // Create main swimlane 
        sb.AppendLine($"        <mxCell id=\"{serviceSwimlaneId}\" value=\"{rootProjectName}\" style=\"swimlane;whiteSpace=wrap;html=1;fillColor=#f5f5f5;strokeColor=#666666;fontColor=#333333;startSize=30;\" parent=\"1\" vertex=\"1\">");
        sb.AppendLine($"          <mxGeometry x=\"{currentX}\" y=\"{swimlaneY - 100}\" width=\"{swimlaneWidth}\" height=\"{totalHeight}\" as=\"geometry\" />");
        sb.AppendLine("        </mxCell>");
        
        // Store the position of the main swimlane
        swimlanePositions[rootProjectName] = currentX;
        currentX += swimlaneWidth + swimlaneSpacing;
        
        // Create microservice swimlanes
        int swimlaneIndex = 1;
        foreach (var microservice in microserviceGroups.Keys)
        {
            if (string.IsNullOrEmpty(microservice) || microservice == "Unknown") continue;
            
            // Create a unique swimlane ID
            string swimlaneId = microservice == "Database" ? 
                DATABASE_SWIMLANE_ID : 
                $"swimlane_{Guid.NewGuid().ToString("N").Substring(0, 8)}";
                
            swimlaneIds[microservice] = swimlaneId;
            
            // Skip the main service as we've already created it
            if (microservice == rootProjectName) continue;
            
            // Update node style for this microservice
            foreach (var node in microserviceGroups[microservice])
            {
                // Use appropriate style for all nodes
                node.Style = $"{GetStyleForNodeType(node.Type)};fillColor={_microserviceColors[microservice]};";
            }
            
            // Create the swimlane
            string fillColor = microservice == "Database" ? "#dae8fc" : "#f5f5f5";
            string strokeColor = microservice == "Database" ? "#6c8ebf" : "#666666";
            
            sb.AppendLine($"        <mxCell id=\"{swimlaneId}\" value=\"{microservice}\" style=\"swimlane;whiteSpace=wrap;html=1;fillColor={fillColor};strokeColor={strokeColor};fontColor=#333333;startSize=30;\" parent=\"1\" vertex=\"1\">");
            sb.AppendLine($"          <mxGeometry x=\"{currentX}\" y=\"{swimlaneY - 100}\" width=\"{swimlaneWidth}\" height=\"{totalHeight}\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Store the position of this swimlane
            swimlanePositions[microservice] = currentX;
            currentX += swimlaneWidth + swimlaneSpacing;
            swimlaneIndex++;
        }
        
        // Update style for database nodes
        foreach (var node in databaseNodes)
        {
            // Use database-specific style
            node.Style = $"{GetStyleForNodeType(node.Type)}";
            node.MicroserviceName = "Database"; // Ensure all DB nodes have "Database" as microservice
        }
        
        // First, organize nodes by swimlane for better assignment
        var nodesBySwimlaneMicroservice = new Dictionary<string, List<DiagramNode>>();

        // Pre-assign all nodes to their swimlanes
        foreach (var node in diagram.Nodes)
        {
            // Determine which swimlane this node belongs to - critical for proper assignment
            string microservice;
            
            if (node.Type == NodeType.DatabaseOperation)
            {
                // Database operations always go in the Database swimlane
                microservice = "Database";
            }
            else 
            {
                // Non-database operations stay in their original microservice swimlane
                microservice = string.IsNullOrEmpty(node.MicroserviceName) ? rootProjectName : node.MicroserviceName;
                
                // Override: If this is the source class, always use the main swimlane
                if (node.OriginalClassName == diagram.SourceClassName)
                {
                    microservice = rootProjectName;
                }
            }
            
            // Keep track of the resolved microservice name
            node.MicroserviceName = microservice;
            
            // Group nodes by swimlane
            if (!nodesBySwimlaneMicroservice.ContainsKey(microservice))
            {
                nodesBySwimlaneMicroservice[microservice] = new List<DiagramNode>();
            }
            nodesBySwimlaneMicroservice[microservice].Add(node);
        }

        // Now organize nodes vertically based on their connections
        // This is a simplified algorithm - in a real app, you might want more sophisticated layout
        var processedNodes = new HashSet<string>();
        
        // Start with the source node (the first one)
        if (diagram.Nodes.Any())
        {
            var startNode = diagram.Nodes.First();
            processedNodes.Add(startNode.Id);
            startNode.Y = swimlaneY + 60; // Start below swimlane header
            
            int currentY = swimlaneY + 160; // Start Y position for next nodes (after start node)
            
            // Process connected nodes in order of connections
            OrganizeConnectedNodes(startNode.Id, diagram, processedNodes, ref currentY);
        }
        
        // First, add all the connection edges - follow the demo.drawio.xml order (connections first, then cells)
        foreach (var connection in diagram.Connections)
        {
            var sourceNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            
            string style = connection.Style;
            if (sourceNode != null && targetNode != null)
            {
                if (targetNode.Type == NodeType.DatabaseOperation)
                {
                    // Special style for database connections - use dotted line with arrow
                    style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;strokeWidth=1.5;strokeColor=#0066CC;dashed=1;endArrow=block;endFill=1;";
                }
                else if (!string.IsNullOrEmpty(sourceNode.MicroserviceName) && 
                    !string.IsNullOrEmpty(targetNode.MicroserviceName) && 
                    sourceNode.MicroserviceName != targetNode.MicroserviceName)
                {
                    // Use a different style for cross-microservice connections
                    style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;dashed=1;dashPattern=1 4;strokeWidth=2;";
                }
            }
            
            string connId = Guid.NewGuid().ToString("N").Substring(0, 8);
            sb.AppendLine($"        <mxCell id=\"{connId}\" value=\"{connection.Label}\" style=\"{style}\" edge=\"1\" parent=\"1\" source=\"{connection.SourceNodeId}\" target=\"{connection.TargetNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
        }
        
        // Now add all nodes for each swimlane
        foreach (var microservicePair in nodesBySwimlaneMicroservice)
        {
            string microservice = microservicePair.Key;
            var nodes = microservicePair.Value;
            
            // Determine the parent swimlane ID
            string parentId;
            if (swimlaneIds.ContainsKey(microservice))
            {
                parentId = swimlaneIds[microservice];
            }
            else
            {
                parentId = serviceSwimlaneId; // Default to main swimlane
            }
            
            // Add all nodes for this swimlane
            foreach (var node in nodes)
            {
                // Adjust position to be relative to the swimlane
                node.X = (swimlaneWidth / 2) - (node.Width / 2);
                
                string style = string.IsNullOrEmpty(node.Style) 
                    ? GetStyleForNodeType(node.Type) 
                    : node.Style;
                    
                sb.AppendLine($"        <mxCell id=\"{node.Id}\" value=\"{node.Label}\" style=\"{style}\" vertex=\"1\" parent=\"{parentId}\">");
                sb.AppendLine($"          <mxGeometry x=\"{node.X}\" y=\"{node.Y}\" width=\"{node.Width}\" height=\"{node.Height}\" as=\"geometry\" />");
                sb.AppendLine("        </mxCell>");
            }
        }

        // Close the XML
        sb.AppendLine("      </root>");
        sb.AppendLine("    </mxGraphModel>");
        sb.AppendLine("  </diagram>");
        sb.AppendLine("</mxfile>");

        // Write the XML to file - handle any XML escaping
        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false)); // false = no BOM
        
        Console.WriteLine($"XML document saved with {diagram.Nodes.Count} nodes and {diagram.Connections.Count} connections.");
    }

    private void OrganizeConnectedNodes(string nodeId, Diagram diagram, HashSet<string> processedNodes, ref int currentY)
    {
        // Find all connections where this node is the source
        var connections = diagram.Connections
            .Where(c => c.SourceNodeId == nodeId)
            .ToList();
            
        foreach (var connection in connections)
        {
            if (!processedNodes.Contains(connection.TargetNodeId))
            {
                processedNodes.Add(connection.TargetNodeId);
                
                // Find the target node and update its Y position
                var targetNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
                if (targetNode != null)
                {
                    targetNode.Y = currentY;
                    currentY += 100;
                    
                    // Recursively process its connections
                    OrganizeConnectedNodes(targetNode.Id, diagram, processedNodes, ref currentY);
                }
            }
        }
    }

    private void AssignMicroserviceColors(List<DiagramNode> nodes)
    {
        // Get all unique microservice names
        var microservices = nodes
            .Select(n => n.MicroserviceName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Distinct()
            .ToList();

        // Assign a color to each microservice
        foreach (var microservice in microservices)
        {
            if (!_microserviceColors.ContainsKey(microservice))
            {
                _microserviceColors[microservice] = GenerateRandomLightColor();
            }
        }
    }

    private string GenerateRandomLightColor()
    {
        // Generate a light color (for better contrast with black text)
        int r = _random.Next(180, 240);
        int g = _random.Next(180, 240);
        int b = _random.Next(180, 240);
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private string GetStyleForNodeType(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Start => "strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;",
            NodeType.End => "strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;",
            NodeType.Process => "rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;",
            NodeType.Decision => "strokeWidth=2;html=1;shape=mxgraph.flowchart.decision;whiteSpace=wrap;",
            NodeType.DatabaseOperation => "strokeWidth=2;html=1;shape=mxgraph.flowchart.database;whiteSpace=wrap;",
            NodeType.ExternalService => "strokeWidth=2;html=1;shape=mxgraph.flowchart.predefined_process;whiteSpace=wrap;",
            NodeType.Controller => "strokeWidth=2;html=1;shape=mxgraph.flowchart.direct_data;whiteSpace=wrap;",
            NodeType.ApiCall => "strokeWidth=2;html=1;shape=mxgraph.flowchart.direct_data;whiteSpace=wrap;",
            NodeType.MessageQueue => "strokeWidth=2;html=1;shape=mxgraph.flowchart.parallel_mode;whiteSpace=wrap;",
            NodeType.EventPublish => "strokeWidth=2;html=1;shape=mxgraph.flowchart.paper_tape;whiteSpace=wrap;",
            NodeType.EventSubscribe => "strokeWidth=2;html=1;shape=mxgraph.flowchart.sequential_data;whiteSpace=wrap;",
            _ => "rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;"
        };
    }
} 