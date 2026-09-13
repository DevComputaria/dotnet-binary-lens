# CLR Lens development changes

## Status

- **Backlog:** criado
- **Implementação:** não iniciada
- **Fase atual:** aguardando início da Fase 1 — Repository and solution foundation
- **Fonte de requisitos:** `docs/prd/prd.md`
- **Checklist:** `.copilot-tracking/plans/20260913-clr-lens-development-backlog-plan.instructions.md`
- **Detalhes:** `.copilot-tracking/details/20260913-clr-lens-development-backlog-details.md`

## Registro de execução

### 2026-09-13 — Preparação

- Pesquisa consolidada criada em `.copilot-tracking/research/20260913-clr-lens-development-research.md`.
- Backlog de sete fases criado e rastreado ao PRD.
- Referências de linha do checklist verificadas e corrigidas.
- Nenhum arquivo de implementação foi criado ou alterado.
- Nenhum teste de código foi executado porque o repositório ainda não possui solução .NET.

### 2026-09-13 — T01 concluída

- Criado `global.json` fixando SDK `8.0.131`.
- Criado `Directory.Build.props` com TFM `net8.0`, nullable, implicit usings e warnings como erros.
- Criada `ClrLens.sln` com 13 projetos de produção, CLI e 5 projetos de testes.
- Criadas referências entre projetos conforme a direção arquitetural.
- Criado entrypoint mínimo da CLI.
- `dotnet restore ClrLens.sln`: PASS.
- `dotnet build ClrLens.sln --no-restore`: PASS, 0 warnings, 0 errors.
- `dotnet test ClrLens.sln --no-build --no-restore`: sem testes executáveis nesta fase; runner será introduzido na T03.
- Decisão: projetos de teste permanecem compiláveis, sem `IsTestProject`, até a criação das fixtures e escolha do framework de testes.

## Próxima ação

Executar Task 1.1: criar a solução .NET e os limites dos projetos `ClrLens.*`, após selecionar e registrar o SDK/TFM alvo.

## Decisões pendentes

- Selecionar SDK e TFM inicial.
- Confirmar dependências e versões de `System.Reflection.Metadata`, Mono.Cecil, ILVerify e solver SMT.
- Definir framework de testes.
- Definir política de distribuição e licenciamento das dependências.
