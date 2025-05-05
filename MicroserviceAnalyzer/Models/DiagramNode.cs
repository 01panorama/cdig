namespace MicroserviceAnalyzer.Models;

public class DiagramNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Label { get; set; } = string.Empty;
    public NodeType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 120;
    public int Height { get; set; } = 60;
    public string Style { get; set; } = string.Empty;
    public string OriginalMethodName { get; set; } = string.Empty;
    public string OriginalClassName { get; set; } = string.Empty;
    public string MicroserviceName { get; set; } = string.Empty;
    public string EndpointUrl { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
}

public enum NodeType
{
    Start,
    End,
    Process,
    Decision,
    DatabaseOperation,
    ExternalService,
    Controller,
    ApiCall,
    MessageQueue,
    EventPublish,
    EventSubscribe
} 