# Where to look

If you want the implementation of a feature, start in the file on the right.

| Feature | Implementation |
|---|---|
| Mobile API (create, resume, upload, submit) | `src/Onboarding/Atlas.Onboarding.Api/Controllers/ApplicationsController.cs` |
| What those calls do | `src/Onboarding/Atlas.Onboarding.UseCases/Services/ApplicantService.cs` |
| Save and resume (`nextSteps` after a dropped connection) | `src/Onboarding/Atlas.Onboarding.UseCases/Mapping/ApplicationMappings.cs` |
| Status rules (draft, submit, sanctions match, MD branch, approve, reject) | `src/Onboarding/Atlas.Onboarding.Domain/Application.cs` |
| Application id is `{market}-{guid}`, never the national id | `src/Onboarding/Atlas.Onboarding.Domain/ApplicationId.cs` |
| Per-market identifier rules (MF may use a passport) | `src/BuildingBlocks/Atlas.Markets/markets.json`, `IdentifierValidator.cs` |
| Document upload as raw bytes, not base64 | `ApplicationsController.UploadDocument`, stored by `src/Onboarding/Atlas.Onboarding.Infrastructure/Storage/DocumentStore.cs` |
| One database and one blob container per market | `src/Onboarding/Atlas.Onboarding.Infrastructure/Storage/MarketStorageOptions.cs`, `Persistence/MarketDbContextFactory.cs` |
| Applicant token (only the hash is stored) | `src/Onboarding/Atlas.Onboarding.UseCases/Shared/ApplicantToken.cs` |
| Audit log (who read or changed personal data) | `src/Onboarding/Atlas.Onboarding.Infrastructure/Auditing/AuditEntry.cs`, written from `ApplicantService` and `StaffApplicationService` |
| Outbox (status change and event saved together) | `src/Onboarding/Atlas.Onboarding.Infrastructure/Outbox/OutboxPublisher.cs` |
| IDNow, then World-Check | `src/Verification/Atlas.Verification.Worker/Services/VerificationService.cs` |
| Provider retries and circuit breaker | `src/Verification/Atlas.Verification.Worker/Program.cs` (`AddStandardResilienceHandler`) |
| Message retries | `src/BuildingBlocks/Atlas.ServiceDefaults/Messaging/MessagingExtensions.cs` |
| Applying the check result to the application | `src/Onboarding/Atlas.Onboarding.Api/Messaging/VerificationCompletedConsumer.cs`, `UseCases/Services/VerificationResultService.cs` |
| Compliance queue and the officer's decision | `src/Backoffice/Atlas.Backoffice.Api/Domain/ReviewCase.cs`, `UseCases/Services/ReviewCaseService.cs` |
| Officer's token forwarded, so the audit names the person | `src/Backoffice/Atlas.Backoffice.Api/Infrastructure/Onboarding/ForwardUserTokenHandler.cs` |
| MD branch activation (wet signature) | `src/Backoffice/Atlas.Backoffice.Api/UseCases/Services/BranchActivationService.cs` |

## Decisions that were not in the requirements

We did not build the ticket's single `POST /applications` that returns `APPROVED` or `REJECTED` in the same response, with the photos as base64 inside the JSON.

That shape is what the epic asked for. The `#atlas-onboarding` thread is why we did not build it. The photos are several megabytes, so base64 in one JSON body fails on a bad connection, and a lost connection (the tunnel case in the chat) would make the customer start again. The app creates a draft, uploads each image as the raw body (`image/jpeg` or `image/png`), submits, and reads `GET /applications/{id}`. Submit returns `202`, because a possible sanctions match waits for an officer and cannot be decided inside that call.

These were not written requirements. They are the calls we took from that thread:

- **Save and resume.** A draft stays, and `nextSteps` tells the app what is still missing (`UPLOAD_IDENTITY_DOCUMENT`, `UPLOAD_SELFIE`, `ACCEPT_TERMS_AND_SUBMIT`).
- **Images are not base64.** Each document is the request body, up to 10 MB, stored in that market's blob container.
- **The national id is not the key.** The id is `{market}-{guid}`, so it carries no personal data and any service can route to the right country. The national id is validated per market and stored as data. In MF a passport is accepted. It is never the primary key.
- **No websocket.** The app comes back with GET. The slow work (providers, and up to 48 hours of review) is not inside the request.

## Please ignore

`src/Providers/Atlas.Providers.Mock` and `POST /dev/token`. They stand in for IDNow, World-Check and the bank's login so the flow can be run. They are not the product.
