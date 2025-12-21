
using PrimusSaaS.Security.DataAggregator;

if (args.Length < 2)
{
    Console.WriteLine("Usage: DataAggregator <advisories_path> <output_db_path>");
    return;
}

string advisoriesPath = args[0];
string dbPath = args[1];
string schemaPath = "schema.sql";

if (!File.Exists(schemaPath))
{
    // Try finding it if running from bin
    schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "schema.sql");
}

if (!File.Exists(schemaPath))
{
    Console.WriteLine($"Error: schema.sql not found at {schemaPath}");
    return;
}

try
{
    Console.WriteLine("PrimusSaaS Security Data Aggregator");
    Console.WriteLine("===================================");
    
    var builder = new DatabaseBuilder(schemaPath, dbPath);
    
    Console.WriteLine($"Initializing database at {dbPath}...");
    builder.Initialize();
    
    await builder.ProcessAdvisoriesAsync(advisoriesPath);
    
    Console.WriteLine("Done.");
}
catch (Exception ex)
{
    Console.WriteLine($"Fatal Error: {ex}");
}
