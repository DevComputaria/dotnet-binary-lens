# ADR-0005 — Contratos e modo pessimista

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

Payloads e cardinalidades originados de I/O podem ser desconhecidos. Ignorar o risco gera falsos negativos; assumir um número inventado gera falsa precisão.

## Decisão

Suportar modos `typical`, `pessimistic` e `observed`, além de cenários `baseline`, `stress` e `untrusted`. Bounds externos entram por contratos versionados com origem, escopo, owner e expiração.

No modo pessimista:

$$
C_{worst}(if)=\max(C_{then},C_{else})
$$

## Consequências

- Payload sem limite permanece simbólico.
- Contrato externo produz `EXTERNAL_CONTRACT` ou `PROVEN_UNDER_ASSUMPTIONS`.
- Supressões mantêm o finding e nunca convertem UNKNOWN em PROVEN.
- Contratos expirados falham políticas configuradas.
