namespace Atlas.Onboarding.UseCases.Dtos.Responses;

/// <summary>A document's bytes, streamed from storage. Whoever returns it to the client disposes the stream.</summary>
public sealed record DocumentContent(Stream Content, string ContentType);
