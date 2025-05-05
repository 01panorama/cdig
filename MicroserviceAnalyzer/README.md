# Microservice Analyzer

A tool to parse and analyze C# microservice projects and generate draw.io diagrams that represent specific flows, classes, controllers, and interactions between microservices.

## Features

- Analyze C# microservice codebase
- Generate draw.io diagrams that represent code execution flow
- Visualize method calls, decision branches, and other code structures
- Track cross-service API calls and interactions
- Detect database operations and message publishing/subscribing patterns
- Visualize microservices as swimlanes with color-coding
- Start analysis from a specified class and method

## Usage

```bash
dotnet run <project_path> <class_name> <method_name> [output_file]
```

### Parameters:

- `project_path`: Path to the C# project directory
- `class_name`: Name of the class where the flow starts
- `method_name`: Name of the method where the flow starts
- `output_file`: (Optional) Path to save the draw.io XML file (default: flow_diagram.drawio.xml)

### Example:

```bash
dotnet run /path/to/microservice UserController GetUser my-diagram.drawio.xml
```

## Requirements

- .NET 8.0 or later
- Microsoft.CodeAnalysis.CSharp package

## How It Works

1. The tool uses Roslyn (Microsoft.CodeAnalysis) to analyze C# code
2. Starting from the specified class and method, it builds a graph of execution flow
3. The analyzer detects:
   - HTTP client calls between microservices
   - Database operations
   - Message publishing/subscribing patterns
   - Decision branches and control flow
4. It converts this graph into a draw.io XML diagram with:
   - Microservice swimlanes
   - Color-coded nodes by microservice
   - Special styling for cross-service communication
5. The XML can be opened in draw.io to visualize the flow

## Visualization

- **Vertical Swimlanes**: Each microservice is represented as a vertical swimlane
- **Dedicated Database Swimlane**: All database operations are grouped in a single database swimlane
- **Color Coding**: Nodes are color-coded by their microservice
- **Special Connections**: 
  - Cross-service calls are shown with dashed lines
  - Database connections are shown with blue lines
- **Node Types**:
  - Process: Regular method calls
  - Decision: Conditional branches
  - API Call: HTTP client calls to other services
  - Database Operation: Data access and persistence operations
  - Event Publish/Subscribe: Message broker interactions

## Diagram Layout

The generated diagrams follow a vertical flow structure:
- Microservices are organized as parallel vertical swimlanes
- Flow direction is from top to bottom
- Database operations are collected in a dedicated swimlane on the right
- Connected elements are automatically positioned to show logical flow
- Nodes are centered within their respective swimlanes

## Limitations

- The analysis is static, so dynamic behaviors like reflection might not be accurately represented
- Complex code paths and indirect dependencies may not be fully analyzed
- The tool works best with typical RESTful controller patterns in microservices
- URL detection in HTTP client calls relies on common patterns and might miss custom implementations 