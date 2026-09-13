<!-- markdownlint-disable-file -->

# Task Details: CLR Lens development backlog

## Research Reference

**Source Research**: `.copilot-tracking/research/20260913-clr-lens-development-research.md`

**Product Reference**: `docs/prd/prd.md`

## Phase 1: Repository and solution foundation

### Task 1.1: Create the .NET solution and module boundaries

Create the solution and project boundaries described by the PRD without implementing advanced analysis prematurely.

- **Files:** `src/ClrLens.*/*.csproj`, `tests/ClrLens.Tests.*/*.csproj`, solution file, `Directory.Build.props`, repository build configuration.
- **Success:** solution restores and builds; project references follow dependency direction; core does not reference the rewriter or runtime profiler.
- **Research references:** research Lines 7-19 and 111-119; PRD sections 4.1–4.2 and 9.3.
- **Dependencies:** selected .NET SDK/TFM; repository license and dependency policy.

### Task 1.2: Define domain contracts, evidence taxonomy, configuration and report schema

Define immutable contracts for assembly metadata, provenance, evidence kinds, findings, formulas, assumptions, scenarios, contracts, suppressions and `analysis.json`.

- **Files:** `src/ClrLens.Core/`, `src/ClrLens.Reporting/Json/`, `schemas/analysis.schema.json`, `schemas/contracts.schema.json`, `schemas/suppressions.schema.json`.
- **Success:** schemas validate; evidence includes `EXTERNAL_CONTRACT`; report versioning and backward-compatibility policy are documented.
- **Research references:** research Lines 45-61; PRD sections 4.10–4.13 and 7.3.
- **Dependencies:** Task 1.1.

### Task 1.3: Create fixture assemblies and test harness

Create small deterministic assemblies covering valid/invalid PE, EH, loops, allocations, external payloads, strong names, R2R and unsupported input categories.

- **Files:** `tests/fixtures/`, `tests/ClrLens.Tests.IL/`, `tests/ClrLens.Tests.Regression/`.
- **Success:** fixtures build independently; tests can analyze them without executing fixture methods; expected hashes/metadata are recorded.
- **Research references:** research Lines 15-19 and 111-113; PRD sections 4.1 and 10.2.
- **Dependencies:** Task 1.1.

## Phase 2: PE, metadata, CIL and IR

### Task 2.1: Implement safe PE/metadata ingestion

Implement read-only assembly ingestion, hashing, capability detection, dependency/reference resolution and resource limits. Detect NativeAOT, mixed mode, R2R, strong name and missing PDB.

- **Files:** `src/ClrLens.PE/`, `src/ClrLens.Core/Configuration/`, `tests/ClrLens.Tests.IL/MetadataIngestionTests.cs`.
- **Success:** no target code executes; malformed input returns structured diagnostics; supported/unsupported statuses are deterministic.
- **Research references:** research Lines 21-31; PRD section 4.1.
- **Dependencies:** Phase 1; `System.Reflection.Metadata`/`PEReader`.

### Task 2.2: Implement CIL decoding and stack validation

Decode method bodies, opcodes, tokens, signatures and exception regions. Validate evaluation-stack transitions and preserve IL offsets and exceptional edges.

- **Files:** `src/ClrLens.IL/Decoder/`, `src/ClrLens.IL/EvaluationStack/`, `src/ClrLens.IL/ExceptionHandling/`, `tests/ClrLens.Tests.IL/DecoderTests.cs`.
- **Success:** fixture opcodes decode correctly; invalid stack/type/branch cases are reported; EH regions are preserved.
- **Research references:** research Lines 21-31 and 111-114; PRD sections 4.2–4.3.
- **Dependencies:** Task 2.1.

### Task 2.3: Implement normalized typed IR

Convert stack-machine instructions into typed nodes, SSA-like values, local definitions, side-effect flags and provenance links to original instructions.

- **Files:** `src/ClrLens.IR/`, `src/ClrLens.IL/`, `tests/ClrLens.Tests.Unit/IrNormalizationTests.cs`.
- **Success:** each supported instruction maps to a deterministic node; use-def links and original offsets are queryable; no Cecil type leaks into the IR.
- **Research references:** research Lines 21-31 and 113-115; PRD sections 4.2 and 4.4.
- **Dependencies:** Task 2.2.

## Phase 3: CFG, call graph and structural findings

### Task 3.1: Build CFG, dominance, SCCs and loops

Build normal and exceptional CFG edges, dominators, post-dominators, SCCs, backedges, natural loops, loop nesting and irreducible-cycle representation.

- **Files:** `src/ClrLens.IR/CFG/`, `src/ClrLens.Analysis/Dominance/`, `src/ClrLens.Analysis/Loops/`, `tests/ClrLens.Tests.Unit/CfgTests.cs`.
- **Success:** branch targets and EH flow are represented; natural loops are not confused with arbitrary cycles; graph queries are deterministic.
- **Research references:** research Lines 21-31 and 113-115; PRD section 4.3.
- **Dependencies:** Task 2.3.

### Task 3.2: Build call graph and unknown effects

Resolve intra-assembly calls, classify virtual/interface/external calls and attach conservative `UnknownEffect` when no body or summary exists.

- **Files:** `src/ClrLens.Analysis/CallGraph/`, `src/ClrLens.Core/Model/Effects.cs`, `tests/ClrLens.Tests.Unit/CallGraphTests.cs`.
- **Success:** SCCs of calls are visible; unresolved calls do not disappear; effects are included in provenance.
- **Research references:** research Lines 31-44 and 114-115; PRD section 4.6.
- **Dependencies:** Task 2.3 and Task 3.1.

### Task 3.3: Implement initial structural findings

Implement findings for loop nesting, quadratic traversal patterns, busy waits, repeated calls and direct allocations.

- **Files:** `src/ClrLens.Rules/Cpu/`, `src/ClrLens.Rules/Memory/`, `src/ClrLens.Reporting/`, `tests/ClrLens.Tests.Regression/InitialRulesTests.cs`.
- **Success:** findings include IDs, severity, IL evidence, remediation type and `UNKNOWN` where bounds are unavailable; JSON/SARIF output is stable.
- **Research references:** research Lines 45-61 and 113-116; PRD sections 4.7, 4.8 and 4.11.
- **Dependencies:** Tasks 1.2, 2.3, 3.1 and 3.2.

## Phase 4: Mathematical analysis and memory model

### Task 4.1: Implement abstract domains and fixpoint engine

Implement intervals, congruence, nullability, cardinality, allocation, escape, alias and concurrency domains with reduced joins, widening, narrowing and transfer functions.

- **Files:** `src/ClrLens.Analysis/AbstractInterpretation/`, `src/ClrLens.Math/`, `tests/ClrLens.Tests.Unit/AbstractDomainTests.cs`.
- **Success:** joins are monotonic; loops converge within limits; transfer functions preserve fixed-width arithmetic metadata; loss of precision is recorded.
- **Research references:** research Lines 63-76 and 113-115; PRD section 4.4.
- **Dependencies:** Phase 3; selected numeric/SMT abstraction boundaries.

### Task 4.2: Implement Scalar Evolution, trip counts and termination

Implement `{Start,+,Step}` recurrences, induction variables, trip-count distinctions, overflow conditions and termination statuses.

- **Files:** `src/ClrLens.Analysis/ScalarEvolution/`, `src/ClrLens.Analysis/Termination/`, `tests/ClrLens.Tests.Regression/LoopAnalysisTests.cs`.
- **Success:** linear and stepped loops produce symbolic bounds; unknown progress produces `TERMINATION_UNKNOWN`; no non-termination claim is emitted from missing proof alone.
- **Research references:** research Lines 63-76 and 113-115; PRD section 4.5.
- **Dependencies:** Task 4.1 and Phase 3 CFG.

### Task 4.3: Implement summaries, abstract heap, escape, retention and LOH

Add versioned summaries and memory modeling for allocations, static roots, events, closures, caches, collections, LOH threshold and concurrency amplification.

- **Files:** `src/ClrLens.KnowledgeBase/`, `src/ClrLens.Analysis/HeapAnalysis/`, `src/ClrLens.Analysis/EscapeAnalysis/`, `tests/ClrLens.Tests.Regression/MemoryModelTests.cs`.
- **Success:** allocation volume, live memory, retained memory and peak estimate are separate; LOH threshold is configurable; static retention paths are explainable.
- **Research references:** research Lines 31-44 and 77-93; PRD sections 4.6 and 4.8.
- **Dependencies:** Tasks 3.2, 4.1 and 4.2.

## Phase 5: Pessimistic analysis, contracts and reports

### Task 5.1: Implement cost modes and input-bound propagation

Implement `typical`, `pessimistic` and `observed` modes, symbolic external inputs, worst-case branch joins, unbounded-loop status and `MEM012`.

- **Files:** `src/ClrLens.Analysis/CostModes/`, `src/ClrLens.Rules/Memory/UnboundedIoMaterializationRule.cs`, `src/ClrLens.Cli/`.
- **Success:** unknown payloads remain symbolic; worst-case paths are labeled upper bounds; scenarios `baseline`, `stress` and `untrusted` remain independent.
- **Research references:** research Lines 63-93 and 115-117; PRD sections 4.9 and 5.3.
- **Dependencies:** Tasks 4.1–4.3 and Task 3.3.

### Task 5.2: Implement contracts, suppressions and CI policies

Implement YAML/JSON contract loading, attribute/manifest adapters, expiration checks, suppression governance and configurable CI failure policies.

- **Files:** `src/ClrLens.Core/Configuration/`, `src/ClrLens.Rules/Policy/`, `src/ClrLens.Cli/PolicyCommands.cs`, `tests/ClrLens.Tests.Regression/PolicyTests.cs`.
- **Success:** incomplete suppressions fail validation; expired contracts are visible; suppressed findings remain in JSON/SARIF; `UNKNOWN` never becomes `PROVEN` through suppression.
- **Research references:** research Lines 63-93 and 115-117; PRD sections 4.10, 4.13 and 5.3.
- **Dependencies:** Task 1.2 and Task 5.1.

### Task 5.3: Implement reports and graphs

Implement versioned JSON, SARIF, Markdown and HTML reports with executive summary, finding details, formulas, assumptions, evidence badges, CFG and call graph views.

- **Files:** `src/ClrLens.Reporting/Json/`, `src/ClrLens.Reporting/Sarif/`, `src/ClrLens.Reporting/Markdown/`, `src/ClrLens.Reporting/Html/`, `tests/ClrLens.Tests.Regression/ReportContractTests.cs`.
- **Success:** all outputs are deterministic and schema-valid; critical findings contain location/cause/model/recommendation; contracts and suppressions are visible.
- **Research references:** research Lines 45-61 and 115-117; PRD sections 4.11–4.12, 5.2 and 7.3.
- **Dependencies:** Tasks 1.2, 3.3, 5.1 and 5.2.

## Phase 6: Conservative rewriting and validation

### Task 6.1: Implement rewrite candidates and proof objects

Implement opt-in candidates for constant folding, branch folding, unreachable blocks, NOP cleanup and redundant local operations with preconditions and before/after IL.

- **Files:** `src/ClrLens.Optimizer/`, `src/ClrLens.Rewriter/Cecil/`, `tests/ClrLens.Tests.Optimization/`.
- **Success:** original DLL is never overwritten; each candidate records rule, location, preconditions, diff and proof status; unknown effects block unsafe rewrites.
- **Research references:** research Lines 95-107 and 118-119; PRD section 4.14.
- **Dependencies:** Stable IR, report schema, CFG and findings from Phases 2–5.

### Task 6.2: Integrate ILVerify, strong-name/R2R policies and differential validation

Implement output validation, references handling, strong-name detection/re-signing policy, R2R rewrite restrictions, API/metadata checks and differential execution harness for trusted fixtures.

- **Files:** `src/ClrLens.Verification/`, `src/ClrLens.Rewriter/StrongName/`, `src/ClrLens.Rewriter/ReadyToRun/`, `tests/ClrLens.Tests.Optimization/ValidationTests.cs`.
- **Success:** invalid output is rejected; ILVerify status is recorded; strong-name and R2R status is explicit; differential tests compare returns and exceptions.
- **Research references:** research Lines 21-31 and 95-107; PRD sections 4.15–4.16.
- **Dependencies:** Task 6.1; ILVerify; trusted test fixtures.

### Task 6.3: Add SMT translation validation and counterexamples

Implement symbolic semantics for supported transformations, fixed-width integer proof, `PROVEN`/`COUNTEREXAMPLE`/`UNKNOWN` outcomes and counterexample serialization.

- **Files:** `src/ClrLens.Optimizer/TranslationValidation/`, `src/ClrLens.Optimizer/Proofs/`, `tests/ClrLens.Tests.Optimization/SmtValidationTests.cs`.
- **Success:** safe arithmetic transformations are proven; incorrect transformations produce concrete counterexamples; timeouts remain `UNKNOWN`.
- **Research references:** research Lines 95-107 and 118-119; PRD sections 4.14–4.15.
- **Dependencies:** Task 6.1; Z3 or equivalent SMT backend.

## Phase 7: Runtime calibration and release hardening

### Task 7.1: Add EventPipe/profile overlay and benchmark A/B support

Import compatible traces, map observed method/block frequencies where possible, distinguish observed from inferred data and run separate before/after benchmarks.

- **Files:** `src/ClrLens.Runtime/`, `src/ClrLens.Reporting/`, `tests/ClrLens.Tests.Benchmarks/`.
- **Success:** runtime/workload metadata is recorded; static and observed costs are distinct; benchmark output never serves as semantic proof.
- **Research references:** research Lines 104-107 and 118-119; PRD sections 4.7, 7.3 and 8.1.
- **Dependencies:** V0.1–V0.6; EventPipe-compatible traces; representative workloads.

### Task 7.2: Add performance, security, compatibility and release gates

Harden parsing, enforce resource limits, measure phase timings, test supported frameworks, validate privacy/redaction and define release gates for reports and rewrites.

- **Files:** `tests/ClrLens.Tests.Security/`, `tests/ClrLens.Tests.Compatibility/`, `docs/`, CI workflow files and release scripts.
- **Success:** malformed inputs do not crash the process; performance budgets are recorded; reports redact configured sensitive data; every rewritten assembly passes required gates.
- **Research references:** research Lines 7-19 and 121-123; PRD sections 7.3, 8.2–8.4 and 12.
- **Dependencies:** all previous phases; real assembly corpus and review of dependency licenses.

## File Operations Summary

At the implementation stage, create the solution/projects and tests under `src/` and `tests/`. Keep schemas, contracts and release documentation under `schemas/`, `docs/` and CI configuration directories. Do not place analyzer implementation in the report or tracking directories.

## Overall Success Criteria

- PRD requirements CL-001 through CL-018 are traceable to one or more tasks.
- The V0.1 analyzer works without executing analyzed assemblies.
- JSON/SARIF report contracts are stable before rewrite work begins.
- Pessimistic results preserve assumptions, contracts and unknown states.
- Rewrites are opt-in, independently validated and never silently replace the source assembly.
- Runtime calibration is clearly separated from static proof and static estimation.
