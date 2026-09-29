using Atlas.Onboarding.Domain;

namespace Atlas.Onboarding.UseCases.Dtos.Responses;

public sealed record DocumentResponse(DocumentType Type, string ContentType, long SizeBytes, DateTimeOffset UploadedAt);
