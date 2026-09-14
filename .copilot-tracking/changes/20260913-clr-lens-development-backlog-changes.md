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

### 2026-09-13 — T02 concluída

- Criados contratos imutáveis para assembly, provenance, localização IL, assumptions, findings, cenários, bound contracts e suppressions.
- Adicionado `AnalysisReportSerializer` com enumeração em string, camelCase e saída determinística.
- Adicionado `DomainContractValidator` para hash SHA-256, campos obrigatórios, bounds, expiração e `EXTERNAL_CONTRACT`.
- Criados schemas versionados em `schemas/` e exemplos válidos/inválidos em `examples/`.
- `dotnet build ClrLens.sln --no-restore`: PASS, 0 warnings, 0 errors.
- Smoke test temporário: PASS para contrato válido, rejeição de contrato inválido e serialização determinística.
- Observação: o runner de testes permanente permanece planejado para T03.

### 2026-09-13 — T03 concluída

- Criada fixture independente `tests/fixtures/ClrLens.SampleFixtures`.
- Cobertos loops, EH, branches, allocations, boxing, payload externo, static retention, generics e delegates.
- Criado input inválido controlado e manifesto com hash/contagens esperados.
- `ClrLens.Tests.IL` convertido em harness executável usando apenas leitura PE/metadata e SHA-256.
- `dotnet build ClrLens.sln --no-restore`: PASS, 0 warnings, 0 errors.
- Harness: PASS, 6 tipos, 18 métodos, SHA256 `41996472F84A50C73EB8724E183B669FA650450CBB32B567685E0CC74749949C`.
- Strong-name, R2R e NativeAOT permanecem como artefatos gerados em pipeline seguro, conforme documentado no manifesto.

## Próxima ação

Executar Task 1.1: criar a solução .NET e os limites dos projetos `ClrLens.*`, após selecionar e registrar o SDK/TFM alvo.

## Decisões pendentes

- Selecionar SDK e TFM inicial.
- Confirmar dependências e versões de `System.Reflection.Metadata`, Mono.Cecil, ILVerify e solver SMT.
- Definir framework de testes.
- Definir política de distribuição e licenciamento das dependências.
