# Element Template Configuration

Read-only, connector-independent GQI sources with [DataMiner Assistant tools](Assistant).
Both require **elementID** as one string in `dmaID/elementID` format, e.g., `123/456`.
The Assistant example is not a GQI execution default.

| Source | Output |
| --- | --- |
| Element alarm template configuration | Raw thresholds, filters, conditions, schedules, and ordered group members |
| Element trend template configuration | Parameter/row rules with real-time and average trending flags |

Repeated, filtered, and inactive rules remain visible, with template provenance
and raw JSON. Reads use caller permissions; failures are not empty results.
Alarm rules are enabled when `Monitored = true`; trend rules when
`RealTimeTrended = true` or `AverageTrended = true`.

## Build and install

Requires Windows, .NET Framework 4.8, and a .NET SDK. Declared minimum:
**DataMiner 10.5.0 CU0** (legacy GQI API); CU0 runtime compatibility is unverified.

```powershell
dotnet build ElementTemplates.sln -c Release --warnaserror
dotnet test ElementTemplates.Tests\ElementTemplates.Tests.csproj -c Release --no-build
```

Install the `.dmapp` from `ElementTemplates.Package\bin\Release\net48\DataMinerBuild`,
then select either source under **Get custom data**. Install/upgrade also registers
both Assistant tools automatically; a compatible Assistant installation is required.
Only the two owned registrations are overwritten.

**Limitations:** effective coverage, conditions, schedules, and filters are not
evaluated. Separate alarm include/exclude state is unavailable, and excluded-row
completeness is unverified. Assistant query execution remains unverified.

See the [tool contracts](Assistant), [Catalog information](ElementTemplates.Package/CatalogInformation/README.md),
and [publishing guide](ElementTemplates.Package/GettingStarted.md).
