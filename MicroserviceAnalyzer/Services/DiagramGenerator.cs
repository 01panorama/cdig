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
            .Where(n => n.Type == NodeType.DatabaseOperation || n.MicroserviceName == "Database")
            .ToList();

        // Create vertical swimlane containers for each microservice
        int swimlaneY = 40;
        int swimlaneWidth = 250;
        int swimlaneSpacing = 20; // Space between swimlanes
        int initialX = 40; // Starting X position
        int nodeVerticalSpacing = 160; // Match the fixed spacing from ftp.fixed.xml (160px between nodes)
        var swimlaneIds = new Dictionary<string, string>();
        var swimlanePositions = new Dictionary<string, int>(); // To track x positions
        
        // Calculate the maximum diagram height needed based on node count with proper spacing
        int totalNodeCount = diagram.Nodes.Count;
        int totalHeight = Math.Max(totalNodeCount * 100 + 300, 1100); 
        
        // Get all microservices excluding Database (which will be positioned separately)
        var microservices = microserviceGroups.Keys
            .Where(k => !string.IsNullOrEmpty(k) && k != "Unknown" && k != "Database")
            .ToList();
        
        // Count microservices (excluding Database)
        int microserviceCount = microservices.Count;
        
        // Get the root project name (from the directory structure or class name)
        string rootProjectName = Path.GetFileName(Path.GetDirectoryName(outputPath) ?? string.Empty);
        if (string.IsNullOrWhiteSpace(rootProjectName))
        {
            // Fallback if path doesn't have expected structure
            rootProjectName = diagram.SourceClassName ?? "Microservice Project";
        }
        
        // Calculate positions for each swimlane
        int currentX = initialX;

        // Dictionary to store the Y positions for each microservice
        var microserviceYPositions = new Dictionary<string, int>();
        
        // Create swimlanes for each microservice
        foreach (var microservice in microservices)
        {
            string swimlaneId = $"swimlane_{microservice.Replace(" ", "_")}";
            string color = _microserviceColors.ContainsKey(microservice) 
                ? _microserviceColors[microservice] 
                : "#FFFFFF";
                
            // Add the swimlane container
            sb.AppendLine($"        <mxCell id=\"{swimlaneId}\" value=\"{microservice}\" style=\"swimlane;whiteSpace=wrap;html=1;startSize=40;fillColor=none;strokeColor={color};strokeWidth=2;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine($"          <mxGeometry x=\"{currentX}\" y=\"{swimlaneY}\" width=\"{swimlaneWidth}\" height=\"{totalHeight}\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Track the swimlane ID and position
            swimlaneIds[microservice] = swimlaneId;
            swimlanePositions[microservice] = currentX;
            // Initial Y position (60 for the header, then 60 for the first node spacing)
            microserviceYPositions[microservice] = swimlaneY + 60; 
            
            // Move to the next position
            currentX += swimlaneWidth + swimlaneSpacing;
        }
        
        // Add a special swimlane for database operations if needed
        if (databaseNodes.Count > 0)
        {
            sb.AppendLine($"        <mxCell id=\"{DATABASE_SWIMLANE_ID}\" value=\"Database\" style=\"swimlane;whiteSpace=wrap;html=1;startSize=40;fillColor=none;strokeColor=#0066CC;strokeWidth=2;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine($"          <mxGeometry x=\"{currentX}\" y=\"{swimlaneY}\" width=\"{swimlaneWidth}\" height=\"{totalHeight}\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Track the database swimlane
            swimlaneIds["Database"] = DATABASE_SWIMLANE_ID;
            swimlanePositions["Database"] = currentX;
            // Match the initial position from ftp.fixed.xml
            microserviceYPositions["Database"] = swimlaneY + 60;
        }
        
        // Dictionary to track nodes we've already processed by microservice
        var processedNodesByMicroservice = new Dictionary<string, HashSet<int>>();
        // Track node heights to maintain consistency across swimlanes
        var nodeHeights = new Dictionary<int, int>();
        // Track node widths to maintain consistency across swimlanes
        var nodeWidths = new Dictionary<int, int>();
        // Track Y positions for nodes to maintain consistent positions across swimlanes
        var nodeYPositions = new Dictionary<int, int>();
        // Track occupied Y positions for each swimlane to prevent overlaps
        var occupiedYPositions = new Dictionary<string, HashSet<int>>();
        foreach (var microservice in microservices.Concat(new[] { "Database" }))
        {
            processedNodesByMicroservice[microservice] = new HashSet<int>();
            occupiedYPositions[microservice] = new HashSet<int>();
        }

        // Create a list of all nodes ordered the way we want to process them
        var orderedNodes = new List<DiagramNode>();
        
        // Add initial nodes (start nodes) first
        var startNodes = diagram.Nodes.Where(n => n.Type == NodeType.Start).ToList();
        orderedNodes.AddRange(startNodes);
        
        // Add if statements (decision nodes) next to ensure they're included
        var decisionNodes = diagram.Nodes.Where(n => n.Type == NodeType.Decision).ToList();
        orderedNodes.AddRange(decisionNodes);
        
        // Then add all remaining nodes
        orderedNodes.AddRange(diagram.Nodes.Where(n => !orderedNodes.Contains(n)));
        
        // Pre-calculate vertical positions to match the fixed example
        // This will establish consistent Y-coordinates for all nodes
        Dictionary<int, int> preCalculatedYPositions = new Dictionary<int, int>();
        
        // Start with an established base Y-coordinate
        int baseY = swimlaneY + 60; // Starting Y coordinate (after swimlane header)
        
        // Get flow sequence by following connections
        HashSet<int> processed = new HashSet<int>();
        foreach (var startNode in startNodes)
        {
            preCalculatedYPositions[startNode.Id] = baseY;
            processed.Add(startNode.Id);
            baseY += nodeVerticalSpacing;
            
            // Follow the flow to determine exact positions for connected nodes
            CalculatePositionsForSequence(startNode.Id, baseY, diagram, processed, preCalculatedYPositions, nodeVerticalSpacing);
        }
        
        // Assign any remaining nodes that weren't part of the main flow
        foreach (var node in orderedNodes)
        {
            if (!preCalculatedYPositions.ContainsKey(node.Id))
            {
                preCalculatedYPositions[node.Id] = baseY;
                baseY += nodeVerticalSpacing;
            }
        }

        // Process connections first (as in original code)
        var nodePairsToAlign = new List<KeyValuePair<int, int>>();
        foreach (var connection in diagram.Connections)
        {
            var sourceNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var targetNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            
            // Create a direct mapping between connected nodes for Y-position alignment
            // This must be done before node positioning to ensure proper layout
            if (sourceNode != null && targetNode != null &&
                sourceNode.MicroserviceName != targetNode.MicroserviceName)
            {
                // Directly map paired node IDs for exact Y-position matching
                // This will ensure nodes are at exactly the same Y level across swimlanes
                var sourceTargetPair = new KeyValuePair<int, int>(sourceNode.Id, targetNode.Id);
                var targetSourcePair = new KeyValuePair<int, int>(targetNode.Id, sourceNode.Id);
                nodePairsToAlign.Add(sourceTargetPair);
                nodePairsToAlign.Add(targetSourcePair);
            }
            
            string style = connection.Style ?? "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;";
            if (sourceNode != null && targetNode != null)
            {
                if (targetNode.Type == NodeType.DatabaseOperation)
                {
                    // Special style for database connections - use dotted line with arrow
                    style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;strokeWidth=1.5;strokeColor=#0066CC;dashed=1;endArrow=block;endFill=1;";
                }
                else if (sourceNode.Type == NodeType.Decision)
                {
                    // Decision node connections should stand out more
                    string connectionLabel = connection.Label ?? "";
                    if (connectionLabel.ToUpper().Contains("TRUE") || connectionLabel.ToUpper().Contains("FALSE"))
                    {
                        // Special style for TRUE/FALSE branches
                        style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;strokeWidth=1.5;fontStyle=1;";
                    }
                }
                else if (!string.IsNullOrEmpty(sourceNode.MicroserviceName) && 
                    !string.IsNullOrEmpty(targetNode.MicroserviceName) && 
                    sourceNode.MicroserviceName != targetNode.MicroserviceName)
                {
                    // Use a different style for cross-microservice connections
                    style = "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;dashed=1;dashPattern=1 4;strokeWidth=2;";
                    
                    // Store target node's ID in sourceNode's cross-swimlane connections for later alignment
                    if (!nodeYPositions.ContainsKey(sourceNode.Id))
                    {
                        nodeYPositions[sourceNode.Id] = swimlaneY + 100; // Default position if not set yet
                    }
                    
                    // When we encounter a cross-swimlane connection, make sure to mark the target node's 
                    // Y position to match the source node - this will be processed when we create nodes
                    if (!nodeYPositions.ContainsKey(targetNode.Id))
                    {
                        nodeYPositions[targetNode.Id] = nodeYPositions[sourceNode.Id];
                    }
                }
            }
            
            string connId = Guid.NewGuid().ToString("N").Substring(0, 8);
            string sourceRef = $"cell{connection.SourceNodeId}";
            string targetRef = $"cell{connection.TargetNodeId}";
            string label = string.IsNullOrEmpty(connection.Label) ? "" : connection.Label;
            
            sb.AppendLine($"        <mxCell id=\"{connId}\" style=\"{style}\" edge=\"1\" parent=\"1\" source=\"{sourceRef}\" target=\"{targetRef}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            
            // Add label if present
            if (!string.IsNullOrEmpty(label))
            {
                sb.AppendLine($"          <mxCell id=\"{connId}_label\" value=\"{label}\" style=\"edgeLabel;html=1;align=center;verticalAlign=middle;resizable=0;points=[];fontStyle=1;\" vertex=\"1\" connectable=\"0\" parent=\"{connId}\">");
                sb.AppendLine("            <mxGeometry x=\"-0.3\" relative=\"1\" as=\"geometry\">");
                sb.AppendLine("              <mxPoint as=\"offset\" />");
                sb.AppendLine("            </mxGeometry>");
                sb.AppendLine("          </mxCell>");
            }
            
            sb.AppendLine("        </mxCell>");
        }

        // Now process all nodes in our ordered list
        foreach (var node in orderedNodes)
        {
            // Determine which microservice this node belongs to
            string microservice = !string.IsNullOrEmpty(node.MicroserviceName) ? node.MicroserviceName : "Unknown";
            
            // Special case for database operations
            if (node.Type == NodeType.DatabaseOperation)
            {
                microservice = "Database";
            }
            
            // Skip if microservice isn't in our swimlanes
            if (!swimlaneIds.ContainsKey(microservice))
            {
                continue;
            }
            
            // Get the parent swimlane ID
            string parentId = swimlaneIds[microservice];
            
            // Calculate X position (centered in swimlane)
            int xPosition = swimlanePositions[microservice] + (swimlaneWidth / 2) - (node.Width / 2);
            
            // Check if this node has incoming connections from another swimlane
            bool hasIncomingCrossSwimlane = false;
            int sourceYPosition = 0;
            
            // First check for direct alignment matches from our paired nodes list
            var alignmentPairs = nodePairsToAlign.Where(p => p.Key == node.Id).ToList();
            if (alignmentPairs.Any())
            {
                foreach (var pair in alignmentPairs)
                {
                    var pairedNode = diagram.Nodes.FirstOrDefault(n => n.Id == pair.Value);
                    if (pairedNode != null && nodeYPositions.ContainsKey(pairedNode.Id))
                    {
                        // This is a paired node, use exact same Y position
                        hasIncomingCrossSwimlane = true;
                        sourceYPosition = nodeYPositions[pairedNode.Id];
                        break;
                    }
                }
            }
            
            // If no direct alignment, check for regular connections
            if (!hasIncomingCrossSwimlane)
            {
                foreach (var connection in diagram.Connections.Where(c => c.TargetNodeId == node.Id))
                {
                    var sourceNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
                    if (sourceNode != null && sourceNode.MicroserviceName != node.MicroserviceName)
                    {
                        // This node has a connection from another swimlane
                        hasIncomingCrossSwimlane = true;
                        
                        // Check if we have a stored Y position for the source node
                        if (nodeYPositions.ContainsKey(sourceNode.Id))
                        {
                            sourceYPosition = nodeYPositions[sourceNode.Id];
                        }
                        break;
                    }
                }
            }
            
            // Get next Y position for this microservice
            int yPosition;
            
            // Use precalculated position if available (highest priority)
            if (preCalculatedYPositions.ContainsKey(node.Id))
            {
                yPosition = preCalculatedYPositions[node.Id];
            }
            // For cross-swimlane connections, use paired node position
            else if (hasIncomingCrossSwimlane && sourceYPosition > 0)
            {
                // Use the same Y position as the source node in the other swimlane
                // but check if this position is already occupied in this swimlane
                yPosition = sourceYPosition;
                
                // Find a non-conflicting Y position if the current one is occupied
                if (occupiedYPositions.ContainsKey(microservice) && IsYPositionOccupied(occupiedYPositions[microservice], yPosition, 40))
                {
                    // Find the next available Y position in this swimlane
                    yPosition = FindNextAvailableYPosition(occupiedYPositions[microservice], microserviceYPositions[microservice], nodeVerticalSpacing);
                }
            }
            else if (nodeYPositions.ContainsKey(node.Id))
            {
                // Use previously stored Y position for this node
                yPosition = nodeYPositions[node.Id];
                
                // Ensure it doesn't conflict with occupied positions
                if (occupiedYPositions.ContainsKey(microservice) && IsYPositionOccupied(occupiedYPositions[microservice], yPosition, 40))
                {
                    yPosition = FindNextAvailableYPosition(occupiedYPositions[microservice], microserviceYPositions[microservice], nodeVerticalSpacing);
                }
            }
            else
            {
                // Use the next available Y position in this microservice
                yPosition = microserviceYPositions.ContainsKey(microservice) 
                    ? microserviceYPositions[microservice] 
                    : swimlaneY + 60;
                
                // Ensure it doesn't conflict with occupied positions
                if (occupiedYPositions.ContainsKey(microservice) && IsYPositionOccupied(occupiedYPositions[microservice], yPosition, 40))
                {
                    yPosition = FindNextAvailableYPosition(occupiedYPositions[microservice], microserviceYPositions[microservice], nodeVerticalSpacing);
                }
            }
            
            // Update Y position for the next node in this microservice
            microserviceYPositions[microservice] = yPosition + nodeVerticalSpacing; // Match the 160px spacing in ftp.fixed.xml
            
            // Store the Y position for this node
            nodeYPositions[node.Id] = yPosition;
            
            // Width and height
            int width = node.Width;
            int height = node.Height;
            
            // For decision nodes (if statements), make them larger to accommodate text
            if (node.Type == NodeType.Decision)
            {
                width = Math.Max(width, 200);  // Make wider for condition text
                height = Math.Max(height, 120); // Make taller
            }
            // Check if we have dimensions for this node already (from another swimlane)
            else
            {
                // Maintain consistent height across swimlanes
                if (nodeHeights.ContainsKey(node.Id))
                {
                    height = nodeHeights[node.Id];
                }
                
                // Maintain consistent width across swimlanes
                if (nodeWidths.ContainsKey(node.Id))
                {
                    width = nodeWidths[node.Id];
                }
            }
            
            // Store the dimensions for this node to use consistently across swimlanes
            nodeHeights[node.Id] = height;
            nodeWidths[node.Id] = width;
            
            // Mark this Y position as occupied for this swimlane
            if (occupiedYPositions.ContainsKey(microservice))
            {
                // Mark the region as occupied with some padding (using the node height)
                int nodeHeight = height + 10; // Reduce padding for more compact layout
                for (int y = yPosition - nodeHeight/2; y <= yPosition + nodeHeight/2; y++)
                {
                    occupiedYPositions[microservice].Add(y);
                }
            }
            
            // Get the style based on node type
            string style = GetStyleForNodeType(node.Type);
            
            // Add stroke color based on microservice if available
            if (!string.IsNullOrEmpty(node.MicroserviceName) && _microserviceColors.ContainsKey(node.MicroserviceName))
            {
                style += $"strokeColor={_microserviceColors[node.MicroserviceName]};";
            }
            
            // Add special handling for node types
            string nodeLabel = node.Label;
            if (node.Type == NodeType.Decision)
            {
                // Make condition text stand out more
                nodeLabel = "<b>" + nodeLabel + "</b>";
            }
            
            // Add the cell node
            sb.AppendLine($"        <mxCell id=\"cell{node.Id}\" value=\"{EscapeXml(nodeLabel)}\" style=\"{style}\" parent=\"{parentId}\" vertex=\"1\">");

            // Important: Position relative to the parent swimlane
            // X position centers the node within the swimlane
            int relativeX = (swimlaneWidth / 2) - (width / 2);
            
            // For consistent connections across swimlanes, ensure same relative spacing
            if (node.Type != NodeType.Decision && node.Type != NodeType.DatabaseOperation)
            {
                // Standard nodes always use standard width for consistent connections
                // Adjust X to maintain center alignment
                int standardWidth = 100; // Standard width for process nodes
                relativeX = (swimlaneWidth / 2) - (standardWidth / 2);
            }
            
            sb.AppendLine($"          <mxGeometry x=\"{relativeX}\" y=\"{yPosition - swimlaneY}\" width=\"{width}\" height=\"{height}\" as=\"geometry\" />");

            sb.AppendLine("        </mxCell>");
        }
        
        // Finish the document
        sb.AppendLine("      </root>");
        sb.AppendLine("    </mxGraphModel>");
        sb.AppendLine("  </diagram>");
        sb.AppendLine("</mxfile>");
        
        // Write the XML to the output file
        File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false)); // false = no BOM
        
        Console.WriteLine($"XML document saved with {diagram.Nodes.Count} nodes and {diagram.Connections.Count} connections.");
    }

    private void OrganizeConnectedNodes(int nodeId, Diagram diagram, HashSet<int> processedNodes, ref int currentY)
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
                    // Find the source node
                    var sourceNode = diagram.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (sourceNode != null)
                    {
                        // If nodes are in different swimlanes, keep Y position consistent with source
                        if (sourceNode.MicroserviceName != targetNode.MicroserviceName)
                        {
                            targetNode.Y = sourceNode.Y; // Maintain same Y position across swimlanes
                        }
                        else
                        {
                            // For nodes in same swimlane, use standard vertical positioning
                            targetNode.Y = currentY;
                            currentY += 100; // Increment Y position for next node
                        }
                    }
                    else
                    {
                        // Fallback to standard positioning if source not found
                        targetNode.Y = currentY;
                        currentY += 100;
                    }
                    
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
            NodeType.Start => "strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;fillColor=none;",
            NodeType.End => "strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;fillColor=none;",
            NodeType.Process => "rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;fillColor=none;",
            NodeType.Decision => "strokeWidth=2;html=1;shape=mxgraph.flowchart.decision;whiteSpace=wrap;fillColor=none;fillOpacity=0;",
            NodeType.DatabaseOperation => "strokeWidth=2;html=1;shape=mxgraph.flowchart.database;whiteSpace=wrap;fillColor=none;",
            NodeType.ApiCall => "strokeWidth=2;html=1;shape=mxgraph.flowchart.direct_data;whiteSpace=wrap;fillColor=none;",
            NodeType.EventPublish => "strokeWidth=2;html=1;shape=mxgraph.flowchart.paper_tape;whiteSpace=wrap;fillColor=none;",
            NodeType.EventSubscribe => "strokeWidth=2;html=1;shape=mxgraph.flowchart.sequential_data;whiteSpace=wrap;fillColor=none;",
            _ => "rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;fillColor=none;"
        };
    }
    
    private string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
            
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    // Add a new method to organize node positions
    private void OrganizeNodePositions(Diagram diagram)
    {
        if (diagram.Nodes.Count == 0)
            return;

        // Get the start node
        var startNode = diagram.Nodes.FirstOrDefault(n => n.Type == NodeType.Start);
        if (startNode == null)
        {
            // If no start node found, just use the first node
            startNode = diagram.Nodes.First();
        }

        // Set initial position for the start node
        startNode.X = 100;
        startNode.Y = 100;

        // Track processed nodes to avoid cycles
        var processedNodes = new HashSet<int> { startNode.Id };

        // Start layout from the Y position of the start node plus spacing
        int currentY = startNode.Y + 100;

        // Process all connected nodes starting from the start node
        OrganizeConnectedNodes(startNode.Id, diagram, processedNodes, ref currentY);

        // In case there are disconnected nodes, position them as well
        foreach (var node in diagram.Nodes)
        {
            if (!processedNodes.Contains(node.Id))
            {
                // Position disconnected nodes with increasing Y values
                node.Y = currentY;
                currentY += 100;
                
                // Look for connections from this node
                OrganizeConnectedNodes(node.Id, diagram, processedNodes, ref currentY);
            }
        }
    }

    // Check if a Y position is already occupied within a safety margin
    private bool IsYPositionOccupied(HashSet<int> occupiedPositions, int yPosition, int safetyMargin)
    {
        // Check within a margin to avoid nodes being too close
        for (int y = yPosition - safetyMargin; y <= yPosition + safetyMargin; y++)
        {
            if (occupiedPositions.Contains(y))
            {
                return true;
            }
        }
        return false;
    }
    
    // Find the next available Y position that doesn't conflict with occupied positions
    private int FindNextAvailableYPosition(HashSet<int> occupiedPositions, int startY, int increment)
    {
        int yPosition = startY;
        while (IsYPositionOccupied(occupiedPositions, yPosition, 40))
        {
            yPosition += increment;
        }
        return yPosition;
    }

    private void CalculatePositionsForSequence(int nodeId, int baseY, Diagram diagram, HashSet<int> processed, Dictionary<int, int> preCalculatedYPositions, int nodeVerticalSpacing)
    {
        // Find all connections where this node is the source
        var connections = diagram.Connections
            .Where(c => c.SourceNodeId == nodeId)
            .ToList();
            
        foreach (var connection in connections)
        {
            if (!processed.Contains(connection.TargetNodeId))
            {
                processed.Add(connection.TargetNodeId);
                
                // Find the target node and update its Y position
                var targetNode = diagram.Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
                if (targetNode != null)
                {
                    // Find the source node
                    var sourceNode = diagram.Nodes.FirstOrDefault(n => n.Id == nodeId);
                    if (sourceNode != null)
                    {
                        // If nodes are in different swimlanes, keep Y position consistent with source
                        if (sourceNode.MicroserviceName != targetNode.MicroserviceName)
                        {
                            // Use source node's position for cross-swimlane connections
                            preCalculatedYPositions[targetNode.Id] = preCalculatedYPositions[sourceNode.Id];
                        }
                        else
                        {
                            // For nodes in same swimlane, use incremental positioning
                            preCalculatedYPositions[targetNode.Id] = baseY;
                            baseY += nodeVerticalSpacing;
                        }
                    }
                    else
                    {
                        // Fallback to standard positioning if source not found
                        preCalculatedYPositions[targetNode.Id] = baseY;
                        baseY += nodeVerticalSpacing;
                    }

                    // Recursively process connections from this node
                    CalculatePositionsForSequence(targetNode.Id, baseY, diagram, processed, preCalculatedYPositions, nodeVerticalSpacing);
                }
            }
        }
    }
} 