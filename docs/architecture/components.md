# Componentes e fluxo de dados

## Camadas

### ClrLens.Core

Contratos de domínio, proveniência, evidência, findings, configuração, cenários, contratos e supressões.

### ClrLens.PE

Leitura de PE, metadata, method bodies, PDB, strong name, R2R e capacidades do assembly.

### ClrLens.IL

Decodificação de opcodes, assinaturas, evaluation stack, tipos, tokens e regiões de exceção.

### ClrLens.IR

Nodes tipados, ValueId, basic blocks, CFG, SSA-like, use-def e MemorySSA-like.

### ClrLens.Analysis

Call graph, dominators, loops, Scalar Evolution, abstract interpretation, range, cardinality, alias, escape, heap, termination, CPU, memória e concorrência.

### ClrLens.KnowledgeBase

Summaries versionados de BCL, LINQ, collections, streams, serializers, Tasks e APIs externas.

### ClrLens.Rules

Findings CPU, MEM, GC, ASYNC, CONC e regras de governança.

### ClrLens.Reporting

Serialização JSON, SARIF, Markdown, HTML e grafos.

### ClrLens.Optimizer

Candidatos, passes, proof objects e translation validation.

### ClrLens.Rewriter

Writer isolado, strong-name handling, R2R policy e output policy.

### ClrLens.Verification

ILVerify, semantic diff, API compatibility e testes diferenciais.

### ClrLens.Runtime

EventPipe, dotnet-trace, dotnet-counters e profile overlay.

### ClrLens.Cli

Comandos `analyze`, `inspect`, `graph`, `optimize`, `verify`, `diff` e `profile`.

## Direção de dependências

```text
Core <- PE, IL
Core/IL/IR <- Analysis
Core <- KnowledgeBase, Rules, Reporting, Runtime
Analysis/IR <- Optimizer
Optimizer <- Rewriter, Verification
Todos os módulos necessários <- CLI
```

O núcleo formal não deve depender de runtime profiling ou de uma biblioteca específica de reescrita.

## Entradas e saídas

### Entradas

- DLL/assembly gerenciado;
- PDB opcional;
- referências de dependências;
- runtime model;
- contratos de limites;
- supressões;
- trace EventPipe opcional;
- configuração de cenário.

### Saídas

- `analysis.json`;
- `analysis.sarif`;
- `analysis.md`;
- `analysis.html`;
- `optimized.dll` somente quando aprovado;
- `optimization-proof.json`;
- logs e diagnóstico de suporte.
