using Atlas.Backoffice.Api.Dtos.Requests;
using Atlas.Backoffice.Api.Dtos.Responses;
using Atlas.Common;

namespace Atlas.Backoffice.Api.UseCases.Services;

/// <summary>Development-only login, standing in for the bank's identity provider.</summary>
public interface IDevTokenService
{
    Result<DevTokenResponse> Issue(DevTokenRequest request);
}
