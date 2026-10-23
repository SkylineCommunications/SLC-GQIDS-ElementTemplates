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

## Prerequisites

- DataMiner 10.6.12.0 or later with the GQI DxM enabled.
- Caller access to the element and its connector/template configuration.
- A compatible DataMiner Assistant version for optional custom data tools.

## Technical Reference

**Preview limitation:** effective coverage is not evaluated. Independent alarm
include/exclude state and excluded-row completeness are not verified; the
full all-configured-rows requirement remains blocked. Runtime acceptance has
not been performed.

The package installs two read-only GQI sources. Assistant registrations are
provided separately in the source repository. See the repository README and
tool files for setup, inputs, and output contracts.
