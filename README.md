# Microservice Analyzer

A tool for analyzing and visualizing microservice interactions within a .NET solution. The analyzer creates diagrams showing the flow of execution across different microservices, highlighting API calls, database operations, and message publishing.

## Features

- Identifies all projects in a solution
- Traces method calls from a specified entry point
- Visualizes decision points (if statements) with TRUE/FALSE branches
- Detects API calls between microservices
- Identifies database operations
- Detects event publishing operations
- Generates visual diagrams of service interactions

## Prerequisites

- .NET 6.0 SDK or later
- A solution containing microservices to analyze

## Installation

Clone this repository and navigate to the project directory:

```bash
git clone [repository-url]
cd MicroserviceAnalyzer
```

## Usage

Run the analyzer using the `dotnet run` command:

```bash
dotnet run --project MicroserviceAnalyzer [path-to-solution] [starting-class-name] [starting-method-name]
```

### Parameters:

- `path-to-solution`: Path to the root directory of the solution to analyze
- `starting-class-name`: Name of the class containing the entry point method
- `starting-method-name`: Name of the method to start the analysis from

### Example:

```bash
dotnet run --project MicroserviceAnalyzer ~/projects/MyMicroservices OrderController ProcessOrder
```

This will analyze the solution starting from the `ProcessOrder` method in the `OrderController` class.

## Output

The analyzer generates a diagram showing:

- The flow of execution across different microservices
- Decision points (if statements) with clearly marked TRUE/FALSE branches
- Conditional logic and early returns
- API calls between services
- Database operations
- Message publishing events

Each microservice is displayed in its own swimlane, making it easy to visualize cross-service interactions.

## Customization

You can extend the analyzer to detect additional patterns by modifying the detection methods in `CodeAnalyzer.cs`:

- `IsHttpClientCall`: Detects HTTP client calls
- `IsMessagePublishingCall`: Detects message publishing operations  
- `IsDatabaseOperation`: Detects database operations

## License

[License Information] 