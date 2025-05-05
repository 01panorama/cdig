using MicroserviceAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace MicroserviceAnalyzer.Services
{
    public static class TestSimple
    {
        public static void CreateSimpleDiagram(string outputPath)
        {
            // Create a test diagram with an if statement similar to the one in the real code
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<mxfile host=\"Electron\" agent=\"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) draw.io/26.2.15 Chrome/134.0.6998.205 Electron/35.2.1 Safari/537.36\" version=\"26.2.15\">");
            sb.AppendLine("  <diagram name=\"File Check Test\" id=\"simple-test-diagram\">");
            sb.AppendLine("    <mxGraphModel dx=\"1106\" dy=\"783\" grid=\"1\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"827\" pageHeight=\"1169\" math=\"0\" shadow=\"0\">");
            sb.AppendLine("      <root>");
            sb.AppendLine("        <mxCell id=\"0\" />");
            sb.AppendLine("        <mxCell id=\"1\" parent=\"0\" />");
            
            // Create swimlanes
            sb.AppendLine("        <mxCell id=\"project_swimlane\" value=\"BaseFileUploadService\" style=\"swimlane;whiteSpace=wrap;html=1;fillColor=#f5f5f5;strokeColor=#666666;fontColor=#333333;startSize=30;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine("          <mxGeometry x=\"20\" y=\"-60\" width=\"250\" height=\"700\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            sb.AppendLine("        <mxCell id=\"database_swimlane\" value=\"Database\" style=\"swimlane;whiteSpace=wrap;html=1;fillColor=#dae8fc;strokeColor=#6c8ebf;startSize=30;\" vertex=\"1\" parent=\"1\">");
            sb.AppendLine("          <mxGeometry x=\"270\" y=\"-60\" width=\"250\" height=\"700\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Create nodes with connections
            string startNodeId = "start001";
            string ifNodeId = "if001";
            string returnNodeId = "return001";
            string continueNodeId = "continue001";
            string endNodeId = "end001";
            
            // Start node
            sb.AppendLine($"        <mxCell id=\"{startNodeId}\" value=\"BaseFileUploadService.GetFileFtp\" style=\"strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;\" vertex=\"1\" parent=\"project_swimlane\">");
            sb.AppendLine("          <mxGeometry x=\"80\" y=\"100\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // If node (decision)
            sb.AppendLine($"        <mxCell id=\"{ifNodeId}\" value=\"If file == null || file.Length == 0\" style=\"strokeWidth=2;html=1;shape=mxgraph.flowchart.decision;whiteSpace=wrap;\" vertex=\"1\" parent=\"project_swimlane\">");
            sb.AppendLine("          <mxGeometry x=\"80\" y=\"200\" width=\"120\" height=\"100\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Return node (early exit)
            sb.AppendLine($"        <mxCell id=\"{returnNodeId}\" value=\"Return (Early Exit)\" style=\"rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;\" vertex=\"1\" parent=\"project_swimlane\">");
            sb.AppendLine("          <mxGeometry x=\"80\" y=\"350\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Continue processing node
            sb.AppendLine($"        <mxCell id=\"{continueNodeId}\" value=\"Continue Processing\" style=\"rounded=1;whiteSpace=wrap;html=1;absoluteArcSize=1;arcSize=14;strokeWidth=2;\" vertex=\"1\" parent=\"project_swimlane\">");
            sb.AppendLine("          <mxGeometry x=\"200\" y=\"300\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // End node
            sb.AppendLine($"        <mxCell id=\"{endNodeId}\" value=\"End\" style=\"strokeWidth=2;html=1;shape=mxgraph.flowchart.start_1;whiteSpace=wrap;\" vertex=\"1\" parent=\"project_swimlane\">");
            sb.AppendLine("          <mxGeometry x=\"80\" y=\"450\" width=\"120\" height=\"60\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Connections
            string conn1 = "conn001";
            string conn2 = "conn002";
            string conn3 = "conn003";
            string conn4 = "conn004";
            
            // Start to If
            sb.AppendLine($"        <mxCell id=\"{conn1}\" value=\"\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{startNodeId}\" target=\"{ifNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // If true to Return
            sb.AppendLine($"        <mxCell id=\"{conn2}\" value=\"true\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{ifNodeId}\" target=\"{returnNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // If false to Continue
            sb.AppendLine($"        <mxCell id=\"{conn3}\" value=\"false\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{ifNodeId}\" target=\"{continueNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Return to End
            sb.AppendLine($"        <mxCell id=\"{conn4}\" value=\"\" style=\"edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;\" edge=\"1\" parent=\"1\" source=\"{returnNodeId}\" target=\"{endNodeId}\">");
            sb.AppendLine("          <mxGeometry relative=\"1\" as=\"geometry\" />");
            sb.AppendLine("        </mxCell>");
            
            // Close XML
            sb.AppendLine("      </root>");
            sb.AppendLine("    </mxGraphModel>");
            sb.AppendLine("  </diagram>");
            sb.AppendLine("</mxfile>");
            
            // Write to file
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
            
            Console.WriteLine($"Test diagram with if statement saved to: {outputPath}");
        }
    }
} 