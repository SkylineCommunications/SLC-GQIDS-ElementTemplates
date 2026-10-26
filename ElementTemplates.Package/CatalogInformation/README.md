# Element Template Configuration

## About

Inspect the alarm and trend template configuration assigned to an element
without changing its templates or device parameters. Use the returned rules
to review configuration in GQI queries or through DataMiner Assistant tools.

## Key Features

- Inspect raw alarm thresholds, row filters, conditions, and schedules.
- Retain repeated rules and the provenance of ordered alarm group members.
- Review real-time and average trending settings independently.
- Distinguish missing assignments and empty definitions from retrieval failures.
- Install the matching Assistant data tools automatically with the sources.

## Prerequisites

- DataMiner 10.5.0 CU0 or later (legacy SLAnalyticsTypes GQI API).
- Caller access to the element and its connector/template configuration.
- A compatible DataMiner Assistant version for optional custom data tools.

## Technical Reference

**Preview limitation:** effective coverage is not evaluated. Independent alarm
include/exclude state and excluded-row completeness are not verified; the
full all-configured-rows requirement remains blocked. One alarm query was
verified in Dashboards on DataMiner 10.6.10. Trend, Assistant, and CU0 runtime
acceptance remain unverified.

Both sources take one string input, `elementID`, in `dmaID/elementID` format
(for example, `123/456`).

The package installs two read-only GQI sources and their Assistant registrations.
Install and upgrade overwrite only these two owned tool files, retaining unrelated
tools; Assistant discovers and synchronizes them automatically. Assistant itself
is a separate prerequisite. No template assignments or device parameters change.

Only alarm rules with `Monitored = true` are configured in use. Trend rules
are enabled when `RealTimeTrended = true` or `AverageTrended = true`; false/false
rows can be trended but are currently inactive. All returned rules remain visible.
These row-level flags do not resolve effective coverage, filters, or history.
See the repository README and tool files for inputs and output contracts.
