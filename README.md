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
Alarm rows are configured in use only when `Monitored = true`. Trend rows are
enabled when `RealTimeTrended = true` or `AverageTrended = true`; false/false rows
are available but inactive. These flags do not prove effective coverage or history.

**Limitation:** effective coverage is not evaluated. The alarm API exposes no
separate include/exclude field, and excluded-row completeness is not verified.
The full all-configured-rows requirement remains blocked pending supported API
clarification and read-only host comparison ([API investigation](https://github.com/SkylineCommunications/SLC-GQIDS-ElementTemplates/blob/df322046586fa036921e341561b9bb7856b7e02e/README.md)).
Package 2.1.1, including Assistant registrations, was installed on
`slc-h42-g06.skyline.local` (DataMiner 10.6.10). The alarm query for `665/33`
was verified in Dashboards with 2.0.3. Both Assistant editors were verified to
prefill the example and retain descriptions with 2.1.1; Assistant query execution
and CU0 runtime acceptance remain unverified.

## Build and use

Requires Windows, .NET Framework 4.8, and a .NET SDK. The package targets
**DataMiner 10.5.0 CU0 or later**. Uses the legacy `SLAnalyticsTypes` GQI API
and SLNetTypes 10.5.0.1, with caller-scoped DMS access supplied by `OnInit`.
No separate identity/security-key checks or alternate credentials are used;
server permission failures propagate. Runtime acceptance on CU0 is unverified.
This replaces the Core GQI API implementation; refresh existing queries and
Assistant registrations to use the legacy interface identity.

```powershell
dotnet build ElementTemplates.sln -c Release --warnaserror
dotnet test ElementTemplates.Tests\ElementTemplates.Tests.csproj -c Release --no-build
```

Install `ElementTemplates.Package\bin\Release\net48\DataMinerBuild\ElementTemplates.Package.2.1.1.dmapp`
on a compatible system, then select either source under **Get custom data**.
Install/upgrade also places both Assistant tools in
`%ProgramData%\Skyline Communications\DataMiner Assistant\Synced Documents\Context\Custom\Adhoc`,
where Assistant discovers and syncs them automatically. A running Assistant
supporting [custom data tools](https://github.com/SkylineCommunications/dataminer-docs/blob/main/dataminer/Functions/DataMiner_Intelligence/Intelligence_Assistant_custom_data_tools.md)
is required. Only these two owned files are overwritten; unrelated tools remain.
Installation failures are reported, not silently skipped. The SDK installer uses newer SLNet
references; installer and source runtime compatibility on CU0 remain unverified.
The tools use lowercase Assistant type identifiers and prefill `123/456` as
an example only; the actual GQI input remains required with no execution default.

Script/library identity: `ElementTemplates`. Classes:
`ElementTemplates.AlarmTemplateSource` and `ElementTemplates.TrendTemplateSource`.
Keep these names stable for existing queries and tools.

## Repository

- `ElementTemplates\`: SDK GQI source project.
- `ElementTemplates.Package\`: installer, Catalog information, and publish profile.
- `ElementTemplates.Tests\`: mapping, paging, permissions, and tool-contract tests.
- `Assistant\`: custom data-tool registrations.

See [Catalog information](ElementTemplates.Package/CatalogInformation/README.md) for the
item overview and [Getting started](ElementTemplates.Package/GettingStarted.md) for the
template's publishing instructions. Publishing requires separate authorization;
building does not publish or deploy.
