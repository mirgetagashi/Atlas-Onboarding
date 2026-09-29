namespace Atlas.Backoffice.Api.Dtos.Responses;

/// <summary>A document streamed through from Onboarding. The stream is disposed when the response is sent.</summary>
public sealed record DocumentContent(Stream Content, string ContentType);
