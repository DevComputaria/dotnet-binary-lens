---
mode: agent
model: Claude Sonnet 4
---

<!-- markdownlint-disable-file -->

# Implementation Prompt: CLR Lens development backlog

## Task Overview

Implement the CLR Lens product from the dependency-ordered checklist in `.copilot-tracking/plans/20260913-clr-lens-development-backlog-plan.instructions.md`, using the detailed specifications in `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md` and the product requirements in `docs/prd/prd.md`.

The implementation must prioritize static analysis and reporting. Rewriting is opt-in and must not begin until the IR, CFG, findings and report contracts are stable.

## Implementation Instructions

### Step 1: Create changes tracking

Create `.copilot-tracking/changes/20260913-clr-lens-development-backlog-changes.md` if it does not exist. Record each completed task, files changed, tests executed, assumptions and unresolved decisions.

### Step 2: Establish the foundation

1. Select and document the .NET SDK and target framework.
2. Create the solution and project boundaries from the plan.
3. Add build, formatting, analyzer and test configuration.
4. Define core models for provenance, evidence, findings, scenarios, contracts and suppressions.
5. Add JSON schema validation and deterministic serialization.
6. Create fixture assemblies before implementing advanced analysis.

### Step 3: Implement static ingestion and IR

1. Implement read-only PE/metadata ingestion with hashing, limits and capability detection.
2. Decode method bodies, evaluation stacks, signatures, branches and exception regions.
3. Build a project-owned typed IR with source offsets and effect flags.
4. Add tests for malformed inputs, invalid stack states, EH, generic signatures and unsupported formats.

### Step 4: Implement structural analysis

1. Build normal and exceptional CFGs.
2. Compute dominators, post-dominators, SCCs, backedges and natural loops.
3. Build the call graph and classify unknown effects.
4. Implement initial CPU, allocation, busy-loop and nested-traversal findings.
5. Make each finding traceable to IL evidence and a report contract.

### Step 5: Implement formal and mathematical analysis

1. Implement abstract domains and transfer functions.
2. Add fixpoint iteration, widening, narrowing and convergence limits.
3. Add Scalar Evolution, trip counts, overflow conditions and termination statuses.
4. Add versioned method summaries.
5. Add abstract heap, escape, retention, live memory, allocation volume and LOH modeling.
6. Keep unknown external methods and values explicitly conservative.

### Step 6: Implement pessimistic analysis and governance

1. Add `typical`, `pessimistic` and `observed` modes.
2. Propagate external input symbols and bounds.
3. Implement worst-case branch and unbounded-loop reporting.
4. Implement `MEM012` for unbounded I/O materialization.
5. Add baseline, stress and untrusted scenarios.
6. Implement contracts, expiration checks and suppression governance.
7. Ensure suppressions never convert `UNKNOWN` into `PROVEN`.

### Step 7: Implement reports and CLI

1. Implement deterministic JSON and schema validation.
2. Implement SARIF with rule metadata and suppression information.
3. Implement Markdown and HTML reports with executive summary, findings, formulas, evidence badges, CFG and call graph.
4. Implement CLI commands for analyze, inspect, graph, verify, diff, optimize and profile as each capability becomes available.
5. Add CI policy options for severity, unbounded input, expired contracts and required suppression metadata.

### Step 8: Implement rewriting only after analyzer gates pass

1. Add opt-in rewrite candidates for constant folding, branch folding, unreachable block removal, NOP cleanup and redundant locals.
2. Generate IL diffs and proof objects.
3. Preserve metadata, EH, stack validity, public API and observable effects.
4. Detect strong names and require explicit reassignment configuration.
5. Detect R2R and block unsafe rewriting without rebuild/strip policy.

### Step 9: Verify and calibrate

1. Run ILVerify on every rewritten output.
2. Add SMT translation validation for supported transformations.
3. Produce counterexamples for rejected transformations and `UNKNOWN` for timeouts.
4. Add differential tests for returns, exceptions and modelable side effects.
5. Add EventPipe/nettrace profile overlays and separate benchmark A/B reporting.
6. Never present runtime benchmark improvement as semantic proof.

### Step 10: Quality and release gates

For every phase:

- run unit, integration, fixture, regression and contract tests;
- run static analyzers and formatting checks;
- check resource limits and malformed input behavior;
- verify determinism;
- update the changes tracking file;
- record research or design decisions when evidence changes;
- stop before the next phase when `${input:phaseStop:true}` is true;
- stop before the next task when `${input:taskStop:false}` is true.

## Success Criteria

- [ ] All phases in the plan are completed in dependency order.
- [ ] PRD requirements CL-001 through CL-018 are traced to implementation and tests.
- [ ] Analyzed assemblies are never executed by the static analyzer.
- [ ] JSON and SARIF outputs are deterministic and schema-valid.
- [ ] Findings distinguish proof, bounds, estimates, observations, contracts and unknowns.
- [ ] Pessimistic scenarios and suppression governance are auditable.
- [ ] Rewriting is opt-in, preserves the original input and passes ILVerify.
- [ ] SMT timeouts and missing summaries produce `UNKNOWN`, never false proof.
- [ ] Runtime calibration is separated from static analysis and semantic validation.
- [ ] The changes tracking file is current and references tests and release evidence.
