namespace Atlas.Onboarding.UseCases.Dtos.Requests;

/// <summary>An uploaded image as it arrives: the raw stream plus the headers that describe it.</summary>
public sealed record DocumentUpload(Stream Content, string? ContentType, long? ContentLength);
