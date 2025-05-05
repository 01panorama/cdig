namespace MicroserviceAnalyzer.Models;

public class Diagram
{
    public string Name { get; set; } = "Flow Diagram";
    public List<DiagramNode> Nodes { get; set; } = new List<DiagramNode>();
    public List<DiagramConnection> Connections { get; set; } = new List<DiagramConnection>();
    public string SourceClassName { get; set; } = string.Empty;
    public string SourceMethodName { get; set; } = string.Empty;
} 