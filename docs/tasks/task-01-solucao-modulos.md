# T01 — Criar solução e módulos

**Fase:** 1 — Fundação  
**Status:** Concluída  
**Prioridade:** Crítica

## Objetivo
Criar a solução .NET e separar os módulos conforme a arquitetura do PRD.

## Escopo
Criar projetos para Core, PE, IL, IR, Analysis, KnowledgeBase, Rules, Reporting, Optimizer, Rewriter, Verification, Runtime, CLI e testes.

## Dependências
Nenhuma. Selecionar SDK/TFM antes de implementar.

## Arquivos previstos
`*.sln`, `src/ClrLens.*/*.csproj`, `tests/ClrLens.Tests.*/*.csproj`, `Directory.Build.props`.

## Critérios de aceite
- Solução restaura e compila.
- Dependências seguem direção arquitetural.
- Core não referencia Rewriter nem Runtime.
- Build e testes básicos executam em ambiente limpo.

## Resultado da implementação

- SDK fixado em `8.0.131` via `global.json`.
- TFM inicial definido como `net8.0`.
- Solution `ClrLens.sln` criada com 18 projetos.
- Build validado com zero warnings e zero erros.
- Projetos de teste foram preparados como projetos compiláveis; o runner de testes será adicionado na T03 junto com as fixtures.
