using MicroserviceAnalyzer.Models;
using System;
using System.Collections.Generic;

namespace MicroserviceAnalyzer.Services
{
    public static class SampleTest
    {
        public static void CreateSampleDiagram(string outputPath)
        {
            Console.WriteLine("Creating sample diagram to demonstrate if statements...");

            // Create a manual diagram to show the expected structure
            var diagram = new Diagram
            {
                Name = "Flow Diagram",
                SourceClassName = "OrderService",
                SourceMethodName = "ProcessOrder"
            };

            var nodes = new List<DiagramNode>();
            var connections = new List<DiagramConnection>();

            // Start node
            var startNode = new DiagramNode
            {
                Id = "start1",
                Label = "OrderService.ProcessOrder",
                Type = NodeType.Start,
                X = 200,
                Y = 100,
                MicroserviceName = "OrderService"
            };
            nodes.Add(startNode);

            // First if statement
            var ifValidNode = new DiagramNode
            {
                Id = "if1",
                Label = "If order != null",
                Type = NodeType.Decision,
                X = 200,
                Y = 250,
                Width = 100,
                Height = 100,
                MicroserviceName = "OrderService"
            };
            nodes.Add(ifValidNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "start1",
                TargetNodeId = "if1"
            });

            // Validate order
            var validateNode = new DiagramNode
            {
                Id = "process1",
                Label = "ValidateOrder(order)",
                Type = NodeType.Process,
                X = 400,
                Y = 250,
                MicroserviceName = "OrderService"
            };
            nodes.Add(validateNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "if1",
                TargetNodeId = "process1",
                Label = "true"
            });

            // Second if statement - check if order is valid
            var ifPriceNode = new DiagramNode
            {
                Id = "if2",
                Label = "If order.Total > 1000",
                Type = NodeType.Decision,
                X = 400,
                Y = 350,
                Width = 100,
                Height = 100,
                MicroserviceName = "OrderService"
            };
            nodes.Add(ifPriceNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "process1",
                TargetNodeId = "if2"
            });

            // High value order process
            var highValueNode = new DiagramNode
            {
                Id = "process2",
                Label = "ProcessHighValueOrder(order)",
                Type = NodeType.Process,
                X = 550,
                Y = 350,
                MicroserviceName = "OrderService"
            };
            nodes.Add(highValueNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "if2",
                TargetNodeId = "process2",
                Label = "true"
            });

            // Regular order process
            var regularNode = new DiagramNode
            {
                Id = "process3",
                Label = "ProcessRegularOrder(order)",
                Type = NodeType.Process,
                X = 400,
                Y = 450,
                MicroserviceName = "OrderService"
            };
            nodes.Add(regularNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "if2",
                TargetNodeId = "process3",
                Label = "false"
            });

            // Save order to database
            var dbNode = new DiagramNode
            {
                Id = "db1",
                Label = "SaveOrder(order)",
                Type = NodeType.DatabaseOperation,
                X = 650,
                Y = 550,
                MicroserviceName = "Database"
            };
            nodes.Add(dbNode);
            
            // Connect both processing paths to database
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "process2",
                TargetNodeId = "db1"
            });
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "process3",
                TargetNodeId = "db1"
            });

            // Error handling for invalid order
            var errorNode = new DiagramNode
            {
                Id = "process4",
                Label = "LogError(\"Invalid order\")",
                Type = NodeType.Process,
                X = 200,
                Y = 450,
                MicroserviceName = "OrderService"
            };
            nodes.Add(errorNode);
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "if1",
                TargetNodeId = "process4",
                Label = "false"
            });

            // End node
            var endNode = new DiagramNode
            {
                Id = "end1",
                Label = "End",
                Type = NodeType.End,
                X = 400,
                Y = 650,
                MicroserviceName = "OrderService"
            };
            nodes.Add(endNode);
            
            // Connect all terminal paths to end
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "db1",
                TargetNodeId = "end1"
            });
            connections.Add(new DiagramConnection
            {
                SourceNodeId = "process4",
                TargetNodeId = "end1"
            });

            diagram.Nodes = nodes;
            diagram.Connections = connections;

            // Generate the diagram
            var generator = new DiagramGenerator();
            generator.GenerateDiagram(diagram, outputPath);

            Console.WriteLine($"Sample diagram with if statements saved to: {outputPath}");
        }
    }
} 