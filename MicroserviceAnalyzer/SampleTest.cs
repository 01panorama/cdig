using System;
using MicroserviceAnalyzer.Models;
using MicroserviceAnalyzer.Services;
using System.Collections.Generic;

namespace MicroserviceAnalyzer
{
    public class SampleTest
    {
        public static void CreateSampleDiagram(string outputPath)
        {
            Console.WriteLine("Creating sample diagram for testing...");
            
            // Create a sample diagram
            var diagram = new Diagram
            {
                Name = "Test Diagram",
                SourceClassName = "TestClass",
                SourceMethodName = "TestMethod"
            };
            
            // Add some nodes
            var startNode = new DiagramNode
            {
                Label = "Start",
                Type = NodeType.Start,
                X = 100,
                Y = 100,
                MicroserviceName = "Service1"
            };
            
            var processNode = new DiagramNode
            {
                Label = "Process Data",
                Type = NodeType.Process,
                X = 100,
                Y = 200,
                MicroserviceName = "Service1"
            };
            
            var databaseNode = new DiagramNode
            {
                Label = "Save to Database",
                Type = NodeType.DatabaseOperation,
                X = 100,
                Y = 300
            };
            
            var endNode = new DiagramNode
            {
                Label = "End",
                Type = NodeType.End,
                X = 100,
                Y = 400,
                MicroserviceName = "Service1"
            };
            
            // Add nodes to diagram
            diagram.Nodes.Add(startNode);
            diagram.Nodes.Add(processNode);
            diagram.Nodes.Add(databaseNode);
            diagram.Nodes.Add(endNode);
            
            // Add connections
            diagram.Connections.Add(new DiagramConnection
            {
                SourceNodeId = startNode.Id,
                TargetNodeId = processNode.Id
            });
            
            diagram.Connections.Add(new DiagramConnection
            {
                SourceNodeId = processNode.Id,
                TargetNodeId = databaseNode.Id
            });
            
            diagram.Connections.Add(new DiagramConnection
            {
                SourceNodeId = databaseNode.Id,
                TargetNodeId = endNode.Id
            });
            
            // Generate the diagram
            var generator = new DiagramGenerator();
            generator.GenerateDiagram(diagram, outputPath);
            
            Console.WriteLine($"Sample diagram created at: {outputPath}");
        }
    }
} 