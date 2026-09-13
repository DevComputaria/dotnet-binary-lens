---
applyTo: ".copilot-tracking/changes/20260913-clr-lens-development-backlog-changes.md"
---

<!-- markdownlint-disable-file -->

# Task Checklist: CLR Lens development backlog

## Overview

Transform the CLR Lens PRD into a dependency-ordered implementation backlog covering scaffolding, CIL analysis, mathematical modeling, reporting, contracts, verification and runtime calibration.

## Objectives

- Establish a compilable .NET solution and fixture strategy before advanced analysis.
- Implement the static-analysis foundation without executing analyzed assemblies.
- Deliver deterministic CFG, IR, call graph, findings and JSON/SARIF reporting.
- Add abstract interpretation, bounds, memory/GC modeling, pessimistic scenarios and contracts.
- Add conservative IL rewriting only after analyzer and report contracts are stable.
- Validate rewrites with ILVerify, SMT, differential tests and explicit release gates.

## Research Summary

### Project files

- `docs/prd/prd.md` — product requirements, CL-001 through CL-018, milestones and acceptance criteria.
- `docs/plan/development-plan.md` — architecture, phases, evidence model, contracts, risks and tests.
- `plan.md` — initial modules, CLI, optimizer limits and supported input constraints.
- `math.md` — abstract interpretation, CFG, Scalar Evolution, MemorySSA, CLR memory model and translation validation.

### Validated research

- `.copilot-tracking/research/20260913-clr-lens-development-research.md` — verified repository state, technical decisions, external implementation guidance and sequencing (Lines 1-123).

## Implementation Checklist

### [ ] Phase 1: Repository and solution foundation

- [ ] Task 1.1: Create the .NET solution and module boundaries.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 11-21)
- [ ] Task 1.2: Define domain contracts, evidence taxonomy, configuration and report schema.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 22-30)
- [ ] Task 1.3: Create fixture assemblies and test harness for supported/unsupported inputs.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 31-39)

### [ ] Phase 2: PE, metadata, CIL and IR

- [ ] Task 2.1: Implement safe PE/metadata ingestion and assembly capability detection.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 40-50)
- [ ] Task 2.2: Implement CIL decoding, evaluation-stack validation and exception regions.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 51-59)
- [ ] Task 2.3: Implement normalized typed IR with SSA-like values and provenance.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 60-68)

### [ ] Phase 3: CFG, call graph and structural findings

- [ ] Task 3.1: Build normal/exceptional CFG, dominators, SCCs and natural loops.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 69-79)
- [ ] Task 3.2: Build the call graph and unknown-effect classification.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 80-88)
- [ ] Task 3.3: Implement initial CPU, allocation and busy-loop findings.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 89-97)

### [ ] Phase 4: Mathematical analysis and memory model

- [ ] Task 4.1: Implement abstract domains, transfer functions and fixpoint engine.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 98-108)
- [ ] Task 4.2: Implement Scalar Evolution, trip counts and termination classifications.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 109-117)
- [ ] Task 4.3: Implement summaries, abstract heap, escape/retention and LOH modeling.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 118-126)

### [ ] Phase 5: Pessimistic analysis, contracts and reports

- [ ] Task 5.1: Implement typical/pessimistic/observed modes and input-bound propagation.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 127-137)
- [ ] Task 5.2: Implement contracts, suppression governance and CI policies.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 138-146)
- [ ] Task 5.3: Implement JSON, SARIF, Markdown and HTML reports with graphs.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 147-155)

### [ ] Phase 6: Conservative rewriting and validation

- [ ] Task 6.1: Implement opt-in rewrite candidates and proof objects.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 156-166)
- [ ] Task 6.2: Integrate ILVerify, strong-name/R2R policies and differential validation.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 167-175)
- [ ] Task 6.3: Add SMT translation validation and counterexample reporting.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 176-184)

### [ ] Phase 7: Runtime calibration and release hardening

- [ ] Task 7.1: Add EventPipe/profile overlay and benchmark A/B support.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 185-193)
- [ ] Task 7.2: Add performance, security, compatibility and release acceptance gates.
  - Details: `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` (Lines 194-204)

## Dependencies

- .NET SDK version selected during scaffolding.
- `System.Reflection.Metadata`/`PEReader`.
- Mono.Cecil for isolated rewriting.
- ILVerify for post-write validation.
- Z3 or equivalent SMT backend for proof phase.
- EventPipe tooling for optional runtime calibration.
- Test fixture assemblies and explicit dependency/reference resolution.

## Success Criteria

- The implementation backlog is traceable to PRD requirements and verified research.
- Each phase has concrete files, tests, dependencies and exit criteria.
- No advanced rewrite task is scheduled before IR, CFG, findings and report contracts are stable.
- Unknown inputs and external assumptions remain visible in every report mode.
- The implementation prompt can be executed phase by phase without inventing missing project conventions.
