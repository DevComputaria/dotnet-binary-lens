# T19 — Implementar runtime overlay

**Fase:** 7 — Runtime e release  
**Status:** Não iniciado  
**Prioridade:** Média

## Objetivo
Calibrar estimativas estáticas sem confundir medição com prova.

## Implementação
Importar EventPipe/nettrace, associar métodos/blocos quando possível, registrar workload/runtime e criar benchmark A/B separado.

## Dependências
T15, T17 e workloads representativos.

## Critérios de aceite
- Observed é separado de inferred/estimated.
- Divergências static vs runtime aparecem no relatório.
- Benchmark não altera classificação semântica.
- Trace e configuração ficam registrados.
