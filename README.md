# Element Template Configuration

Read-only GQI data sources for the alarm and trend templates assigned to an
element, with [DataMiner Assistant tools](Assistant). Connector-independent.

## Input and output

Both sources require one string argument: **elementID**, in `dmaID/elementID`
format (for example, `123/456`). Both components must be positive `Int32` values.
Existing queries using two integer inputs must be updated to this single input.

| Data source | Output |
| --- | --- |
| Element alarm template configuration | Raw thresholds, parameter IDs, filters, conditions, schedules, and ordered alarm group members |
| Element trend template configuration | Parameter/row rules with separate real-time and average trending flags |

Repeated, filtered, and disabled rules are retained. Results include element,
connector, and template provenance, raw rule JSON, and explicit missing/empty
assignment states. Reads use the caller's permissions; failures are not empty
results. Both sources use normal GQI fetch/paging.

**Limitation:** effective coverage is not evaluated. The alarm API exposes no
separate include/exclude field, and excluded-row completeness is not verified.
The full all-configured-rows requirement remains blocked pending supported API
clarification and read-only host comparison ([API investigation](https://github.com/SkylineCommunications/SLC-GQIDS-ElementTemplates/blob/df322046586fa036921e341561b9bb7856b7e02e/README.md)).
No live GQI/Assistant acceptance
or deployment has been performed.

## Build and use

Requires Windows, .NET Framework 4.8, and a .NET SDK. The package targets
**DataMiner 10.6.12 or later with the GQI DxM enabled** (Core GQI API 1.6.0).

```powershell
dotnet build ElementTemplates.sln -c Release --warnaserror
dotnet test ElementTemplates.Tests\ElementTemplates.Tests.csproj -c Release --no-build
```

Install `ElementTemplates\bin\Release\net48\DataMinerBuild\ElementTemplates.1.0.0.dmapp`
on a compatible system, then select either source under **Get custom data**.
Copy `Assistant\*.md` to the Assistant host's
`C:\ProgramData\Skyline Communications\DataMiner Assistant\Synced Documents\Context\Custom\Adhoc`.
The tool files describe the full input and column contracts.

Script/library identity: `ElementTemplates`. Classes:
`ElementTemplates.AlarmTemplateSource` and `ElementTemplates.TrendTemplateSource`.
Keep these names stable for existing queries and tools.

## Repository

- `ElementTemplates\`: SDK GQI project, Catalog information, and publish profile.
- `ElementTemplates.Tests\`: mapping, paging, permissions, and tool-contract tests.
- `Assistant\`: custom data-tool registrations.

See [Catalog information](ElementTemplates/CatalogInformation/README.md) for the
item overview and [Getting started](ElementTemplates/GettingStarted.md) for the
template's publishing instructions. Publishing requires separate authorization;
building does not publish or deploy.
