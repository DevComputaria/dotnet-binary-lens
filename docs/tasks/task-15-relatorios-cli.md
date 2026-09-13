# T15 — Implementar relatórios e CLI

**Fase:** 5 — Pessimismo e relatórios  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Entregar a autópsia de performance consumível por humanos e CI/CD.

## Implementação
Criar JSON, SARIF, Markdown, HTML, sumário executivo, finding detalhado, CFG/call graph, badges epistemológicos e comandos analyze/inspect/graph.

## Dependências
T09, T13 e T14.

## Critérios de aceite
- Outputs são determinísticos e schema-valid.
- Finding crítico mostra local, causa, fórmula e recomendação.
- CLI suporta filtros por método, severidade, cenário e remediation.
- CI pode falhar por policy.
