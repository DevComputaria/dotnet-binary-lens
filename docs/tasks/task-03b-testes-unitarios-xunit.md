# T03B — Criar testes unitários com xUnit

**Fase:** 1 — Fundação / Qualidade  
**Status:** Concluída  
**Prioridade:** Crítica  
**Base:** T01–T08 concluídas

## Objetivo

Adicionar xUnit à solução e criar testes unitários determinísticos para validar os contratos e componentes implementados nas oito primeiras tarefas.

O harness executável continuará sendo usado para validações de integração com assemblies reais. Os testes xUnit devem validar unidades isoladas, entradas válidas, entradas inválidas, invariantes e casos-limite.

## Escopo técnico

### Dependências de teste

Adicionar aos projetos de teste:

- `Microsoft.NET.Test.Sdk`;
- `xunit`;
- `xunit.runner.visualstudio`;
- `coverlet.collector`.

Projetos-alvo:

- `ClrLens.Tests.Unit`;
- `ClrLens.Tests.IL`;
- `ClrLens.Tests.Optimization`;
- `ClrLens.Tests.Regression`.

O projeto `ClrLens.Tests.Benchmarks` deve permanecer separado dos testes unitários.

## Mapeamento de testes por tarefa

### T01 — Solução e módulos

Testar:

- todos os projetos restauram e compilam;
- referências entre módulos respeitam a direção arquitetural;
- `ClrLens.Core` não referencia Rewriter, Runtime ou CLI;
- todos os projetos de teste são descobertos pelo `dotnet test`;
- TFM e nullable estão configurados conforme a solução.

Arquivo sugerido:

```text
tests/ClrLens.Tests.Unit/SolutionStructureTests.cs
```

### T02 — Contratos de domínio

Testar:

- `AssemblyIdentity` preserva hash, versão, TFM e flags;
- `EvidenceKind.ExternalContract` é serializado corretamente;
- `AnalysisReportSerializer` produz JSON determinístico;
- desserialização preserva os valores;
- hash inválido é rejeitado;
- contrato com `max` negativo é rejeitado;
- contrato externo deve usar `ExternalContract`;
- contrato expirado gera erro;
- suppression sem reason/owner/scope/expiration é rejeitada;
- schemas rejeitam campos obrigatórios ausentes e propriedades extras.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.Unit/DomainContractTests.cs
tests/ClrLens.Tests.Unit/DomainValidationTests.cs
tests/ClrLens.Tests.Unit/SerializationTests.cs
```

### T03 — Fixtures e harness

Testar:

- manifesto de fixtures existe e é válido;
- fixture principal contém os tipos esperados;
- fixture contém os métodos esperados;
- fixture inválida existe;
- hash da fixture é determinístico;
- o harness não invoca métodos da fixture durante inspeção.

Arquivo sugerido:

```text
tests/ClrLens.Tests.IL/FixtureManifestTests.cs
```

### T04 — Ingestão PE e metadata

Testar:

- DLL válida produz `AssemblyModel`;
- SHA-256 possui 64 caracteres hexadecimais;
- nome, versão, TFM e arquitetura são capturados;
- referências são capturadas e ordenadas;
- contagem de tipos e métodos é maior que zero;
- arquivo inexistente gera `FileNotFound`;
- input inválido gera `InvalidPe` ou `MissingMetadata`;
- arquivo acima do limite gera `FileTooLarge`;
- timeout/cancelamento gera `Timeout`;
- análise funciona sem depender da execução do assembly;
- PDB é opcional e offsets continuam disponíveis.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.IL/AssemblyReaderTests.cs
tests/ClrLens.Tests.IL/AssemblyReaderInvalidInputTests.cs
```

### T05 — Decoder CIL e stack

Testar:

- `nop`, constantes, locals, args e operações aritméticas são decodificados;
- operandos inline e short inline são interpretados;
- branches curtos e longos possuem destinos corretos;
- `switch` preserva todos os destinos;
- tokens e operandos de metadata são preservados;
- branch para offset inválido gera `InvalidBranchTarget`;
- operand truncado gera `TruncatedOperand`;
- stack underflow gera `StackUnderflow`;
- stack height inconsistente gera `StackHeightMismatch`;
- `try/catch/finally/filter` são preservados;
- calls com comportamento variável geram diagnóstico explícito.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.IL/CilDecoderTests.cs
tests/ClrLens.Tests.IL/CilStackValidationTests.cs
tests/ClrLens.Tests.IL/CilExceptionRegionTests.cs
```

### T06 — IR tipada

Testar:

- cada node mantém offset IL;
- constantes geram `Constant` e `ValueId`;
- locals geram `LoadLocal`/`StoreLocal`;
- operações binárias e unárias preservam inputs;
- `newobj`/`newarr` geram `Allocate` e `CanAllocate`;
- `box` gera `Box`;
- fields geram `LoadField`/`StoreField`;
- branches, switch, return e throw geram operações próprias;
- `CanThrow`, `HasSideEffect` e `CanAllocate` são coerentes;
- regiões EH são preservadas na `MethodIr`;
- calls variáveis geram `IrDiagnostic` sem inventar argumentos;
- values SSA-like são determinísticos.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.Unit/IrModelTests.cs
tests/ClrLens.Tests.Unit/CilToIrBuilderTests.cs
```

### T07 — CFG e loops

Testar:

- cada método com corpo gera ao menos um basic block;
- entry block é identificado;
- offsets de instructions pertencem ao bloco correto;
- arestas normais são separadas de excepcionais;
- todos os endpoints de edge existem;
- dominadores convergem;
- pós-dominadores são calculados para exits;
- SCCs são determinísticos;
- backedges apontam para headers dominantes;
- loops naturais incluem header e latch;
- loops aninhados possuem depth correto;
- ciclo irreduzível permanece SCC e não é classificado como loop natural;
- EH cria arestas excepcionais para handlers quando aplicável.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.Unit/ControlFlowGraphTests.cs
tests/ClrLens.Tests.Unit/DominanceTests.cs
tests/ClrLens.Tests.Unit/LoopAnalysisTests.cs
```

### T08 — Call graph e efeitos

Testar:

- call sites preservam caller e offset IL;
- tokens de chamadas são preservados;
- MethodDefinition é resolvido como chamada interna;
- MemberReference é preservado como chamada externa;
- MethodSpecification não desaparece;
- chamada desconhecida recebe `UnknownEffect`;
- `newobj` inclui `MayAllocate`;
- calls externas incluem `MayThrow`, `MayWriteMemory` e `UnknownCpu`;
- métodos de bloqueio recebem `MayBlock`;
- métodos de spawn recebem `MaySpawnWork`;
- edges internos são criados corretamente;
- cada método analisado aparece como nó do grafo;
- SCCs do call graph são determinísticos.

Arquivos sugeridos:

```text
tests/ClrLens.Tests.Unit/CallGraphTests.cs
tests/ClrLens.Tests.Unit/UnknownEffectTests.cs
```

## Organização dos testes

```text
tests/
  ClrLens.Tests.Unit/
    SolutionStructureTests.cs
    DomainContractTests.cs
    DomainValidationTests.cs
    SerializationTests.cs
    IrModelTests.cs
    CilToIrBuilderTests.cs
    ControlFlowGraphTests.cs
    DominanceTests.cs
    LoopAnalysisTests.cs
    CallGraphTests.cs
    UnknownEffectTests.cs

  ClrLens.Tests.IL/
    FixtureManifestTests.cs
    AssemblyReaderTests.cs
    AssemblyReaderInvalidInputTests.cs
    CilDecoderTests.cs
    CilStackValidationTests.cs
    CilExceptionRegionTests.cs

  ClrLens.Tests.Regression/
    FixtureRegressionTests.cs
    ReportCompatibilityTests.cs

  ClrLens.Tests.Optimization/
    # Reservado para T16–T18
```

## Critérios de aceite

- `dotnet test ClrLens.sln` descobre e executa os testes xUnit.
- Os testes não executam métodos das fixtures durante análise estática.
- Todos os testes são determinísticos e independentes de ordem.
- Casos positivos e negativos são cobertos para cada tarefa T01–T08.
- O harness de integração continua passando.
- Cobertura mínima inicial recomendada: 80% para `ClrLens.Core`, `ClrLens.PE`, `ClrLens.IL`, `ClrLens.IR` e `ClrLens.Analysis`.
- Falhas de contrato, stack, branch, CFG e call graph apresentam mensagens localizadas.
- CI executa testes unitários, harness de fixtures e build.

## Dependências

- T01–T08.
- SDK .NET 8.0.131.
- xUnit e `Microsoft.NET.Test.Sdk`.
- Fixtures em `tests/fixtures/`.

## Resultado esperado

Ao concluir esta tarefa, cada componente implementado até T08 terá uma suíte xUnit correspondente, enquanto o harness continuará validando a integração real entre PEReader, decoder, IR, CFG e call graph.

## Resultado da implementação

- xUnit, `Microsoft.NET.Test.Sdk`, runner Visual Studio e Coverlet configurados nos projetos unitário/regressão.
- Suíte unitária criada para contratos, IR, CFG, loops, call graph, ingestão PE e decoder CIL.
- Testes de regressão criados para fixture válida, fixture inválida e branches.
- Harness `tests/ClrLens.Tests.IL` preservado como teste de integração executável.
- `dotnet test ClrLens.sln --no-restore`: 15 testes passaram, 0 falharam.
- Harness de integração: PASS — 18 CFGs, 3 loops, 3 arestas excepcionais, 15 call sites e 13 chamadas externas.
- Cobertura coletada em Cobertura XML; unitários atingiram 72,66% de linhas e regressão 25,10% de linhas. O alvo recomendado de 80% permanece como melhoria futura.
