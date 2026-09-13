# ADR-0006 — Reescrita e translation validation

- **Status:** Aceita
- **Data:** 2026-09-13

## Contexto

Uma DLL válida estruturalmente ainda pode ter comportamento diferente. Transformações de IL podem afetar overflow, exceções, EH, side effects e strong name.

## Decisão

Reescrita é opt-in. V1 limita passes simples. Cada transformação gera diff, pré-condições, proof object e resultado `PROVEN`, `COUNTEREXAMPLE` ou `UNKNOWN`. ILVerify é gate obrigatório; testes diferenciais são complementares.

Para aritmética, usar bit-vectors SMT, não apenas inteiros matemáticos.

## Consequências

- Assembly original nunca é sobrescrito.
- Timeout de solver bloqueia aprovação.
- R2R e strong name exigem políticas explícitas.
- Transformações algorítmicas complexas permanecem como REVIEW/SOURCE-CHANGE.
