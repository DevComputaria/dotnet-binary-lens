# Changelog

Todas as mudanças relevantes do CLR Lens serão registradas neste arquivo.

O formato segue as convenções do [Keep a Changelog](https://keepachangelog.com/pt-BR/1.1.0/), com versionamento ainda em fase de definição.

## [Unreleased]

### Added

- Findings estruturais `CPU001`, `CPU004`, `CPU005` e `MEM001`.
- Serializer SARIF 2.1.0 para findings.
- Suíte xUnit para contratos, ingestão PE, decoder CIL, IR, CFG, loops e call graph.
- Cobertura Cobertura XML via Coverlet.
- Call graph com call sites, resolução de métodos internos/externos, SCCs e `UnknownEffect`.
- CFG e análises de loops em `ClrLens.Analysis`, com fluxo normal/excepcional, dominadores, SCCs, backedges e loops naturais.
- IR tipada com `ValueId`, nodes, flags de efeitos, provenance, locals SSA-like e regiões EH.
- Modelos e decoder CIL em `ClrLens.IL` com operandos, branches, stack diagnostics e EH regions.
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

- T09: 14 testes unitários e 3 de regressão passaram; harness de integração continuou verde.
- T03B: 15 testes xUnit passaram, 0 falharam; harness de integração também passou.
- Cobertura T03B: 72,66% de linhas nos unitários e 25,10% nos testes de regressão; meta recomendada de 80% permanece pendente.
- T08 call graph harness: PASS, call sites, chamadas externas e efeitos conservadores validados.
- T07 CFG harness: PASS, 18 CFGs, 3 loops naturais e 3 arestas excepcionais.
- T06 IR harness: PASS, 163 nodes, 64 SSA-like values e 6 allocation nodes.
- T05 decoder harness: PASS, corpos CIL decodificados sem executar métodos, branches validados e EH preservado.
- T04 ingestion harness: PASS, DLL válida analisada sem executar métodos; input inválido e ausente produziram diagnósticos estruturados.
- T03 fixture harness: PASS, 6 tipos, 18 métodos e SHA-256 validado sem executar métodos da fixture.
- T02 smoke test: contrato `EXTERNAL_CONTRACT` aceito, contrato inválido rejeitado e serialização determinística confirmada.
- `dotnet restore ClrLens.sln` executado com sucesso.
- `dotnet build ClrLens.sln --no-restore` executado com sucesso.
- 18 projetos compilados.
- 0 warnings e 0 errors.
- Projetos de teste preparados para receber o framework e fixtures na T03.

### Pending

- Aumentar cobertura da suíte T03B para a meta recomendada de 80%.
- Selecionar o framework de testes e dependências de produção.
- Implementar T10: domínios abstratos e fixpoint engine.

## Convenções

- `Added`: novas funcionalidades ou artefatos.
- `Changed`: alterações em comportamento, configuração ou arquitetura.
- `Fixed`: correções de problemas.
- `Removed`: funcionalidades ou arquivos removidos.
- `Security`: mudanças relacionadas à segurança.
- `Verified`: validações executadas e seus resultados.
- `Pending`: próximos itens conhecidos, sem representar implementação concluída.
