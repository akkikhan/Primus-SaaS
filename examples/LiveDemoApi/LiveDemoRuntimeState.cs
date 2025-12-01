namespace LiveDemoApi;

public record LiveDemoRuntimeState(bool IdentityEnabled)
{
    public string IdentityMessage => IdentityEnabled
        ? "Primus Identity Validator is active."
        : "Primus Identity Validator is commented out or not registered in Program.cs.";
}
