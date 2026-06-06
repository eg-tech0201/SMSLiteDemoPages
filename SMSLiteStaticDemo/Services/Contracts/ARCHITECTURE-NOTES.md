# Survey Management Integration Notes

Keep the current Blazor mock UI in place, but move future data access behind service contracts so the database/integration step is mostly a service implementation swap.

## Current UI Surfaces
- `SMSLiteStaticDemo/Screens/Surveys/SurveyDetails.razor`
  - survey detail
  - materials tab
  - survey records tab
- `SMSLiteStaticDemo/Screens/Surveys/SurveyDocumentViewer.razor`
  - questionnaire/material viewer shell
- `SMSLiteStaticDemo/Screens/Surveys/RespondentDetails.razor`
  - burden widget
  - interaction timeline
  - ELMA link shell
- `SMSLiteStaticDemo/Screens/Search/GlobalSearch.razor`
  - grouped cross-system search
  - advanced filters
  - saved searches UI
- `SMSLiteStaticDemo/Screens/Surveys/SurveyExports.razor`
  - export permission shell
  - export/audit demo

## New Contract Layer
- `Models/Workflows/SurveyWorkspaceModels.cs`
  - survey instance keys
  - survey list/detail DTOs
  - questionnaire/material DTOs
  - record grid DTOs
  - document DTOs
- `Models/Workflows/RespondentWorkspaceModels.cs`
  - respondent detail DTOs
  - burden/timeline DTOs
- `Models/Workflows/SearchAndAccessModels.cs`
  - global search DTOs
  - saved search DTOs
  - export/access/audit DTOs
- `Services/Contracts/*.cs`
  - service interfaces the UI should eventually depend on

## Next
1. Add mock implementations of the contract interfaces that adapt the current `SurveyInstanceService`.
2. Change Razor pages to inject the contracts instead of directly using `SurveyInstanceService`.
3. Introduce repository/DAO interfaces for real data sources.
4. Replace mock implementations with DB/API-backed implementations when integration starts.

## Integration Boundaries
- Survey Review:
  - `ISurveyReviewGateway`
  - real auth, resilience, caching, and API mapping still pending
- ELMA:
  - `IElmaGateway`
  - `External.ELMA.Client`
  - static-link behavior remains the approved UI fallback until ELMA publishes a supported submit/status contract
- Search:
  - `IGlobalSearchService`
  - current UI is demo-only and not performance validated
- Export/Auth:
  - `IExportAndAccessService`
  - current role/audit behavior is demo-only
