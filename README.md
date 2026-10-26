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

See the [tool contracts](Assistant), [Catalog information](ElementTemplates.Package/CatalogInformation/README.md),
and [publishing guide](ElementTemplates.Package/GettingStarted.md).
