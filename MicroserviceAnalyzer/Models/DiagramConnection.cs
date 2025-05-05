namespace MicroserviceAnalyzer.Models;

public class DiagramConnection
{
    private static int _nextId = 1;
    
    public DiagramConnection()
    {
        Id = _nextId++;
    }
    
    public int Id { get; set; }
    public int SourceNodeId { get; set; }
    public int TargetNodeId { get; set; }
    public string? Label { get; set; }
    public string? Style { get; set; }
} 