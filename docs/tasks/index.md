# CLR Lens — Índice de tarefas

## Controle de execução

- **Status geral:** Não iniciado
- **PRD:** `docs/prd/prd.md`
- **Plano:** `docs/plan/development-plan.md`
- **Última atualização:** 2026-09-13

## Fases

| Fase | Tarefas | Status |
|---|---|---|
| 1. Fundação | T01–T03 | 🔵 Em andamento — T01 concluída |
| 2. PE, CIL e IR | T04–T06 | ⬜ Não iniciada |
| 3. CFG e findings | T07–T09 | ⬜ Não iniciada |
| 4. Análise matemática | T10–T12 | ⬜ Não iniciada |
| 5. Pessimismo e relatórios | T13–T15 | ⬜ Não iniciada |
| 6. Reescrita e validação | T16–T18 | ⬜ Não iniciada |
| 7. Runtime e release | T19–T20 | ⬜ Não iniciada |

## Lista de tarefas

- [T01 — Solução e módulos](task-01-solucao-modulos.md) ✅
- [T02 — Contratos de domínio](task-02-contratos-dominio.md)
- [T03 — Fixtures e testes-base](task-03-fixtures-testes.md)
- [T04 — Ingestão PE e metadata](task-04-ingestao-pe-metadata.md)
- [T05 — Decoder CIL e stack](task-05-decoder-cil-stack.md)
- [T06 — IR tipada](task-06-ir-tipada.md)
- [T07 — CFG e loops](task-07-cfg-loops.md)
- [T08 — Call graph e efeitos](task-08-call-graph-efeitos.md)
- [T09 — Findings estruturais](task-09-findings-estruturais.md)
- [T10 — Domínios abstratos](task-10-dominios-abstratos.md)
- [T11 — Scalar Evolution](task-11-scalar-evolution.md)
- [T12 — Memória e summaries](task-12-memoria-summaries.md)
- [T13 — Modo pessimista](task-13-modo-pessimista.md)
- [T14 — Contratos e supressões](task-14-contratos-supressoes.md)
- [T15 — Relatórios e CLI](task-15-relatorios-cli.md)
- [T16 — Reescrita IL](task-16-reescrita-il.md)
- [T17 — ILVerify e diferencial](task-17-validacao-il.md)
- [T18 — SMT e contraexemplos](task-18-smt-translation-validation.md)
- [T19 — Runtime overlay](task-19-runtime-overlay.md)
- [T20 — Hardening e release](task-20-hardening-release.md)

## Regra de atualização

Ao iniciar uma tarefa, alterar `⬜ Não iniciada` para `🔵 Em andamento`; ao concluir, usar `✅ Concluída` e registrar testes, decisões e pendências.
