# T09 — Implementar findings estruturais

**Fase:** 3 — CFG e findings  
**Status:** Concluída  
**Prioridade:** Alta

## Objetivo
Criar as primeiras regras acionáveis de CPU e memória.

## Regras
Loop excessivo, traversal quadrático, busy wait, call cara em loop, `newobj/newarr/box` repetidos e alocação dentro de loop.

## Dependências
T02, T06, T07 e T08.

## Critérios de aceite
- Finding possui ID, severidade, evidência IL e remediation.
- Bounds desconhecidos permanecem `UNKNOWN`.
- JSON/SARIF são produzidos.
- Não há auto-fix nesta tarefa.

## Resultado da implementação

- Criado `StructuralFindingsAnalyzer` em `src/ClrLens.Rules/StructuralFindingsAnalyzer.cs`.
- Implementados findings `CPU001` para nesting excessivo, `MEM001` para alocação em loop, `CPU004` para loops sem progresso reconhecível e `CPU005` para calls em loops.
- Findings preservam método, token, offsets IL, severidade, confidence, evidence kind, cost model, recomendações e remediation.
- Nenhuma regra estrutural produz `AUTO_FIX`; remediações são `REVIEW` ou `SOURCE_CHANGE`.
- Criado `SarifReportSerializer` em `src/ClrLens.Reporting/SarifReportSerializer.cs`.
- Testes xUnit validam findings acionáveis e documento SARIF.
- Bounds desconhecidos permanecem simbólicos/inferred; regras dependentes de trip count preciso aguardam T10/T11.
