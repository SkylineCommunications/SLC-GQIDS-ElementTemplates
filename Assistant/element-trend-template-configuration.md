---
name: "element-trend-template-configuration"
description: "Inspect trend template parameter/row rules for one exact element. RealTimeTrended=true or AverageTrended=true enables trending; false/false rows can be trended but currently are not. Retains duplicate and filtered rules. Not proof of available history."
dataSource: '{"DataSourceInterface":"Skyline.DataMiner.Analytics.GenericInterface.IGQIDataSource","ScriptName":"ElementTemplates","LibraryName":"ElementTemplates","TypeFullName":"ElementTemplates.TrendTemplateSource","TypeName":"TrendTemplateSource"}'
inputArguments:
  - name: "elementID"
    type: "string"
    description: "Exact element identifier in dmaID/elementID format, with two positive Int32 values. Never an element name."
    example: "123/456"
columns:
  - name: "RecordKind"
    type: "string"
    description: "Assignment, Template, or TrendRule."
  - name: "Status"
    type: "string"
    description: "NoAssignment, EmptyTemplate, or Configured; not a health verdict."
  - name: "dmaID"
    type: "int"
  - name: "elementID"
    type: "int"
  - name: "ElementName"
    type: "string"
  - name: "Protocol"
    type: "string"
    description: "Connector name from the exact element."
  - name: "ElementProtocolVersion"
    type: "string"
    description: "Connector version configured on the element, possibly Production."
  - name: "TemplateProtocolVersion"
    type: "string"
    description: "Assigned connector version or alias used to retrieve the template, including Production."
  - name: "AssignedTemplate"
    type: "string"
  - name: "TemplateName"
    type: "string"
  - name: "TemplatePath"
    type: "string"
    description: "root; trend template inheritance/grouping is not inferred."
  - name: "RowOrdinal"
    type: "int"
    description: "Zero-based rule position, null on headers. Filter order matters."
  - name: "ParameterID"
    type: "int"
    description: "Actual configured parameter ID, including parameters without metadata/current values."
  - name: "ParameterName"
    type: "string"
    description: "Connector parameter name when available."
  - name: "Units"
    type: "string"
    description: "Raw connector units, not evaluated dynamic units."
  - name: "Filter"
    type: "string"
    description: "Unexpanded row filter/instance expression."
  - name: "EffectiveCoverage"
    type: "string"
    description: "Always NotEvaluated; enablement does not prove available history or matching live instances."
  - name: "RawConfiguration"
    type: "string"
    description: "JSON containing the complete API trend rule, including central-database and anomaly flags, or template header."
  - name: "RealTimeTrended"
    type: "boolean"
    description: "True enables real-time trending on this rule. Trending is enabled when this or AverageTrended is true; false/false is inactive. Null on headers."
  - name: "AverageTrended"
    type: "boolean"
    description: "True enables average trending on this rule. Trending is enabled when this or RealTimeTrended is true; false/false is inactive. Null on headers."
---

# Interpretation

Only `TrendRule` rows with `RealTimeTrended = true` OR `AverageTrended = true`
are configured for trending. Parameters on false/false rows can be trended but
currently are not being trended by that rule. Null flags on headers are not
false/false rules. Interpret each filtered or repeated rule separately rather
than collapsing it to a parameter-wide verdict.

Retrieve all pages. Retain every `TrendRule`, including false/false rules and
duplicate parameter IDs or filters. Preserve `RowOrdinal`: row filters are
ordered and not expanded against live device instances. `NoAssignment` and an
assigned `EmptyTemplate` have different meanings. Errors or unsupported
responses are not successful empty results. Neither enablement flag proves
that a device reports the parameter or that any history exists.

This tool is read-only. Do not change trending, assignments, or device values.
Parameter-specific device-reporting checks belong to the consuming agent, not
to this generic configuration source.
