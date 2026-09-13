# T06 — Criar IR tipada

**Fase:** 2 — PE, CIL e IR  
**Status:** Não iniciado  
**Prioridade:** Crítica

## Objetivo
Converter a stack machine em IR própria, tipada e rastreável.

## Implementação
Criar nodes, ValueId, locals SSA-like, use-def inicial, flags `CanThrow`, `HasSideEffect`, `IsVolatile`, `MayAllocate` e provenance para instrução original.

## Dependências
T05.

## Critérios de aceite
- IR determinística.
- Cada node mantém offset IL.
- Cecil não vaza para contratos da IR.
- Testes cobrem constantes, locals, calls, branches e allocations.
