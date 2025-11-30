namespace LiveDemoApi.Models;

public record SendWelcomeRequest(string Email, string Name);
public record SendSmsRequest(string PhoneNumber, string Message);
public record LogTestRequest(string? Message, string? UserId, string? Level, string? Email);
public record DocumentRenderRequest(string? TenantId, string? Title, string? Subtitle, string? ContentType, string? Content, Dictionary<string, string>? Metadata);
public record DocumentSelfTestRequest(string? Mode);
public record TemplateUpdateRequest(string Content);
public record TemplatePreviewRequest(string Type, string Channel, string? Content, string? Message, string? PhoneNumber, string? Name, string? Email, string? Code, string? Link);
public record NotificationTestRequest(string Email, string Name, string Code, string Link);
