namespace PrimusSaaS.Logging.Core;

/// <summary>
/// Enriches logs with the machine name
/// </summary>
public class MachineNameEnricher : IEnricher
{
    private readonly string _machineName;

    public MachineNameEnricher()
    {
        try
        {
            _machineName = Environment.MachineName;
        }
        catch
        {
            _machineName = "unknown";
        }
    }

    public void Enrich(Dictionary<string, object> context)
    {
        context["machineName"] = _machineName;
    }
}

/// <summary>
/// Enriches logs with the current thread ID
/// </summary>
public class ThreadIdEnricher : IEnricher
{
    public void Enrich(Dictionary<string, object> context)
    {
        context["threadId"] = Environment.CurrentManagedThreadId;
    }
}

/// <summary>
/// Enriches logs with a static property value
/// </summary>
public class PropertyEnricher : IEnricher
{
    private readonly string _key;
    private readonly object _value;

    public PropertyEnricher(string key, object value)
    {
        _key = key;
        _value = value;
    }

    public void Enrich(Dictionary<string, object> context)
    {
        context[_key] = _value;
    }
}
