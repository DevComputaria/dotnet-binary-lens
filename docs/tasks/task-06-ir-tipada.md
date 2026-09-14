# T06 — Criar IR tipada

**Fase:** 2 — PE, CIL e IR  
**Status:** Concluída  
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

## Resultado da implementação

- Criados `ValueId`, `IrOperationKind`, `IrNode`, `IrDiagnostic` e `MethodIr`.
- Criado `CilToIrBuilder` para normalizar stack-machine em IR própria.
- Implementados nodes para constantes, argumentos, locals, operações binárias/unárias, fields, calls, allocations, boxing, branches, switch, return e throw.
- Preservados offsets IL, opcode original, flags de efeitos e regiões EH.
- Criados values SSA-like determinísticos e mapa de locals.
- Calls com comportamento de stack variável permanecem diagnosticadas para refinamento futuro com assinatura metadata-aware.
- Harness validou 163 nodes, 64 values e 6 allocation nodes sem executar métodos da fixture.
