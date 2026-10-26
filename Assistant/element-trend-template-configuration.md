---
name: "element-trend-template-configuration"
description: "Inspect trend template parameter/row rules for one exact element. RealTimeTrended=true or AverageTrended=true enables trending; false/false rows can be trended but currently are not. Retains duplicate and filtered rules. Not proof of available history."
dataSource: '{"DataSourceInterface":"Skyline.DataMiner.Analytics.GenericInterface.IGQIDataSource","ScriptName":"ElementTemplates","LibraryName":"ElementTemplates","TypeFullName":"ElementTemplates.TrendTemplateSource","TypeName":"TrendTemplateSource"}'
inputArguments:
  - name: "elementID"
    type: "String"
    description: "Exact element identifier in dmaID/elementID format, with two positive Int32 values. Never an element name."
    example: "123/456"
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
    description: "Assigned connector version or alias used to retrieve the template, including Production."
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
    description: "True enables real-time trending on this rule. Trending is enabled when this or AverageTrended is true; false/false is inactive. Null on headers."
  - name: "AverageTrended"
    type: "Boolean"
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
