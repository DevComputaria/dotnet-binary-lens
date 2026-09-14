# Changelog

Todas as mudanças relevantes do CLR Lens serão registradas neste arquivo.

O formato segue as convenções do [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), com versionamento ainda em fase de definição.

## [Unreleased]

### Added

- `AssemblyReader` e `AssemblyModel` para ingestão segura de PE/metadata.
- Diagnósticos estruturados para input ausente, inválido, grande, sem metadata, timeout e falhas de leitura.
- Detecção de hash SHA-256, TFM, arquitetura, referências, PDB, strong name e ReadyToRun.
- Fixture `ClrLens.SampleFixtures` com padrões de loops, EH, branches, allocations, boxing, payload externo, retenção, generics e delegates.
- Harness determinístico de metadata em `ClrLens.Tests.IL`.
- Manifesto de fixtures com contagens e SHA-256 esperado.
- Fixture de input inválido para validar diagnóstico de formato não suportado.
- Contratos imutáveis de domínio, serializer determinístico e validador de invariantes para reports.
- Schemas versionados para analysis reports, bound contracts e suppressions.
- Exemplos de contrato válido e inválido para validação.
- Plano de desenvolvimento em `docs/plan/development-plan.md`.
- PRD completo em `docs/prd/prd.md`.
- Índice e tarefas individuais em `docs/tasks/`.
- Pesquisa consolidada e artefatos de planejamento em `.copilot-tracking/`.
- Solution `ClrLens.sln`.
- Projetos iniciais para Core, PE, IL, IR, Analysis, KnowledgeBase, Rules, Reporting, Optimizer, Rewriter, Verification, Runtime e CLI.
- Projetos de testes para Unit, IL, Optimization, Regression e Benchmarks.
- Entry point inicial da CLI `ClrLens.Cli`.

### Changed

- SDK .NET fixado em `8.0.131` por meio de `global.json`.
- TFM inicial definido como `net8.0`.
- Configuração compartilhada adicionada em `Directory.Build.props` com nullable, implicit usings, análise recomendada e warnings como erros.
- Índice de tarefas atualizado com a conclusão da T01.

### Verified

- T04 ingestion harness: PASS, DLL válida analisada sem executar métodos; input inválido e ausente produziram diagnósticos estruturados.
- T03 fixture harness: PASS, 6 tipos, 18 métodos e SHA-256 validado sem executar métodos da fixture.
- T02 smoke test: contrato `EXTERNAL_CONTRACT` aceito, contrato inválido rejeitado e serialização determinística confirmada.
- `dotnet restore ClrLens.sln` executado com sucesso.
- `dotnet build ClrLens.sln --no-restore` executado com sucesso.
- 18 projetos compilados.
- 0 warnings e 0 errors.
- Projetos de teste preparados para receber o framework e fixtures na T03.

### Pending

- Implementar T02: contratos de domínio, evidência e schemas.
- Implementar T03: fixtures e harness de testes.
- Selecionar o framework de testes e dependências de produção.
- Implementar o decoder CIL e a IR.

## Convenções

- `Added`: novas funcionalidades ou artefatos.
- `Changed`: alterações em comportamento, configuração ou arquitetura.
- `Fixed`: correções de problemas.
- `Removed`: funcionalidades ou arquivos removidos.
- `Security`: mudanças relacionadas à segurança.
- `Verified`: validações executadas e seus resultados.
- `Pending`: próximos itens conhecidos, sem representar implementação concluída.
