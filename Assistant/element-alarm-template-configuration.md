---
name: "element-alarm-template-configuration"
description: "Inspect alarm template rules and thresholds for one exact element. Monitored=true rows are configured in use; false rows are available but inactive. Includes repeated rules and unmerged group members. Does not calculate effective coverage."
dataSource: '{"DataSourceInterface":"Skyline.DataMiner.Analytics.GenericInterface.IGQIDataSource","ScriptName":"ElementTemplates","LibraryName":"ElementTemplates","TypeFullName":"ElementTemplates.AlarmTemplateSource","TypeName":"AlarmTemplateSource"}'
inputArguments:
  - name: "elementID"
    type: "string"
    description: "Exact element identifier in dmaID/elementID format, with two positive Int32 values. Never an element name."
    example: "123/456"
columns:
  - name: "RecordKind"
    type: "string"
    description: "Assignment, Template, GroupEntry, AlarmRule, or GroupProjectionRule. GroupProjectionRule is a returned group projection, not an original member rule."
  - name: "Status"
    type: "string"
    description: "NoAssignment, EmptyTemplate, Configured, or UnresolvedProjection. Configured is not a health or coverage verdict."
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
    description: "Assigned connector version or alias used to retrieve templates, including Production."
  - name: "AssignedTemplate"
    type: "string"
    description: "Element's alarm template or group assignment."
  - name: "TemplateName"
    type: "string"
    description: "Specific unmerged template supplying this record."
  - name: "TemplatePath"
    type: "string"
    description: "root or root/member-ordinal/...; retains group order and repeated member occurrences."
  - name: "RowOrdinal"
    type: "int"
    description: "Zero-based rule or group member position, never a parameter identity. Null on headers."
  - name: "ParameterID"
    type: "int"
    description: "Actual parameter ID from the rule, retained even if parameter metadata is absent."
  - name: "ParameterName"
    type: "string"
    description: "Connector parameter name when available, not a device-reported value."
  - name: "Units"
    type: "string"
    description: "Raw connector parameter units, not threshold normalization or evaluated dynamic units."
  - name: "Filter"
    type: "string"
    description: "Raw table row filter/instance expression; not expanded against live rows."
  - name: "EffectiveCoverage"
    type: "string"
    description: "Always NotEvaluated. Never treat configured rules as effective monitoring."
  - name: "RawConfiguration"
    type: "string"
    description: "JSON of the complete API rule, template header including conditions/schedule, or group member including IsEnabled/IsScheduled. No rows are filtered."
  - name: "Monitored"
    type: "boolean"
    description: "Row-level enablement: true means configured in use; false means available but currently not used. Null on headers. Not a separate inclusion-state or effective-coverage verdict."
  - name: "ThresholdType"
    type: "string"
    description: "Raw API Type encoding; no type or sentinel interpretation is invented."
  - name: "DisabledIf"
    type: "string"
    description: "Raw condition reference/expression. Definitions are in the same template's header JSON."
  - name: "CH"
    type: "string"
    description: "Raw critical-high threshold encoding; zero is valid."
  - name: "MaH"
    type: "string"
    description: "Raw major-high threshold encoding."
  - name: "MiH"
    type: "string"
    description: "Raw minor-high threshold encoding."
  - name: "WaH"
    type: "string"
    description: "Raw warning-high threshold encoding."
  - name: "Normal"
    type: "string"
    description: "Raw normal threshold encoding, preserving empty strings."
  - name: "WaL"
    type: "string"
    description: "Raw warning-low threshold encoding."
  - name: "MiL"
    type: "string"
    description: "Raw minor-low threshold encoding."
  - name: "MaL"
    type: "string"
    description: "Raw major-low threshold encoding."
  - name: "CL"
    type: "string"
    description: "Raw critical-low threshold encoding."
  - name: "Info"
    type: "string"
    description: "Raw information-message threshold encoding."
  - name: "Hysteresis"
    type: "string"
    description: "Raw hysteresis; additional per-severity and baseline configuration is in rule JSON."
---

# Interpretation

Inspect every page and distinguish `RecordKind` before interpreting thresholds.
For alarm rule records, only `Monitored = true` rows are configured in use.
Rows with `Monitored = false` can be used but are currently not in use; they are
retained so the Assistant can distinguish available rules from enabled rules.
Null flags on headers are not inactive rules.
Do not collapse rows by parameter ID or filter: duplicate rules, their order,
conditions, and group occurrence paths matter. Disabled group members and
unmonitored rules are intentionally retained. Group member enabled/scheduled
flags are in `GroupEntry` JSON; follow its `TemplatePath` to the child header and
rules. No group precedence, condition, schedule, row-filter, baseline, or live
device-reporting evaluation is performed. `Monitored` is raw API data; the API
does not expose a separate include/exclude field. The header records this as
`InclusionState: NotSeparatelyExposedByApi`. Do not infer three-state UI inclusion
or coverage from that flag alone.
These flags describe configured row-level enablement, not guaranteed effective
monitoring for a particular device instance.
If the group response itself contains rules, those are retained separately as
`GroupProjectionRule` with `UnresolvedProjection`; do not confuse them with
original `AlarmRule` records retrieved from individual member templates.

`NoAssignment` differs from an assigned `EmptyTemplate`. Exceptions are inspection
failures or unsupported responses, never healthy empty configurations.
Do not mutate templates, assignments, or parameters. Consumers may separately
suppress domain-specific findings for parameters a device does not report, but
must not discard this source's configured rows.
