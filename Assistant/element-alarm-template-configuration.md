---
name: "element-alarm-template-configuration"
description: "Inspect all configured alarm template rules and thresholds for one exact element, including repeated and disabled rules and unmerged alarm group members. This does not calculate effective alarm coverage."
dataSource: '{"DataSourceInterface":"Skyline.DataMiner.Core.GQI.Extensions.IGQIDataSource","ScriptName":"ElementTemplates","LibraryName":"ElementTemplates","TypeFullName":"ElementTemplates.AlarmTemplateSource","TypeName":"AlarmTemplateSource"}'
inputArguments:
  - name: "dmaID"
    type: "Int32"
    description: "Positive DataMiner ID of the exact element. Use together with elementID, never an element name."
    example: "1"
  - name: "elementID"
    type: "Int32"
    description: "Positive element ID within dmaID."
    example: "1"
columns:
  - name: "RecordKind"
    type: "String"
    description: "Assignment, Template, GroupEntry, AlarmRule, or GroupProjectionRule. GroupProjectionRule is a returned group projection, not an original member rule."
  - name: "Status"
    type: "String"
    description: "NoAssignment, EmptyTemplate, Configured, or UnresolvedProjection. Configured is not a health or coverage verdict."
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
    description: "Resolved connector version used to retrieve templates."
  - name: "AssignedTemplate"
    type: "String"
    description: "Element's alarm template or group assignment."
  - name: "TemplateName"
    type: "String"
    description: "Specific unmerged template supplying this record."
  - name: "TemplatePath"
    type: "String"
    description: "root or root/member-ordinal/...; retains group order and repeated member occurrences."
  - name: "RowOrdinal"
    type: "Int32"
    description: "Zero-based rule or group member position, never a parameter identity. Null on headers."
  - name: "ParameterID"
    type: "Int32"
    description: "Actual parameter ID from the rule, retained even if parameter metadata is absent."
  - name: "ParameterName"
    type: "String"
    description: "Connector parameter name when available, not a device-reported value."
  - name: "Units"
    type: "String"
    description: "Raw connector parameter units, not threshold normalization or evaluated dynamic units."
  - name: "Filter"
    type: "String"
    description: "Raw table row filter/instance expression; not expanded against live rows."
  - name: "EffectiveCoverage"
    type: "String"
    description: "Always NotEvaluated. Never treat configured rules as effective monitoring."
  - name: "RawConfiguration"
    type: "String"
    description: "JSON of the complete API rule, template header including conditions/schedule, or group member including IsEnabled/IsScheduled. No rows are filtered."
  - name: "Monitored"
    type: "Boolean"
    description: "Unmodified API Monitored flag. Not an independent inclusion-state or effective-coverage verdict."
  - name: "ThresholdType"
    type: "String"
    description: "Raw API Type encoding; no type or sentinel interpretation is invented."
  - name: "DisabledIf"
    type: "String"
    description: "Raw condition reference/expression. Definitions are in the same template's header JSON."
  - name: "CH"
    type: "String"
    description: "Raw critical-high threshold encoding; zero is valid."
  - name: "MaH"
    type: "String"
    description: "Raw major-high threshold encoding."
  - name: "MiH"
    type: "String"
    description: "Raw minor-high threshold encoding."
  - name: "WaH"
    type: "String"
    description: "Raw warning-high threshold encoding."
  - name: "Normal"
    type: "String"
    description: "Raw normal threshold encoding, preserving empty strings."
  - name: "WaL"
    type: "String"
    description: "Raw warning-low threshold encoding."
  - name: "MiL"
    type: "String"
    description: "Raw minor-low threshold encoding."
  - name: "MaL"
    type: "String"
    description: "Raw major-low threshold encoding."
  - name: "CL"
    type: "String"
    description: "Raw critical-low threshold encoding."
  - name: "Info"
    type: "String"
    description: "Raw information-message threshold encoding."
  - name: "Hysteresis"
    type: "String"
    description: "Raw hysteresis; additional per-severity and baseline configuration is in rule JSON."
---

# Interpretation

Inspect every page and distinguish `RecordKind` before interpreting thresholds.
Do not collapse rows by parameter ID or filter: duplicate rules, their order,
conditions, and group occurrence paths matter. Disabled group members and
unmonitored rules are intentionally retained. Group member enabled/scheduled
flags are in `GroupEntry` JSON; follow its `TemplatePath` to the child header and
rules. No group precedence, condition, schedule, row-filter, baseline, or live
device-reporting evaluation is performed. `Monitored` is raw API data; the API
does not expose a separate include/exclude field. The header records this as
`InclusionState: NotSeparatelyExposedByApi`. Do not infer three-state UI inclusion
or coverage from that flag alone.
If the group response itself contains rules, those are retained separately as
`GroupProjectionRule` with `UnresolvedProjection`; do not confuse them with
original `AlarmRule` records retrieved from individual member templates.

`NoAssignment` differs from an assigned `EmptyTemplate`. Exceptions are inspection
failures or unsupported responses, never healthy empty configurations.
Do not mutate templates, assignments, or parameters. Consumers may separately
suppress domain-specific findings for parameters a device does not report, but
must not discard this source's configured rows.
