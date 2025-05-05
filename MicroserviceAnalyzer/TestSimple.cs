using System;
using System.IO;
using System.Text;

namespace MicroserviceAnalyzer
{
    public class TestSimple
    {
        public static void CreateSimpleDiagram(string outputPath)
        {
            Console.WriteLine($"Creating simple test diagram at {outputPath}...");
            
            // Create exact XML format
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<mxfile host=\"Electron\" agent=\"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/26.2.15 Chrome/134.0.6998.205 Electron/35.2.1 Safari/537.36\" version=\"26.2.15\">");
            sb.AppendLine("  <diagram name=\"Flow Diagram\" id=\"simple-test-diagram\">");
            sb.AppendLine("    <mxGraphModel dx=\"1106\" dy=\"783\" grid=\"1\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"827\" pageHeight=\"1169\" math=\"0\" shadow=\"0\">");
            sb.AppendLine("      <root>");
            sb.AppendLine("        <mxCell id=\"0\" />");
            sb.AppendLine("        <mxCell id=\"1\" parent=\"0\" />");
            
            // Start node
            string startNodeId = "start-node";
            sb.AppendLine($"        <mxCell id=\"{startNodeId}\" value=\"Start\" style=\"strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine("          <mxGeometry x=\"100\" y=\"100\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Process node
            string processNodeId = "process-node";
            sb.AppendLine($"        <mxCell id=\"{processNodeId}\" value=\"Process\" style=\"rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine("          <mxGeometry x=\"100\" y=\"220\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // End node
            string endNodeId = "end-node";
            sb.AppendLine($"        <mxCell id=\"{endNodeId}\" value=\"End\" style=\"strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine("          <mxGeometry x=\"100\" y=\"340\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Connection 1
            string conn1Id = "conn1";
            sb.AppendLine($"        <mxCell id=\"{conn1Id}\" value=\"\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{startNodeId}\" target=\"{processNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Connection 2
            string conn2Id = "conn2";
            sb.AppendLine($"        <mxCell id=\"{conn2Id}\" value=\"\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{processNodeId}\" target=\"{endNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Close tags
            sb.AppendLine("      </root>");
            sb.AppendLine("    </mxGraphModel>");
            sb.AppendLine("  </diagram>");
            sb.AppendLine("</mxfile>");
            
            // Write to file
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            
            Console.WriteLine($"Simple test diagram created at: {outputPath}");
        }
    }
} 