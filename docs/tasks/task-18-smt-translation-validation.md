# T18 — Implementar SMT e translation validation

**Fase:** 6 — Reescrita e validação  
**Status:** Não iniciado  
**Prioridade:** Média

## Objetivo
Provar equivalência de transformações suportadas e explicar rejeições.

## Implementação
Modelar bit-vectors signed/unsigned, emitir PROVEN/COUNTEREXAMPLE/UNKNOWN, persistir entradas divergentes e controlar timeout.

## Dependências
T10, T16 e backend Z3/equivalente.

## Critérios de aceite
- Transformação segura é provada.
- Transformação incorreta produz contraexemplo.
- Timeout produz UNKNOWN.
- Resultado é incluído no proof object e relatório.
