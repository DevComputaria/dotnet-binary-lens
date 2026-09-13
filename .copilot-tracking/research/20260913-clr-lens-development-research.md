<!-- markdownlint-disable-file -->

# Research: CLR Lens development backlog

## Source documents

- `docs/prd/prd.md` — product vision, personas, requirements CL-001 through CL-018, milestones, launch criteria and quality metrics.
- `docs/plan/development-plan.md` — architecture, phases V0.1–V1, evidence model, pessimistic analysis, contracts, suppressions, testing and risks.
- `plan.md` — initial product scope, module boundaries, CLI, report formats, optimizer limits and supported assembly constraints.
- `math.md` — formal foundations: ECMA-335, abstract interpretation, CFG, SSA, Scalar Evolution, MemorySSA, GC/LOH, queueing theory, SMT and translation validation.

## Verified project structure

The repository is currently documentation-first. Existing project artifacts are:

```text
LICENSE
math.md
plan.md
docs/plan/development-plan.md
docs/prd/prd.md
```

No source solution, project files, test projects, dependency manifests or existing task-tracking files were found. The development backlog must therefore begin with repository scaffolding, contracts and fixtures before implementation modules can be compiled.

## Technical decisions verified by the source documents

### Analysis boundary

- The analyzed assembly must never be executed during static analysis.
- The primary input is a managed PE/DLL, with optional PDB and explicit dependency references.
- NativeAOT and mixed-mode C++/CLI are unsupported; ReadyToRun is analysis-supported but rewrite-sensitive.
- Strong-name handling, PDB alignment, metadata validity and output hashing are release concerns.

### Architecture

The planned boundaries are:

```text
ClrLens.Core
ClrLens.PE
ClrLens.IL
ClrLens.IR
ClrLens.Analysis
ClrLens.KnowledgeBase
ClrLens.Rules
ClrLens.Reporting
ClrLens.Optimizer
ClrLens.Rewriter
ClrLens.Verification
ClrLens.Runtime
ClrLens.Cli
```

The core analyzer must not expose Cecil types. CIL is decoded into a project-owned typed IR with normal and exceptional CFG edges, SSA-like values, use-def chains and memory operations.

### Mathematical model

The abstract state is a reduced product of intervals, congruence, nullability, cardinality, allocation, escape, alias and concurrency. Analysis uses transfer functions, join, fixpoint iteration, widening and optional narrowing.

Integer proof analysis must model fixed-width CIL arithmetic, signed/unsigned comparisons, checked/unchecked overflow and shifts. Fast interval analysis and proof-oriented SMT bit-vectors are separate layers.

Loop analysis requires dominators, post-dominators, SCCs, backedges, natural loops, nesting, Scalar Evolution-style recurrences and explicit `TripCount` versus `BackedgeTakenCount`.

### Evidence model

Findings must distinguish:

```text
PROVEN
PROVEN_UNDER_ASSUMPTIONS
STATIC_UPPER_BOUND
STATIC_LOWER_BOUND
INFERRED
SYMBOLIC
ESTIMATED
OBSERVED
HEURISTIC
EXTERNAL_CONTRACT
UNKNOWN
```

A score is heuristic prioritization, never a theorem. Allocation volume, live memory, retained memory, working-set estimate and native memory are separate quantities.

### Pessimistic analysis and contracts

The CLI/report supports `typical`, `pessimistic` and `observed` cost modes, plus `baseline`, `stress` and `untrusted` scenarios. Unknown external input must remain symbolic unless bounded by code, a library summary, an external contract or a documented runtime limit.

Contracts require symbol, maximum, unit, source, scope, owner and expiration. Suppressions require ID, target, reason, owner, ticket, scope and expiration. Suppression preserves the finding and cannot convert `UNKNOWN` into `PROVEN`.

### Reporting

The primary artifact is versioned `analysis.json`. Additional outputs are SARIF, Markdown and HTML. Findings require assembly hash, method/type/token, IL range, evidence, formula, assumptions, cause, impact, recommendation and remediation type.

### Safe rewriting

The optimizer is opt-in and follows the analyzer. Initial automatic passes are limited to constant folding/propagation, branch folding, unreachable block elimination, NOP cleanup and redundant local operations. Every rewrite requires preconditions, an IL diff, a proof object or validation result, ILVerify, preservation checks and a new output file.

## External implementation guidance

- ECMA-335 is the normative reference for CLI metadata, CIL semantics, evaluation stack and verification.
- `System.Reflection.Metadata` and `PEReader` are the planned low-level readers.
- Mono.Cecil is the planned isolated assembly writer.
- ILVerify is the post-write MSIL validation gate.
- Z3 bit-vectors are the planned proof backend for fixed-width arithmetic.
- EventPipe, `dotnet-counters` and `dotnet-trace` provide optional runtime calibration.
- LLVM Loop Terminology, Scalar Evolution and MemorySSA are design references, not dependencies or proof of CLR semantics.

## Implementation guidance

1. Scaffold the solution and contracts before implementing advanced analysis.
2. Build fixture assemblies that cover valid/invalid metadata, exception handling, loops, allocations, external payloads, strong names and ReadyToRun.
3. Implement deterministic ingestion, decoding and IR before abstract interpretation.
4. Implement CFG and call graph before cost rules.
5. Add summaries and abstract state before pessimistic bounds.
6. Add report schema and SARIF before CI gates.
7. Add contracts/suppressions before enabling pessimistic findings as release blockers.
8. Add rewriting only after analysis evidence and output contracts are stable.
9. Add SMT, ILVerify, differential validation and runtime overlays last.

## Research limitations

No implementation source, build command, dependency manifest or test framework exists yet. Exact package versions and target framework must be selected during scaffolding. Runtime calibration and benchmark claims require real workloads and must not be invented from static analysis alone.
