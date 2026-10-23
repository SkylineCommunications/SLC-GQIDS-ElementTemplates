# Element template configuration sources

Read-only, connector-independent GQI sources and DataMiner Assistant custom
data tools for the alarm and trend templates assigned to one exact element.
No PTP dependency, vendor parameter map, current-alarm filter, or subscription
callback is required.

**Status:** implementation baseline with local build/contract evidence, not
runtime-accepted release software. Excluded-row completeness and independent
three-state alarm inclusion remain explicit API/host verification gates below.

## Sources and tools

Both sources belong to automation script **ElementTemplates**, compiled
library **ElementTemplates**. These identities are compatibility contracts.

| GQI display name | Full class name | Assistant tool |
| --- | --- | --- |
| Element alarm template configuration | `ElementTemplates.AlarmTemplateSource` | `element-alarm-template-configuration` |
| Element trend template configuration | `ElementTemplates.TrendTemplateSource` | `element-trend-template-configuration` |

The interface is `Skyline.DataMiner.Core.GQI.Extensions.IGQIDataSource`.
The files in `Assistant\` are actual Markdown/YAML custom data-tool definitions,
not prompts or native-resource placeholders.

Both sources require **dmaID** and **elementID**, positive `Int32` arguments.
For example, `dmaID=12`, `elementID=34` identifies exactly `12/34`.
Names, view IDs, and fallback elements are not accepted.

## Supported target and API evidence

The official `Skyline.DataMiner.VisualStudioTemplates` **2.8.15**
`dataminer-gqi-ad-hoc-data-source-project` template was scaffolded and built
before implementation. Its legacy references were replaced with the modern
Core API. The project retains `DataMinerType=AdHocDataSource`, `net48`, and
the generated library XML with `preCompile=true` and a dot-free library name.
The platform is explicitly **x64**; the template's implicit platform must
not be relied on for the GQI DxM.

Versions selected and locally compiled:

- `Skyline.DataMiner.Sdk` **2.6.1**, pinned in `global.json`.
- `Skyline.DataMiner.Core.GQI.Extensions` **1.6.0**.
- `Skyline.DataMiner.Files.SLNetTypes` **10.6.10.1**.
- `Newtonsoft.Json` **13.0.3**.

The current published [Core API compatibility table][core-package] specifies
DataMiner **10.6.12 [CU0]**, **10.6.0 [CU9]**, or **10.5.0 [CU21]**, or the
corresponding DataMiner Web version, for Core API 1.6.0. This package
conservatively declares **10.6.12.0** as its minimum DataMiner version.
The other branches/web-only combinations are not enabled by this manifest
or tested here. **The GQI DxM must be enabled.** No legacy SLAnalyticsTypes
implementation is provided.

The latest published Core API package/table was used, not an inferred
capability level from an older test host. Package availability does not
prove the target Agent runs a compatible release.

Typed members verified against the selected NuGet packages:

| Operation | Request/response |
| --- | --- |
| Caller connection | Injected `IGQIDMSInterface.GetConnection()`, verified against `IGQIUser.Username` |
| Caller permissions | `GetInfoMessage(InfoType.ClientSecurity)` / `ClientSecurityChangeEventMessage`; `ClientSecurity.IsAllowedOnElement` and `CheckPermissions` |
| Exact element and assignments | `GetElementByIDMessage(dmaID, elementID)` / `ElementInfoEventMessage`; `ProtocolTemplate`, `Trending`, `Protocol`, `ProtocolVersion` |
| Connector version/metadata | `GetProtocolMessage` / `GetProtocolInfoResponseMessage`; `AllParameters`, parameter `Name` and `Units` |
| Complete alarm response | `GetAlarmTemplateMessage` with `AsOneObject=true` / `AlarmTemplateEventMessage.TemplateParams`, `Conditions`, `Schedule`, `GroupEntries` |
| Trend response | `GetTrendingTemplateInfoMessage` / `GetTrendingTemplateInfoResponseMessage.Parameters` (`ParamTrendingInfo`) |

The official `AlarmTemplateHelper` uses the same one-object alarm retrieval
contract, but its selected-row helpers are deliberately **not** used.
Selection/deduplication would not satisfy this source's contract.
SLNet messages are [version-sensitive][dms-api]. Strongly typed compilation
proves the package surface, not server behavior on arbitrary releases.
Documentation was checked on **2026-10-23** against the official documentation
repository at revision
[`0995299b1a4ae8657876a0639e578d012d41ff2c`](https://github.com/SkylineCommunications/dataminer-docs/tree/0995299b1a4ae8657876a0639e578d012d41ff2c).

Official references:

- [Core versus legacy GQI API][core-api] and [ad hoc discovery identity][creation].
- [Constructor-injected DMS access][dms-interface] and [user context][user-context].
- [Assistant custom data-tool files][assistant-docs].
- [Alarm groups, precedence, and three UI inclusion states][groups].
- [Conditions and ordered rules][conditions].
- [Trend filters, repeated rows, and ordering][trends].
- [Raw alarm threshold fields][alarm-fields].

## Build and package

Use Windows with the .NET SDK (verified locally with **10.0.401**) and .NET
Framework 4.8 targeting/runtime support. Restore needs nuget.org access.
No DataMiner installation, credentials, or manually copied DataMiner
assemblies are needed for the local build.

```powershell
dotnet build ElementTemplates.sln -c Release --warnaserror
dotnet test tests\ElementTemplates.Tests\ElementTemplates.Tests.csproj -c Release --no-build --verbosity minimal
```

The SDK generates:

```text
src\ElementTemplates\bin\Release\net48\DataMinerBuild\ElementTemplates.1.0.0.dmapp
```

The package contains the library script and its NuGet dependency references,
the Core GQI and JSON dependency files, and the SDK-generated default
installer. It does not contain tests or a provisioning script. Installing it
registers read-only source code. Querying it does not change templates,
assignments, parameters, or device state. Package installation itself is an
administrative change and is **not** performed by the tests.

Assistant files are distributed separately in `Assistant\`; the `.dmapp`
does not write the Assistant document store. This avoids a custom privileged
installer or hard-coded Assistant service paths.
No Catalog upload or deployment is required to build.

## Setup on a compatible system

1. Have an authorized administrator install the generated `.dmapp`, or use
   Visual Studio with DIS for development publishing. Do not manually copy
   dependency assemblies into a running Agent.
2. In a GQI query, select **Get custom data** (or **Get ad hoc data**) and the
   required source display name. Supply `dmaID` and `elementID`.
3. Verify discovery, initial fetch, multiple pages, and permissions using a
   known element in the actual target system.
4. Copy both `Assistant\*.md` files to the documented Assistant host folder:
   `C:\ProgramData\Skyline Communications\DataMiner Assistant\Synced Documents\Context\Custom\Adhoc`.
5. Verify the tools appear in the Assistant app and grant the intended agent
   access through normal Assistant configuration.
6. Run each tool for the same element and compare its complete result with
   the template editor, including group members, repeated/disabled rules,
   filters, conditions, and schedules.

The Assistant version must support the documented custom ad hoc data-tool
format and deployed Core GQI identity. No Assistant version/build or live
target was supplied or exercised. Registration acceptance remains a host
check, not an implication of the YAML tests.

## Caller permissions and failure behavior

All reads use the injected caller-scoped DMS connection. Its
`UserDomainName` must match `IGQIUser.Username` case-insensitively;
an unverifiable/mismatched identity fails closed. The caller's security key
is requested through that connection, never supplied as a credential or
replaced by a process-wide/elevated connection. The server's empty-key
semantics are left to the official `ClientSecurity` implementation.

The source explicitly checks element visibility and request-type permissions
before reading the exact element, connector, and templates. The caller needs
visibility of the element, permission to run the GQI source, and access to
the corresponding connector/template reads. No broad administrative role or
template-edit permission is requested. Server authorization errors propagate.
Role names and the exact permission combination need target verification.

Template data can contain rules for parameters the caller cannot read as
live values. This tool inspects the permitted **template definition**, not
parameter values, and does not claim that parameter-value access was granted.
Do not grant these tools to users who should not inspect their templates.
No element connection strings, credentials, or complete element metadata
are serialized.

| Situation | Result |
| --- | --- |
| Null/empty assignment on the resolved element | One `Assignment` record with `Status=NoAssignment` |
| Assigned, inspectable definition with no rules/members | One `Template` record with `Status=EmptyTemplate` |
| Normal template/rule/member | `Status=Configured`, never a health verdict |
| Rules returned on a group object itself | `GroupProjectionRule`, `Status=UnresolvedProjection` |
| Invalid IDs | Argument exception |
| Caller mismatch or denied element | Authorization exception, no template reads |
| Element missing, deleted, or different identity | Query fails; never represented as `NoAssignment` |
| Unexpected/incomplete response, unknown type, template identity mismatch, cyclic/overdeep group | `NotSupportedException` |
| Timeout, server authorization error, other retrieval failure | Original exception propagates |

Inspection exceptions are logged through the GQI logger and rethrown.
No exception becomes an empty page or healthy status. Once a fetch fails,
subsequent page requests fail until a new fetch is prepared. Member retrieval
failures fail the whole initial fetch rather than returning partial rows.

## Output contract

Column names, types, and order are checked against both Assistant files.
The alarm source has **32** columns; the trend source has **20**.
Metadata records have null parameter/rule-specific cells; null and empty
strings are not interchangeable.

### Common columns

| Column | Type | Meaning |
| --- | --- | --- |
| RecordKind | String | `Assignment`, `Template`, `GroupEntry`, `AlarmRule`, `GroupProjectionRule`, or `TrendRule`, as applicable |
| Status | String | Explicit inspection state described above |
| dmaID / elementID | Int32 | Exact requested and resolved identity |
| ElementName | String | Element name, for display only |
| Protocol | String | Element connector name |
| ElementProtocolVersion | String | Version configured on the element, possibly `Production` |
| TemplateProtocolVersion | String | Version resolved by the connector API and used for template requests |
| AssignedTemplate | String | Original element assignment, not replaced by a child name |
| TemplateName | String | Definition supplying this record |
| TemplatePath | String | `root`, `root/0`, `root/0/1`, etc.; preserves group occurrence/order |
| RowOrdinal | Int32 | Zero-based rule/member position, null on headers |
| ParameterID | Int32 | Actual configured ID, retained even without metadata |
| ParameterName / Units | String | Connector metadata when available, not guessed or dynamically evaluated |
| Filter | String | Raw rule filter/instance expression, including empty/null |
| EffectiveCoverage | String | Always `NotEvaluated` |
| RawConfiguration | String | JSON of the complete API rule, header, or group member |

Keys include element IDs, template kind, occurrence path, record kind, and
ordinal. Repeated rules and group members remain distinct even when their
parameter IDs and filters are identical.

### Alarm-only columns

`Monitored` is the unmodified API Boolean. `ThresholdType` is raw API `Type`;
`DisabledIf` is the raw condition reference/expression. The remaining columns
are strings copied without conversion:
`CH`, `MaH`, `MiH`, `WaH`, `Normal`, `WaL`, `MiL`, `MaL`, `CL`, `Info`, and
`Hysteresis`.

Zero, negative values, discrete lists, baseline/relative encodings, and
empty/null thresholds are preserved. No parsing, unit conversion, sentinel
interpretation, or threshold synthesis is performed. Full per-severity
hysteresis, baseline, anomaly configuration, auto-clear, and other returned
fields remain in each rule's `RawConfiguration`.

Template header JSON preserves description, type, interpretation version,
conditions, schedule, schedule enablement, and ordered group members.
`GroupEntry` JSON includes `Name`, `IsEnabled`, and `IsScheduled`.
Every member is inspected, including disabled and scheduled members.
Nested occurrences retain their path. Cycles and nesting beyond 128 fail
explicitly instead of returning truncated success.

### Trend-only columns

`RealTimeTrended` and `AverageTrended` are independent Booleans on each
`TrendRule`. Both false is a valid retained rule.
Full rule JSON also preserves central-database flags and nullable anomaly,
proactive, and trend-icon settings. Duplicate IDs, filters, and order are
preserved without consulting current values/history.

## Configured rows are not effective coverage

The source returns **every row supplied by the complete template API
collections**, without deduplication, monitored-only filtering, or reliance
on active alarms/current values. It does not synthesize rows for connector
parameters absent from the template response.

The selected alarm API exposes `Monitored` but **no independent include/exclude
field**. It cannot unambiguously reconstruct all three UI states
(included+monitored, included+not monitored, excluded) from that flag alone.
Headers explicitly contain
`InclusionState="NotSeparatelyExposedByApi"`; raw flags/encodings are retained
without invented semantics. Verification that a server returns all configured
excluded rows, and a richer contract for any excluded rows it omits, remain
**release gates**. Do not claim complete UI-state fidelity from mocked tests.

Groups are traversed, not flattened into a coverage verdict. Earlier members
override later members, but conditions and schedules affect which rules apply.
Rules returned directly on a group may be a consolidated projection: they
remain `GroupProjectionRule` records, distinct from member `AlarmRule` records.
No precedence resolution is claimed.

Other unresolved aspects include live condition truth, schedule activation
and time zones, table display-key versus primary-key matching, dynamic
baselines/units, and whether the device reports a parameter. Explicit groups
are supported; implicit inheritance or server-side transformations not
represented by this API are not resolved. Reads form a per-fetch snapshot,
**not an atomic server transaction**. Concurrent assignment/template edits can
affect different reads; refresh after edits.

A domain agent can separately suppress findings for parameters its device
does not report, but this source never hides disabled, excluded-as-returned,
or unreported rules.

## Verification boundary

Local tests cover complete rule arrays and raw thresholds, duplicate/filtered/
unmonitored rules, ordered and disabled group members, conditions/schedule
metadata, empty/missing assignments, authorization/retrieval failures,
unsupported responses, cycles, paging/reset, and instance isolation.
They also parse actual Assistant YAML/JSON and compare source identities,
required inputs, and every column/type with compiled classes.
The Release build completed with zero warnings/errors and all **15** local
tests passed using the commands above.

These are **mocked contract tests**, not a standalone GQI host. They do not
prove discovery, constructor injection, SLNet server semantics, excluded-row
completeness, real permission enforcement, group projection behavior, or
Assistant registration/execution on a deployed system.
No live deployment, template/device writes, or host acceptance checks were
performed or authorized.

[core-package]: https://www.nuget.org/packages/Skyline.DataMiner.Core.GQI.Extensions/1.6.0
[core-api]: https://docs.dataminer.services/develop/devguide/GQI_Extensions/GQI_Extension_API.html
[creation]: https://docs.dataminer.services/develop/devguide/GQI_Extensions/Ad_hoc_data/Ad_hoc_Creation.html
[dms-api]: https://docs.dataminer.services/develop/devguide/GQI_Extensions/API_Reference/GQIDMS.html
[dms-interface]: https://docs.dataminer.services/develop/devguide/GQI_Extensions/API_Reference/IGQIDMSInterface.html
[user-context]: https://docs.dataminer.services/develop/devguide/GQI_Extensions/API_Reference/IGQIUser.html
[assistant-docs]: https://docs.dataminer.services/dataminer/Functions/DataMiner_Intelligence/Intelligence_Assistant_custom_data_tools.html
[groups]: https://docs.dataminer.services/dataminer/Operator_guide/Protocols_and_templates/Alarm_templates/Alarm_template_groups.html
[conditions]: https://docs.dataminer.services/dataminer/Operator_guide/Protocols_and_templates/Alarm_templates/Configuring_alarm_templates/Using_conditions_in_an_alarm_template.html
[trends]: https://docs.dataminer.services/dataminer/Operator_guide/Protocols_and_templates/Trend_templates/Configuring_trend_templates.html
[alarm-fields]: https://docs.dataminer.services/develop/webservices/WS_v1/WS_CustomDataTypes_v1/DMAAlarmTemplateParameter.html
