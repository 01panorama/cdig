using System;

namespace MicroserviceAnalyzer.Models;

public enum NodeType
{
    Start,
    Process,
    Decision,
    End,
    ApiCall,
    DatabaseOperation,
    EventPublish,
    EventSubscribe
}

public class DiagramNode
{
    private static int _nextId = 1;
    
    public DiagramNode()
    {
        Id = _nextId++;
        Width = 100;
        Height = 60;
        BackgroundColor = "transparent"; // Ensure transparent background
    }
    
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string BackgroundColor { get; set; }
    public string MicroserviceName { get; set; } = string.Empty;
    public string OriginalClassName { get; set; } = string.Empty;
    public string OriginalMethodName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    
    public bool IsDecision => Type == NodeType.Decision;
} 