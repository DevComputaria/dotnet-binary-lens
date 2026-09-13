# Roadmap técnico

## V0.1 — Fundamentos

- solução e módulos;
- PE/metadata;
- decoder CIL;
- IR;
- CFG;
- loops;
- call graph;
- allocations;
- JSON/SARIF.

## V0.2 — Análise matemática

- abstract interpretation;
- ranges/congruence;
- Scalar Evolution;
- complexity inference;
- escape analysis;
- method summaries.

## V0.3 — Memória e cenários

- abstract heap;
- retention/live/allocated/peak;
- LOH parametrizado;
- concorrência;
- modo pessimista;
- contratos e supressões.

## V0.4 — Relatório operacional

- Markdown/HTML;
- CFG/call graph;
- findings detalhados;
- CI policies;
- evidence badges.

## V0.5 — Reescrita conservadora

- constant folding;
- branch folding;
- dead blocks;
- proof objects;
- Cecil;
- strong-name/R2R policy.

## V0.6/V1 — Validação e runtime

- ILVerify;
- SMT translation validation;
- counterexamples;
- differential tests;
- EventPipe overlay;
- benchmark A/B.
