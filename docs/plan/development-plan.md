# CLR Lens — Plano de desenvolvimento

**Versão:** 0.2
**Status:** Planejamento inicial
**Data:** 2026-09-13
**Produto:** CLR Binary Performance Analyzer + Verified IL Rewriter

## 1. Visão do produto

O CLR Lens será uma biblioteca e uma CLI para analisar assemblies .NET gerenciados a partir do PE, metadata e CIL, sem depender do código-fonte original. O produto deverá construir uma representação intermediária formal do assembly, analisar fluxo de controle, dados, chamadas, alocações, memória e concorrência, e produzir um relatório técnico de performance com evidências rastreáveis.

A reescrita de IL será uma capacidade posterior e opcional. Ela somente poderá ser aplicada quando a transformação preservar a semântica observável dentro das pré-condições conhecidas, passar pela validação estrutural de IL e gerar um registro de prova ou justificativa equivalente.

A proposta não é produzir uma “DLL matematicamente ótima”. A proposta é separar rigorosamente:

- **Prova estática:** propriedade demonstrada pelo modelo formal.
- **Limite estático:** upper/lower bound calculado sob hipóteses explícitas.
- **Inferência:** conclusão derivada de padrões e summaries.
- **Estimativa:** aproximação numérica ou simbólica.
- **Medição:** valor obtido em runtime.
- **Heurística:** priorização que não constitui teorema.
- **Desconhecido:** informação insuficiente para concluir.

### 1.1 Posicionamento de mercado

O produto deve ser posicionado como **CLR Binary Performance Analyzer + Verified IL Rewriter**, não como substituto de analisadores de código-fonte, profilers ou do RyuJIT.

| Categoria adjacente | Resolve bem | Lacuna que o CLR Lens atende |
| --- | --- | --- |
| Roslyn analyzers, SonarQube e NDepend | AST, métricas de fonte e arquitetura | Não observam necessariamente o CIL efetivamente distribuído nem preservam evidência por offset IL |
| dotTrace, dotMemory, PerfView e EventPipe | Medição dinâmica sob workload | Não antecipam o risco diretamente no pipeline quando não há execução representativa |
| Fody, PostSharp, Cecil, dnSpy e ILSpy | Inspeção, weaving e edição de IL | Não são, por si só, um motor de diagnóstico matemático com findings SARIF/JSON e prova de transformação |
| RyuJIT, tiered compilation e Dynamic PGO | Otimização de machine code em runtime | Não explicam a causa algorítmica, a amplificação de memória ou o risco arquitetural para revisão de um assembly |

O diferencial comercial é o **relatório shift-left baseado em binário**, capaz de combinar evidência CIL, modelos de complexidade, memória/GC, concorrência e telemetria opcional. A reescrita automática deve ser apresentada como recurso opt-in e secundário; o relatório e o diagnóstico são o produto principal.

## 2. Objetivos

### 2.1 Objetivos do MVP

1. Ler assemblies PE gerenciados de forma segura e não executar o código analisado.
2. Extrair tipos, métodos, metadata, corpos CIL, regiões de exceção e símbolos PDB quando disponíveis.
3. Decodificar CIL para uma IR tipada e independente da biblioteca de leitura.
4. Construir CFG com fluxo normal e excepcional: `CFG = (V, E_N, E_X)`.
5. Detectar loops, dominadores, SCCs, call graph e pontos de alocação.
6. Produzir métricas estruturais e findings de CPU, memória, GC, async e concorrência.
7. Exportar JSON, SARIF, Markdown e HTML.
8. Manter evidência por finding: assembly, tipo, método, token, offsets IL, bloco, análise e modelo matemático.
9. Garantir determinismo: a mesma entrada e configuração devem gerar o mesmo relatório.

### 2.2 Objetivos pós-MVP

1. Implementar interpretação abstrata com produto reduzido de domínios.
2. Inferir ranges, cardinalidade, congruência, escape, alias e evolução escalar.
3. Criar uma base versionada de summaries semânticos de bibliotecas.
4. Calibrar o modelo estático com EventPipe, `dotnet-counters` e `dotnet-trace`.
5. Aplicar transformações IL conservadoras.
6. Validar transformações com semântica simbólica, bit-vectors/SMT e testes diferenciais.
7. Verificar assemblies produzidos com ILVerify e validar assinaturas/compatibilidade.

### 2.3 Não objetivos

- Substituir o RyuJIT, tiered compilation ou Dynamic PGO.
- Garantir análise exata para todos os programas.
- Inferir automaticamente a intenção de negócio.
- Reescrever LINQ, coleções, async, paralelismo, tipos ou ownership sem prova suficiente.
- Analisar NativeAOT ou código nativo como se fosse CIL gerenciado.
- Declarar vazamento, loop infinito ou ganho de performance quando apenas existe possibilidade.

### 2.4 Objetivos de produto derivados da análise de mercado

1. Entregar valor mesmo quando só existe uma DLL proprietária, sem código-fonte ou workload confiável.
2. Expor um modo pessimista útil para revisão de resiliência e abuso de entrada, sem confundi-lo com previsão média.
3. Permitir que equipes forneçam limites verificáveis por contratos e que o CI controle exceções por supressões auditáveis.
4. Mostrar divergências entre o modelo CIL, o RyuJIT/runtime e o comportamento observado, em vez de declarar que o IL representa diretamente o custo de máquina.
5. Fazer do `analysis.json` um contrato de integração para CI/CD, dashboards, revisão arquitetural e políticas de release.

## 3. Princípios de engenharia

1. **Analisador antes do otimizador.** O diagnóstico deve ser confiável antes da reescrita.
2. **Semântica do CIL primeiro.** A referência normativa é a ECMA-335.
3. **Evidência antes da severidade.** Todo finding deve apontar para artefatos observáveis.
4. **Incerteza explícita.** Nunca converter `unknown` em certeza por conveniência de UX.
5. **Conservadorismo semântico.** Se houver dúvida sobre exceção, volatilidade, alias, ordem, side effect ou metadata, não reescrever.
6. **Fluxo excepcional é parte do programa.** `call`, `callvirt`, `newobj`, loads e outras operações potencialmente excepcionais não podem ser tratados apenas pelo caminho normal.
7. **Inteiros têm largura fixa.** O modelo de prova deve distinguir `int32`, `int64`, `native int`, signed/unsigned e overflow checked/unchecked.
8. **Alocação não é memória viva.** Separar `AllocationVolume`, `LiveManagedMemory`, `RetainedMemory` e `PeakWorkingSetEstimate`.
9. **Custo estrutural não é tempo de parede.** SCU e Big-O não devem ser apresentados como nanosegundos.
10. **Medição valida o modelo, não o substitui.** Runtime profile deve ser um overlay com origem e condições documentadas.
11. **Saída nunca sobrescreve a entrada por padrão.** Reescrita deve gerar novo arquivo e preservar hash da origem.
12. **Passes devem declarar invalidação.** Toda transformação deve declarar quais análises deixa inválidas e quais devem ser recalculadas.

## 4. Arquitetura proposta

```text
Assembly DLL/PDB
      |
      v
PE + Metadata Reader ---- Runtime/Profile Inputs
      |
      v
CIL Decoder + Typed Stack Validator
      |
      v
Normalized IR
      |
      +--> CFG (E_N, E_X), EH, Dominators, Post-dominators, Loops
      +--> SSA-like values, use-def, liveness
      +--> Call Graph, SCCs, method summaries
      +--> Memory model, alias/escape, abstract heap
      |
      v
Abstract Interpreter / Fixed Point Engine
      |
      +--> Range + Congruence + Nullability + Cardinality
      +--> Scalar Evolution + Trip Counts + Termination levels
      +--> CPU Cost + Allocation + Live/Retained Memory
      +--> Concurrency + Queueing + Runtime overlay
      |
      v
Findings Engine + Evidence Model
      |
      +--> JSON / SARIF / Markdown / HTML
      |
      +--> Rewrite Candidate (opcional)
                    |
                    v
          Proof/Translation Validation
                    |
                    v
          Cecil Writer + Strong-name handling
                    |
                    v
          ILVerify + API/metadata/differential tests
```

### 4.1 Módulos da biblioteca

```text
src/
  ClrLens.Core/
    Model/
    Evidence/
    Diagnostics/
    Configuration/
    Provenance/

  ClrLens.PE/
    PeReader/
    Metadata/
    AssemblyModel/
    Pdb/
    RuntimeConfiguration/

  ClrLens.IL/
    Decoder/
    OpCodes/
    Signatures/
    EvaluationStack/
    ExceptionHandling/
    Verification/

  ClrLens.IR/
    Values/
    Nodes/
    BasicBlocks/
    CFG/
    SSA/
    MemorySSA/
    UseDef/

  ClrLens.Analysis/
    CallGraph/
    Dominance/
    Loops/
    ScalarEvolution/
    AbstractInterpretation/
    RangeAnalysis/
    Nullability/
    Cardinality/
    AliasAnalysis/
    EscapeAnalysis/
    HeapAnalysis/
    Termination/
    Cpu/
    Memory/
    Concurrency/
    Async/

  ClrLens.KnowledgeBase/
    Summaries/
    RuntimeModels/
    LibraryModels/
    Schema/

  ClrLens.Rules/
    Cpu/
    Memory/
    Gc/
    Async/
    Concurrency/
    Correctness/

  ClrLens.Reporting/
    Json/
    Sarif/
    Markdown/
    Html/
    Graphs/

  ClrLens.Optimizer/
    Candidates/
    Transformations/
    Proofs/
    TranslationValidation/

  ClrLens.Rewriter/
    Cecil/
    StrongName/
    ReadyToRun/
    OutputPolicy/

  ClrLens.Runtime/
    EventPipe/
    DotnetCounters/
    DotnetTrace/
    ProfileOverlay/

  ClrLens.Cli/
```

A implementação poderá começar como uma solução .NET única, mas as fronteiras acima devem ser preservadas por interfaces. O analisador não deve depender diretamente de Cecil ou de uma API de runtime em seu núcleo formal.

## 5. Modelo de dados essencial

### 5.1 Proveniência e evidência

Cada resultado deve carregar:

```text
EvidenceKind:
  PROVEN
  PROVEN_UNDER_ASSUMPTIONS
  STATIC_UPPER_BOUND
  STATIC_LOWER_BOUND
  INFERRED
  SYMBOLIC
  ESTIMATED
  OBSERVED
  HEURISTIC
  EXTERNAL_CONTRACT
  UNKNOWN
```

Campos mínimos:

```text
AssemblyPath
AssemblySha256
TargetFramework
RuntimeModel
TypeName
MethodName
MetadataToken
IlStart
IlEnd
BasicBlockIds
CallPath
Assumptions
EvidenceKind
Confidence
Formula
Explanation
RuleId
RemediationType
```

`Confidence` deve ser tratado como confiança operacional/heurística, não como probabilidade matemática, salvo quando o método de calibração estiver documentado.

### 5.2 Estado abstrato

O estado deve ser modelado como produto reduzido:

$$
State = Intervals \times Congruence \times Nullability \times Cardinality \times Allocation \times Escape \times Alias \times Concurrency
$$

Um `AbstractValue` deve poder representar, quando conhecido:

```text
Type
BitWidth
Signedness
Range
Congruence
Constant/Symbol
NullState
PointsTo
Cardinality
ElementSize
AllocationSite
EscapeState
SideEffectState
```

Para provas de transformação, a camada rápida baseada em intervalos deve ser complementada por semântica de bit-vectors e um solver SMT. A camada SMT deve poder retornar `PROVEN`, `COUNTEREXAMPLE` ou `UNKNOWN/TIMEOUT`.

### 5.3 Estados de terminação

```text
TERMINATION_PROVEN
TERMINATION_PROVEN_UNDER_ASSUMPTIONS
NON_TERMINATION_PROVEN
NON_TERMINATION_POSSIBLE
TERMINATION_UNKNOWN
```

O produto nunca deve emitir `NON_TERMINATION_PROVEN` apenas porque não encontrou uma ranking function.

### 5.4 Contratos de entrada e limites confiáveis

Entradas cujo tamanho nasce de rede, arquivo, banco, parser, deserialização ou reflection devem carregar uma origem simbólica, por exemplo:

```text
Source = HttpBody
Symbol = payloadBytes
Bound = Unknown
Trust = External
```

O analisador deve aceitar limites vindos de quatro fontes, em ordem de especificidade:

1. **Prova local no CIL:** validação, comparação, slice, `Content-Length` ou limite de loop demonstravelmente dominante.
2. **Contrato de biblioteca/framework:** summary de `PipeReader`, `Stream`, serializer, `SqlDataReader` ou API conhecida.
3. **Contrato fornecido pelo usuário:** manifesto, atributo, arquivo de configuração ou regra de pipeline.
4. **Limite padrão do modo de análise:** somente para gerar upper bound declarado; nunca como fato observado.

Contratos precisam registrar origem, escopo, unidade, validade e confiança:

```yaml
contract:
  symbol: payloadBytes
  max: 2097152
  unit: bytes
  source: api-gateway-policy
  scope: DocumentEndpoint.Process
  expires: 2027-01-01
  evidence: EXTERNAL_CONTRACT
```

Um contrato externo reduz o upper bound apenas como `PROVEN_UNDER_ASSUMPTIONS` ou `STATIC_UPPER_BOUND`; ele não deve virar `PROVEN` sobre todo ambiente.

## 6. Pipeline de análise

### Fase A — Ingestão e identificação

- Validar o caminho e o tipo do arquivo.
- Calcular SHA-256 antes da análise.
- Detectar PE inválido, metadata malformada, assembly misto, NativeAOT e R2R.
- Identificar TFM, arquitetura, versão, strong name, referências e PDB.
- Carregar configurações de runtime disponíveis, incluindo `System.GC.LOHThreshold` quando fornecidas.
- Definir política para input não confiável: limites de tamanho, tempo, memória e cancelamento.

**Saída:** `AssemblyModel` e diagnóstico de suporte.

### Fase B — Metadata e CIL

- Ler metadata com `PEReader`/`MetadataReader`.
- Obter method bodies e regiões de exceção.
- Decodificar todos os opcodes suportados.
- Representar tokens e assinaturas sem resolver tudo via execução/reflection.
- Validar evaluation stack, tipos, alturas e destinos de branch.
- Preservar offsets IL originais para rastreabilidade.

**Saída:** `MethodBodyModel` e warnings de decodificação.

### Fase C — IR normalizada

- Converter stack machine para operações tipadas.
- Criar valores SSA-like para locals, argumentos, constantes e resultados.
- Representar `phi` de valores e `MemoryPhi` quando necessário.
- Marcar `CanThrow`, `HasSideEffect`, `IsVolatile`, `MayAllocate` e `MayBlock`.
- Conservar instrução original junto ao nó normalizado.

**Saída:** IR estável para todos os passes.

### Fase D — CFG e análise de fluxo

Construir:

```text
CFG normal:      E_N
CFG excepcional: E_X
Dominator tree
Post-dominator tree
SCCs/cycles
Backedges
Natural loops
Loop nesting forest
Exception regions
```

Loops irreducíveis e ciclos sem header dominante devem ser preservados como `cycle`/`irreducible-flow`, não classificados automaticamente como loops naturais.

**Saída:** grafo por método com consultas determinísticas.

### Fase E — Call graph e summaries

Classificar cada chamada em:

1. Corpo disponível: analisar IL.
2. Método conhecido sem corpo: usar summary versionado.
3. Método desconhecido: usar `UnknownEffect` conservador.

`UnknownEffect` pode incluir:

```text
MayAllocate
MayThrow
MayWriteMemory
MayRetainArguments
MayBlock
MaySpawnWork
UnknownCpu
```

O banco de summaries deve suportar:

- complexidade temporal;
- alocação e cardinalidade;
- efeitos de memória;
- retenção/escape;
- enumeração/materialização;
- exceções;
- bloqueio e concorrência;
- pré-condições e versão do runtime.

### Fase F — Interpretação abstrata

- Inicializar estado no entry block.
- Aplicar funções de transferência por opcode e summary.
- Fazer `join` em convergências.
- Iterar até fixpoint.
- Aplicar widening quando necessário.
- Aplicar narrowing opcionalmente para refinar resultados.
- Registrar assumptions e perda de precisão.
- Invalidar resultados quando uma operação externa é desconhecida.

Exemplo de função de transferência:

$$
F_{add}([a,b],[c,d]) = [a+c,b+d]
$$

Para aritmética CIL, a implementação deve respeitar overflow, checked context e largura do tipo.

### Fase G — Scalar evolution e loops

Representar recorrências como:

```text
{Start,+,Step}<LoopId>
```

Derivar quando possível:

```text
BackedgeTakenCount
TripCount
MinTripCount
MaxTripCount
InductionVariable
OverflowCondition
TerminationStatus
```

A distinção entre `TripCount` e `BackedgeTakenCount` deve ser mantida no modelo e no relatório.

### Fase H — Memória, alocação e retenção

Separar:

```text
AllocationVolume
LiveManagedMemory
RetainedMemory
PeakWorkingSetEstimate
NativeMemory
```

Analisar:

- `newobj`, `newarr`, `box`;
- materialização LINQ e cópia de buffers;
- tamanho simbólico de arrays/strings;
- escape local, método, thread, global ou desconhecido;
- static fields, eventos, closures, caches e `GCHandle`;
- alias e interferência em fields/arrays;
- LOH com threshold parametrizado;
- coexistência de buffers e amplificação;
- concorrência multiplicando memória de trabalho.

### Fase I — Custo e findings

Para CPU:

$$
C(B)=\sum_{i\in B} w(i)
$$

$$
C(Method)=\sum_{B\in CFG} Visits(B)\times C(B)
$$

O peso inicial deve ser `Structural Cost Unit`, não tempo absoluto. Calls, alocações e efeitos externos devem permanecer simbólicos quando não houver modelo calibrado.

Para alocação:

$$
AllocatedBytes = \sum_i Size_i \times Executions_i
$$

Para retenção:

$$
M(t) \approx M_0 + \lambda s t
$$

Para paralelismo:

$$
PeakParallel \leq Base + P \times PeakTask
$$

Para filas:

$$
L = \lambda W
$$

Toda fórmula precisa declarar suas hipóteses e seu nível de evidência.

### 6.1 Modo pessimista e análise de pior caso

O modelo deve oferecer três perspectivas, selecionáveis e exibidas separadamente no relatório:

```text
typical       usa valores conhecidos/observados e evita extrapolação agressiva
pessimistic   calcula upper bounds sob entradas externas e branches mais caros
observed      sobrepõe medições de runtime e workload identificado
```

O modo `pessimistic` não deve afirmar que o pior caso ocorrerá. Ele deve responder: “qual é o maior custo que permanece compatível com as informações disponíveis?”.

#### Entradas desconhecidas

Para cada valor originado de I/O, o analisador deve propagar um símbolo e uma classificação:

```text
BoundedByCode
BoundedByContract
BoundedByRuntime
UnknownExternal
Unbounded
```

Sem limite local ou contrato confiável, um `byte[]`, string, coleção ou objeto desserializado deve gerar um upper bound simbólico `UnknownExternal`, e não um número inventado. Quando o tipo/runtime possui um limite físico conhecido, esse limite pode ser usado apenas como cenário de saturação e deve ser rotulado `STATIC_UPPER_BOUND`.

Exemplo:

```text
ReadToEnd() -> payloadBytes
newarr byte[payloadBytes]

Worst-case:
  payloadBytes = UnknownExternal
  allocation = O(payloadBytes)
  peak = at least allocation + live predecessors
  evidence = STATIC_UPPER_BOUND / UNKNOWN
```

#### Branches

No relatório pessimista, o custo de um ponto de decisão deve usar o máximo entre os caminhos alcançáveis:

$$
C_{worst}(if)=\max(C_{then},C_{else})
$$

Para memória, o cálculo deve usar o pico de cada caminho e os objetos que coexistem antes da junção. Para custo esperado, somente o modo observado/calibrado pode usar probabilidades de branch.

#### Loops sem bound

Um loop cujo progresso ou condição depende de I/O, estado mutável desconhecido, callback ou método sem summary deve produzir:

```text
TripCount = Unknown
Complexity = O(Unbounded)
Evidence = UNKNOWN ou STATIC_UPPER_BOUND
Termination = TERMINATION_UNKNOWN
```

`O(Unbounded)` é uma classificação de risco, não uma prova de não terminação. O finding deve recomendar um contrato, uma validação explícita ou um limite operacional.

#### I/O materialization

Adicionar a regra `MEM012 Unbounded I/O materialization` para detectar combinações como `ReadToEnd`, desserialização em memória, `ToArray`, `ToList`, cópia de stream e `newarr` dependente de payload. O finding deve mostrar a origem do dado, o primeiro ponto de materialização, os bounds encontrados ou ausentes, a fórmula de alocação e sua relação com LOH, heap limit e concorrência.

#### Cenários, não apenas um número

O relatório deve permitir comparar cenários independentes:

```text
baseline: limite contratado de 2 MiB, concorrência 8
stress:    limite de 32 MiB, concorrência 32
untrusted: sem limite conhecido
```

Assim, o cenário pessimista não é confundido com a expectativa de produção.

## 7. Catálogo inicial de regras

### CPU

```text
CPU001 Excessive loop nesting
CPU002 Nested collection scan / quadratic traversal
CPU003 Recursive SCC with cost amplification
CPU004 Busy wait or progress-free loop
CPU005 Repeated expensive call in loop
CPU006 Reflection or dynamic invocation in loop
CPU007 Regex construction in loop
CPU008 Serialization/materialization in loop
CPU009 Unbounded recursive branching
```

### Memória e GC

```text
MEM001 Allocation inside loop
MEM002 Large/LOH allocation
MEM003 Repeated boxing
MEM004 String concatenation in loop
MEM005 Enumeration materialization in loop
MEM006 Potential unbounded static retention
MEM007 Event/closure retention
MEM008 Buffer duplication
MEM009 Memory amplification
MEM010 Allocation volume growth
MEM011 Native/unmanaged retention risk
MEM012 Unbounded I/O materialization
REL001 Missing external bound contract
```

### Async e concorrência

```text
ASYNC001 Sync-over-async
ASYNC002 Blocking Wait/Result
ASYNC003 Uncontrolled Task creation
ASYNC004 Missing cancellation propagation
CONC001 Unbounded parallelism
CONC002 Excessive fan-out
CONC003 Memory multiplied by concurrency
CONC004 Contention-sensitive region
CPU010 Worst-case branch amplification
```

A regra deve emitir recomendação, não reescrita automática, quando o problema for algorítmico, de ownership, de API ou de arquitetura.

## 8. Modelo de finding e relatório

### 8.1 Remediação

```text
AUTO_FIX
REVIEW
SOURCE_CHANGE
INFORMATIONAL
```

`AUTO_FIX` só é permitido para regras com pré-condições verificadas e prova de preservação. `REVIEW` é apropriado para summaries, recomendações algorítmicas e conclusões dependentes de comportamento externo. `SOURCE_CHANGE` é obrigatório para mudanças de ownership, pooling, async, paralelismo e estruturas de dados.

### 8.2 Formatos

Gerar:

```text
analysis.json   contrato primário versionado
analysis.sarif  integração CI/CD
analysis.md     leitura humana e revisão
analysis.html   navegação por findings, CFG e call graph
```

O JSON deve conter pelo menos:

```json
{
  "schemaVersion": "0.1",
  "assembly": {
    "name": "...",
    "sha256": "...",
    "targetFramework": "..."
  },
  "analysis": {
    "toolVersion": "...",
    "runtimeModel": "...",
    "durationMs": 0,
    "status": "completed"
  },
  "summary": {
    "methods": 0,
    "instructions": 0,
    "loops": 0,
    "allocations": 0,
    "cpuRisk": 0,
    "memoryRisk": 0
  },
  "findings": []
}
```

Cada finding deve incluir localização, IL, fórmula, assumptions, evidence kind, confidence, causa, impacto, recomendação, remediação e limitações.

### 8.3 Hot Path Score

O score é apenas priorização heurística e deve ser rotulado como tal. Uma forma inicial:

$$
RiskScore = SeverityWeight \times FrequencyFactor \times CostFactor \times ConfidenceFactor
$$

O relatório deve exibir os fatores separadamente para evitar que o score pareça um teorema ou uma medida física.

### 8.4 Dicas do desenvolvedor e supressões auditáveis

O pessimismo só é útil se o sistema permitir reduzir incerteza sem silenciar riscos. O CLR Lens deve suportar três mecanismos complementares:

1. **Contratos positivos:** informam um limite que pode ser verificado localmente ou assumido externamente.
2. **Summaries de ambiente:** descrevem limites de gateway, broker, banco, serializer, container ou plataforma.
3. **Supressões:** aceitam um finding conhecido, mas exigem justificativa e não alteram silenciosamente o modelo.

Exemplo conceitual de contrato no código:

```csharp
[ClrLensMaxLength("payload", 2 * 1024 * 1024,
    Source = "api-gateway", Expires = "2027-01-01")]
```

O atributo é uma dica/contrato, não uma prova automática. Se o analisador conseguir ligar a anotação ao valor e verificar o fluxo até a materialização, a evidência pode ser `PROVEN_UNDER_ASSUMPTIONS`. Caso contrário, permanece `EXTERNAL_CONTRACT` e o relatório informa que a garantia depende da infraestrutura.

Exemplo de supressão versionada:

```yaml
suppression:
  id: MEM012
  target: Namespace.Type::Process
  reason: "Gateway limita o corpo a 2 MiB e rejeita Content-Length ausente"
  owner: platform-team
  ticket: REL-1842
  expires: 2027-01-01
  scope: analysis
```

Regras de governança:

- supressão sem `reason`, `owner`, `scope` e `expires` é inválida;
- supressão não pode converter `UNKNOWN` em `PROVEN`;
- o finding suprimido permanece no JSON com `suppressed: true`;
- o SARIF deve registrar a supressão e sua justificativa;
- supressões expiradas voltam a falhar o gate;
- `--fail-on` deve poder falhar por finding ativo, contrato ausente ou contrato expirado;
- CI deve poder exigir revisão para alterações no arquivo de contratos/supressões;
- uma supressão ampla de assembly deve gerar warning de governança.

O relatório deve mostrar:

```text
Finding: MEM012
Status: SUPPRESSED_UNDER_EXTERNAL_CONTRACT
Contract: api-gateway payload <= 2 MiB
Verification: not proven in assembly
Owner: platform-team
Expires: 2027-01-01
```

## 9. Fases e entregáveis

### V0.1 — Fundamentos do analisador

**Entregáveis:**

- solução .NET e contratos de domínio;
- leitura de PE/metadata;
- decoder CIL;
- validação de evaluation stack;
- IR normalizada;
- CFG normal e excepcional;
- loops, dominadores e SCC;
- call graph básico;
- detecção de alocação;
- primeiros findings CPU/MEM;
- JSON e SARIF;
- fixtures de assemblies válidos, inválidos e com EH.

**Critério de saída:** analisar uma DLL de exemplo sem executar seu código, rastrear cada finding até offsets IL e gerar saída determinística.

### V0.2 — Análise matemática

**Entregáveis:**

- lattice/abstract state;
- intervals e congruence;
- constant propagation;
- liveness e use-def;
- widening/narrowing;
- scalar evolution;
- trip counts com overflow explícito;
- estados de terminação;
- complexity inference;
- summaries iniciais para `List<T>`, arrays, LINQ materializing e Tasks.
- classificação de origem de entrada (`BoundedByCode`, `BoundedByContract`, `UnknownExternal`);
- modo `pessimistic` para branches, loops e materialização de I/O;
- schema inicial de contratos e supressões com expiração.

**Critério de saída:** provar ou classificar corretamente exemplos lineares, quadráticos, bounds desconhecidos, loops com overflow, loops condicionais e payloads externos sem limite.

### V0.3 — Memória semântica

**Entregáveis:**

- abstract heap;
- escape e retention;
- MemorySSA-like com `MemoryDef`, `MemoryUse` e `MemoryPhi`;
- alias conservador;
- allocation/live/retained/peak separados;
- LOH threshold parametrizado;
- amplificação por pipeline;
- concorrência e upper bounds de memória;
- findings MEM/GC/CONC enriquecidos.
- análise de cenários `baseline`, `stress` e `untrusted`;
- regra MEM012 para materialização sem limite;
- upper bounds condicionais à concorrência e ao heap limit.

**Critério de saída:** distinguir alocação temporária de retenção estática em fixtures conhecidas e emitir upper bounds pessimistas sem apresentá-los como working set observado.

### V0.4 — Relatório operacional

**Entregáveis:**

- Markdown e HTML;
- grafos CFG/call graph;
- visão por assembly, tipo, método e finding;
- explicação humana “Why this matters”;
- filtros por severidade, evidence kind e remediação;
- manifesto de ambiente para CPU, memória, paralelismo e runtime.
- visualização dos bounds usados, contratos aplicados e supressões ativas/expiradas;
- explicação da diferença entre cenário típico, pessimista e observado.

**Critério de saída:** um engenheiro consegue localizar causa, evidência, modelo, impacto e próxima ação sem ler o código interno do analisador.

### V0.5 — Reescrita conservadora

**Entregáveis:**

- integração de writer, preferencialmente Cecil;
- passes: NOP cleanup, constant folding, branch folding, unreachable block elimination, redundant local copy;
- prova local e proof object;
- preservação de EH, debug info quando possível e API pública;
- política de cópia da DLL original;
- detecção de strong name e fluxo de reassinatura explícito;
- tratamento de R2R: análise permitida, rewrite bloqueado ou rebuild/strip explícito.

**Critério de saída:** cada transformação gera diff IL, pré-condições, resultado de prova e falha segura.

### V0.6 — Verificação e validação

**Entregáveis:**

- ILVerify no pipeline de saída;
- semantic diff para retorno, exceções e efeitos observáveis modeláveis;
- SMT bit-vector para regras aritméticas;
- counterexample report;
- testes diferenciais original/otimizada;
- testes de regressão para assinado/não assinado, overflow, `div`, shifts, null e EH.

**Critério de saída:** uma transformação incorreta é rejeitada ou reporta contraexemplo; assembly inválido nunca é publicado como resultado bem-sucedido.

### V1 — Runtime calibration e PGO overlay

**Entregáveis:**

- importação de EventPipe/nettrace;
- integração opcional com `dotnet-counters` e `dotnet-trace`;
- overlay de frequências no CFG;
- correlação de métodos/IL quando possível;
- comparação static vs observed;
- relatório de divergência entre modelo e runtime;
- benchmark A/B separado de prova semântica.

**Critério de saída:** o relatório diferencia custo estrutural, estimativa calibrada e medição observada, incluindo configuração do runtime e workload.

## 10. Estratégia de testes

### 10.1 Testes unitários

- decoder de cada família de opcode;
- evaluation stack e tipos;
- branch targets;
- EH e filtros;
- dominadores, post-dominadores e SCC;
- loops naturais e fluxo irreduzível;
- lattice operations, `join`, widening e narrowing;
- transfer functions;
- aritmética signed/unsigned e overflow;
- serialização do contrato JSON/SARIF.

### 10.2 Fixtures de assemblies

Criar assemblies pequenos e controlados para:

- loop linear, aninhado e desconhecido;
- `int32` overflow checked/unchecked;
- arrays com tamanho constante e simbólico;
- alocação SOH e LOH;
- cache static com e sem remoção;
- eventos e closures;
- `Task.Run`, `WhenAll`, `Parallel` e semaphore;
- calls que lançam exceções;
- `try/catch/finally/filter`;
- payloads externos sem limite, `ReadToEnd`, `ToArray` e desserialização direta;
- payloads limitados por contrato local e por contrato externo;
- branches com custos assimétricos e cenários `typical`/`pessimistic`;
- contratos expirados, supressões válidas e supressões sem justificativa;
- generics, boxing, delegates e virtual dispatch;
- assemblies strong-named;
- R2R e NativeAOT como casos de suporte limitado.

### 10.3 Testes de propriedade

- CFG não perde instruções alcançáveis.
- Todo branch aponta para um bloco válido.
- A soma de instruções dos blocos corresponde ao corpo decodificado.
- O resultado é determinístico.
- `join` é monotônico.
- A análise converge dentro do limite configurado.
- Um pass não publica IR com stack inconsistente.
- JSON produzido valida contra o schema.

### 10.4 Testes de reescrita

Para cada regra automática:

1. testar transformação positiva;
2. testar pré-condição ausente;
3. testar exceções e side effects;
4. testar overflow e bit width;
5. executar ILVerify;
6. comparar resultados e exceções;
7. confirmar que a DLL original não foi modificada;
8. confirmar que a prova/contraexemplo foi persistida.

### 10.5 Performance do próprio analisador

Medir:

```text
tempo por método;
memória do processo;
tempo de construção do CFG;
tempo de fixpoint;
tempo de summaries;
tempo de serialização;
cache hit/miss;
```

O analisador deve impor limites configuráveis para assemblies muito grandes ou maliciosos e retornar diagnóstico parcial claramente identificado.

## 11. Critérios de qualidade do produto

- Zero dependência de execução do assembly analisado.
- Nenhum `RiskScore` apresentado sem seus fatores.
- Nenhuma afirmação de vazamento baseada somente em `newobj`.
- Nenhum `O(N)`/`O(N²)` sem parâmetros e assumptions identificados.
- Nenhuma reescrita automática em presença de `UnknownEffect` relevante.
- Todo resultado `PROVEN` deve apontar para regra, modelo e pré-condições.
- Todo resultado `OBSERVED` deve registrar workload, runtime, ferramenta e timestamp.
- Todo output otimizado deve ser validado antes de ser considerado sucesso.
- Mudanças de versão do runtime model ou knowledge base devem ser registradas no relatório.

## 12. Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| CIL malformado ou hostil | crash/DoS | limites de recurso, parser defensivo, modo seguro, diagnóstico parcial |
| Metadata externa incompleta | baixa precisão | `UnknownEffect`, summaries versionados e confidence reduzida |
| EH ignorado | prova incorreta | `E_X` desde V0.1 e testes intensivos de try/catch/finally |
| Overflow modelado como inteiro matemático | transformação incorreta | bit-vectors e distinção checked/unchecked |
| Alias impreciso | falso positivo/negativo | análise conservadora e bloqueio de rewrite quando necessário |
| LOH hardcoded | relatório incorreto | `RuntimeModel.LOHThreshold` parametrizado |
| R2R reescrito sem rebuild | binário incoerente | detectar R2R e bloquear rewrite inseguro |
| Strong name invalidado | falha de carga/identidade | detectar, copiar, reassinar somente por opção explícita |
| Confundir alocação com retenção | diagnóstico errado | quatro grandezas de memória separadas |
| Competir com JIT/PGO | posicionamento inadequado | foco em causa algorítmica, memória e semântica |
| SMT timeout | falsa certeza | retornar `UNKNOWN`, preservar candidate e logar timeout |
| Score “matemática fake” | perda de confiança | rotular score como heurístico e expor composição |
| Pessimismo excessivo | fadiga por falsos positivos | separar modo `pessimistic`, cenário e evidence kind; exigir contracts para reduzir bounds |
| Contrato externo incorreto | falsa sensação de segurança | registrar origem/expiração, marcar `PROVEN_UNDER_ASSUMPTIONS` e manter opção de cenário `untrusted` |
| Supressão permanente | risco oculto no CI | owner, ticket, escopo, expiração e finding preservado no JSON/SARIF |
| Divergência entre IL e RyuJIT | finding não reproduzido no hardware | separar custo estrutural de medição e usar profile overlay |

## 13. Interface de uso planejada

```text
clrlens analyze input.dll --out report/
clrlens inspect input.dll --method Namespace.Type::Method
clrlens graph input.dll --method Namespace.Type::Method --format dot
clrlens optimize input.dll --out optimized.dll --mode conservative
clrlens verify optimized.dll --references refs/
clrlens diff original.dll optimized.dll --proof proof.json
clrlens profile input.dll --trace workload.nettrace --out calibrated/
```

Opções globais previstas:

```text
--analysis-mode conservative|balanced|permissive
--cost-mode typical|pessimistic|observed
--runtime-model auto|netframework|net6|net8|net9|net10
--loh-threshold bytes
--container-memory bytes
--expected-concurrency N
--contracts path/to/contracts.yaml
--suppressions path/to/suppressions.yaml
--scenario baseline|stress|untrusted
--timeout duration
--max-memory bytes
--no-pdb
--include-private
--fail-on severity
```

Políticas recomendadas para CI:

```text
--fail-on critical
--fail-on unbounded-input
--fail-on expired-contract
--require-suppression-owner
--require-suppression-ticket
```

## 14. Decisões técnicas iniciais

- **Leitura:** `System.Reflection.Metadata`/`PEReader` para acesso de baixo nível e metadata conforme CLI.
- **Reescrita:** Mono.Cecil, isolado atrás de `IAssemblyWriter`.
- **Verificação:** ILVerify como gate estrutural pós-escrita.
- **Provas:** SMT bit-vectors, preferencialmente Z3, isolado atrás de `ISemanticProver`.
- **Modelo:** IR própria; não expor tipos de Cecil ao restante da biblioteca.
- **Runtime:** EventPipe como fonte primária de trace; `dotnet-counters` para métricas e `dotnet-trace` para coleta/inspeção operacional.
- **Relatório:** JSON versionado como contrato primário; SARIF como integração CI/CD.
- **Knowledge base:** YAML/JSON versionado, com testes de summary e origem/documentação.
- **Licenciamento:** verificar compatibilidade das dependências antes de distribuir binários ou incorporar código.

## 15. Definition of Done por feature

Uma feature de análise somente está pronta quando:

- há modelo de domínio e contrato de saída;
- há fixture positiva, negativa e desconhecida;
- há testes unitários e pelo menos um teste de integração;
- o finding contém localização e evidência;
- assumptions e limitações são exibidas;
- a classificação epistemológica está correta;
- o comportamento em timeout/metadata incompleta é seguro;
- JSON e SARIF permanecem compatíveis;
- a documentação da regra inclui fórmula e recomendação;
- não há dependência acidental de execução do assembly.

Uma feature de reescrita somente está pronta quando, adicionalmente:

- pré-condições são verificadas;
- efeitos excepcionais e memória são considerados;
- há prova ou justificativa formal dentro do escopo da regra;
- ILVerify passa com referências explícitas;
- testes diferenciais passam;
- o diff IL e o proof object são persistidos;
- a entrada não é sobrescrita;
- strong name, R2R e debug information têm política explícita.

## 16. Referências técnicas

- [ECMA-335 — Common Language Infrastructure](https://ecma-international.org/publications-and-standards/standards/ecma-335/): arquitetura CLI, metadata e conjunto CIL.
- [Cousot & Cousot — Abstract interpretation](https://www.di.ens.fr/~cousot/COUSOTpapers/POPL77.shtml): lattices, funções monotônicas e fixpoints.
- [LLVM Loop Terminology](https://llvm.org/docs/LoopTerminology.html): dominância, headers, backedges, trip count, loops irreduzíveis e LCSSA.
- [LLVM Analysis and Transform Passes](https://www.llvm.org/docs/Passes.html): referências para Scalar Evolution, alias, dominators e passes.
- [LLVM MemorySSA](https://www.llvm.org/docs/MemorySSA.html): MemoryDef, MemoryUse, MemoryPhi e trade-offs de precisão.
- [System.Reflection.Metadata.MetadataReader](https://learn.microsoft.com/pt-br/dotnet/api/system.reflection.metadata.metadatareader?view=net-10.0): leitura de metadata CLI e uso com PEReader.
- [Mono.Cecil](https://github.com/jbevain/cecil): inspeção e modificação de assemblies CIL.
- [ILVerify](https://github.com/dotnet/runtime/tree/main/src/coreclr/tools/ILVerify): verificação cross-platform de MSIL segundo ECMA-335.
- [Z3 Bitvectors](https://microsoft.github.io/z3guide/docs/theories/Bitvectors/): aritmética signed/unsigned e bit-vectors de largura fixa.
- [Alive2](https://github.com/AliveToolkit/alive2): referência conceitual para translation validation e counterexamples; não deve ser tratado como componente interprocedural do CLR.
- [EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe): eventos de runtime, GC, JIT e traces cross-platform.
- [dotnet-counters](https://learn.microsoft.com/pt-br/dotnet/core/diagnostics/dotnet-counters): CPU, allocation rate, GC heap, LOH, contention e working set.
- [Compilation config](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation): tiered compilation, ReadyToRun e Dynamic PGO.
- [GC configuration](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector): LOH threshold, heap hard limit e configurações de runtime.
- [LOH](https://learn.microsoft.com/pt-br/dotnet/standard/garbage-collection/large-object-heap): threshold padrão, coleta e implicações de alocações grandes.
- [Strong-named assemblies](https://learn.microsoft.com/pt-br/dotnet/standard/assembly/create-use-strong-named): identidade, assinatura e necessidade de política de reassinatura.

## 17. Resultado esperado

Ao final da primeira linha de desenvolvimento, o CLR Lens deverá responder, para uma DLL gerenciada arbitrária e sem código-fonte:

1. quais métodos e regiões IL concentram risco;
2. qual caminho de controle e chamada sustenta cada conclusão;
3. qual modelo matemático explica o custo;
4. se o resultado é prova, limite, inferência, estimativa, medição ou heurística;
5. quais recomendações exigem código-fonte;
6. quais transformações são candidatas a auto-fix;
7. por que uma transformação foi aceita, rejeitada ou ficou desconhecida;
8. como validar qualquer DLL produzida antes de distribuição.

A cadeia de entrega será:

$$
DLL \rightarrow CIL \rightarrow IR \rightarrow CFG \rightarrow DataFlow \rightarrow CostModel \rightarrow Diagnosis \rightarrow Proof \rightarrow Rewrite \rightarrow ILVerify \rightarrow A/B
$$

A prioridade é construir confiança no diagnóstico. A otimização automática será liberada apenas quando a cadeia de evidência e verificação for suficientemente forte para tornar o resultado tecnicamente defensável.
