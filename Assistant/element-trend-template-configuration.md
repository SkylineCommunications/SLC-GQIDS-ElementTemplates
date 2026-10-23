---
name: "element-trend-template-configuration"
description: "Inspect every configured trend template parameter/row rule for one exact element, including duplicate, filtered, and disabled rules with separate real-time and average enablement. Not a live trend-coverage verdict."
dataSource: '{"DataSourceInterface":"Skyline.DataMiner.Core.GQI.Extensions.IGQIDataSource","ScriptName":"ElementTemplates","LibraryName":"ElementTemplates","TypeFullName":"ElementTemplates.TrendTemplateSource","TypeName":"TrendTemplateSource"}'
inputArguments:
  - name: "dmaID"
    type: "Int32"
    description: "Positive DataMiner ID of the exact element. Use together with elementID."
    example: "1"
  - name: "elementID"
    type: "Int32"
    description: "Positive element ID within dmaID."
    example: "1"
columns:
  - name: "RecordKind"
    type: "String"
    description: "Assignment, Template, or TrendRule."
  - name: "Status"
    type: "String"
    description: "NoAssignment, EmptyTemplate, or Configured; not a health verdict."
  - name: "dmaID"
    type: "Int32"
  - name: "elementID"
    type: "Int32"
  - name: "ElementName"
    type: "String"
  - name: "Protocol"
    type: "String"
    description: "Connector name from the exact element."
  - name: "ElementProtocolVersion"
    type: "String"
    description: "Connector version configured on the element, possibly Production."
  - name: "TemplateProtocolVersion"
    type: "String"
    description: "Resolved connector version used to retrieve the template."
  - name: "AssignedTemplate"
    type: "String"
  - name: "TemplateName"
    type: "String"
  - name: "TemplatePath"
    type: "String"
    description: "root; trend template inheritance/grouping is not inferred."
  - name: "RowOrdinal"
    type: "Int32"
    description: "Zero-based rule position, null on headers. Filter order matters."
  - name: "ParameterID"
    type: "Int32"
    description: "Actual configured parameter ID, including parameters without metadata/current values."
  - name: "ParameterName"
    type: "String"
    description: "Connector parameter name when available."
  - name: "Units"
    type: "String"
    description: "Raw connector units, not evaluated dynamic units."
  - name: "Filter"
    type: "String"
    description: "Unexpanded row filter/instance expression."
  - name: "EffectiveCoverage"
    type: "String"
    description: "Always NotEvaluated; enablement does not prove available history or matching live instances."
  - name: "RawConfiguration"
    type: "String"
    description: "JSON containing the complete API trend rule, including central-database and anomaly flags, or template header."
  - name: "RealTimeTrended"
    type: "Boolean"
    description: "Configured real-time enablement on this specific rule; null on headers."
  - name: "AverageTrended"
    type: "Boolean"
    description: "Configured average enablement on this specific rule; null on headers."
---

# Interpretation

Retrieve all pages. Retain every `TrendRule`, including false/false rules and
duplicate parameter IDs or filters. Preserve `RowOrdinal`: row filters are
ordered and not expanded against live device instances. `NoAssignment` and an
assigned `EmptyTemplate` have different meanings. Errors or unsupported
responses are not successful empty results. Neither enablement flag proves
that a device reports the parameter or that any history exists.

This tool is read-only. Do not change trending, assignments, or device values.
Parameter-specific device-reporting checks belong to the consuming agent, not
to this generic configuration source.
