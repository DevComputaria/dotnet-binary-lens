# PRD: CLR Lens

## 1. Visão geral do produto

### 1.1 Título e versão do documento

- **PRD:** CLR Lens
- **Versão:** 1.0
- **Status:** Proposta de produto
- **Data:** 2026-09-13
- **Produto:** CLR Binary Performance Analyzer + Verified IL Rewriter

### 1.2 Resumo do produto

O CLR Lens é uma biblioteca e uma CLI para análise estática de assemblies .NET gerenciados. A ferramenta lê PE, metadata, PDB quando disponível e CIL, constrói uma representação intermediária própria e analisa fluxo de controle, chamadas, loops, alocações, memória, GC, async e concorrência sem executar o assembly analisado.

O produto transforma uma DLL em uma autópsia de performance rastreável: cada finding deve indicar onde está o problema, por que ele pode ocorrer, qual modelo matemático sustenta a conclusão, qual é o impacto potencial e se a correção exige alteração no código-fonte ou pode ser aplicada com segurança ao IL.

A análise e o relatório são o produto principal. A reescrita automática de IL será opt-in, conservadora e posterior, condicionada à preservação semântica, validação com ILVerify e, quando aplicável, prova simbólica ou contraexemplo.

## 2. Objetivos

### 2.1 Objetivos de negócio

- Criar uma categoria de análise de performance baseada no binário distribuído, complementando analisadores de fonte e profilers dinâmicos.
- Atender equipes que possuem DLLs proprietárias, legadas ou sem código-fonte completo.
- Reduzir a descoberta tardia de riscos de CPU, memória, GC e concorrência em CI/CD.
- Fornecer evidências compreensíveis para desenvolvedores, arquitetos, SREs e equipes de plataforma.
- Criar uma base extensível para diagnósticos de confiabilidade e resiliência contra entradas de tamanho não limitado.
- Diferenciar claramente diagnóstico, estimativa, medição e otimização verificada.

### 2.2 Objetivos dos usuários

- Analisar uma DLL sem executar seu código.
- Localizar findings por assembly, tipo, método, token e intervalo IL.
- Entender o impacto por meio de fórmulas de complexidade, alocação, retenção e concorrência.
- Comparar cenários típico, pessimista e observado.
- Definir limites externos para payloads, coleções, concorrência e memória.
- Integrar findings ao GitHub Actions, Azure DevOps e outras ferramentas via SARIF/JSON.
- Inspecionar CFG, call graph, loops e caminhos de alocação.
- Aplicar apenas transformações IL comprovadamente seguras.
- Receber contraexemplos quando uma otimização proposta não for equivalente.

### 2.3 Fora de escopo

- Substituir o RyuJIT, tiered compilation ou Dynamic PGO.
- Garantir análise exata para todos os programas.
- Inferir intenção de negócio ou semântica ausente do assembly.
- Converter automaticamente LINQ em loops, coleções em dicionários, classes em structs, `Task` em `ValueTask` ou alterar paralelismo sem prova e autorização explícita.
- Analisar NativeAOT, mixed-mode C++/CLI ou código nativo como se fossem CIL gerenciado.
- Declarar vazamento, loop infinito ou ganho de performance apenas porque uma heurística não encontrou uma prova contrária.
- Executar assemblies analisados como parte da análise estática.

## 3. Personas e controle de acesso

### 3.1 Tipos de usuário

- **Desenvolvedor .NET:** investiga um método ou finding e corrige a causa no código-fonte.
- **Arquiteto de software:** avalia complexidade, retenção, amplificação e riscos estruturais de assemblies.
- **Engenheiro de plataforma/SRE:** relaciona upper bounds com limites de container, CPU, memória e concorrência.
- **Engenheiro de qualidade:** configura gates, thresholds, contratos e supressões no CI/CD.
- **Pesquisador/engenheiro de compiladores:** estende a IR, os domínios abstratos, summaries e provas.
- **Operador de release:** verifica uma DLL reescrita, sua assinatura, metadata e resultado de validação.

### 3.2 Detalhes das personas

- **Desenvolvedor .NET:** precisa de evidência IL e recomendação acionável, mas não necessariamente domina teoria de compiladores.
- **Arquiteto:** precisa comparar métodos, tipos, call graph e fórmulas de custo em nível de sistema.
- **SRE:** precisa saber se uma entrada externa ou paralelismo pode ultrapassar a capacidade de um pod.
- **Engenheiro de qualidade:** precisa de saída determinística e política de falha sem falsos positivos silenciosos.
- **Pesquisador:** precisa de contratos estáveis para CFG, SSA-like, MemorySSA-like e abstract state.
- **Operador de release:** precisa de uma cadeia verificável entre assembly original, transformação, prova e DLL de saída.

### 3.3 Acesso baseado em função

O CLR Lens é inicialmente uma ferramenta local/CLI e não exige autenticação para analisar arquivos. Em integrações compartilhadas, os seguintes papéis devem ser suportados:

- **Viewer:** lê relatórios e findings.
- **Analyst:** executa análises e exporta resultados.
- **Policy editor:** altera contratos, thresholds e supressões.
- **Release operator:** autoriza reescrita e reassinatura.
- **Administrator:** configura integrações, políticas globais e retenção de artefatos.

Alterações em contratos, supressões, chaves de assinatura e políticas de release devem ser auditáveis.

## 4. Requisitos funcionais

### 4.1 Ingestão segura de assemblies (Prioridade: crítica)

- Aceitar assemblies gerenciados .NET Framework, .NET 6, .NET 8, .NET 9 e .NET 10, desde que contenham CIL analisável.
- Ler PE e metadata com uma camada de acesso independente do rewriter.
- Calcular SHA-256 do input antes da análise.
- Detectar metadata inválida, assembly misto, NativeAOT, obfuscação suspeita e ReadyToRun.
- Não executar métodos, construtores, inicializadores ou callbacks do assembly analisado.
- Aplicar limites de tempo, memória, tamanho e cancelamento.
- Retornar diagnóstico parcial explícito quando a análise não puder ser concluída.

### 4.2 Decodificação e normalização CIL (Prioridade: crítica)

- Decodificar opcodes, operandos, tokens, assinaturas e method bodies.
- Validar evaluation stack, tipos, alturas e destinos de branch.
- Preservar offsets IL originais.
- Representar instruções em IR própria, sem expor diretamente tipos de Cecil ou de outra biblioteca.
- Marcar capacidade de lançar exceção, alocar, alterar memória, bloquear, ser volátil ou possuir efeitos observáveis.
- Suportar regiões de exceção, filtros, `try`, `catch`, `finally` e fluxo excepcional.

### 4.3 CFG, loops e grafos (Prioridade: crítica)

- Construir CFG com arestas normais e excepcionais: $CFG=(V,E_N,E_X)$.
- Calcular dominadores e post-dominadores.
- Identificar SCCs, backedges, loops naturais e árvore de aninhamento.
- Preservar ciclos irreduzíveis como ciclos, sem classificá-los indevidamente como loops naturais.
- Construir call graph e SCCs de chamadas.
- Expor CFG e call graph em JSON, Markdown, HTML e formato GraphViz opcional.

### 4.4 Representação abstrata e data flow (Prioridade: alta)

- Implementar valores SSA-like e use-def chains.
- Representar estado abstrato como produto reduzido de intervals, congruence, nullability, cardinality, allocation, escape, alias e concurrency.
- Implementar `join`, widening, narrowing e iteração até fixpoint.
- Aplicar funções de transferência por opcode.
- Registrar assumptions, perda de precisão e origem de cada conclusão.
- Distinguir análise rápida por intervalos de prova precisa por bit-vectors/SMT.

### 4.5 Scalar evolution, bounds e terminação (Prioridade: alta)

- Representar induction variables como `{Start,+,Step}<LoopId>`.
- Calcular `BackedgeTakenCount`, `TripCount`, mínimos, máximos e condições de overflow quando possível.
- Distinguir `TripCount` de `BackedgeTakenCount`.
- Reportar terminação como `TERMINATION_PROVEN`, `TERMINATION_PROVEN_UNDER_ASSUMPTIONS`, `NON_TERMINATION_PROVEN`, `NON_TERMINATION_POSSIBLE` ou `TERMINATION_UNKNOWN`.
- Nunca inferir não terminação apenas pela ausência de uma ranking function.

### 4.6 Method Summary Database (Prioridade: alta)

- Classificar chamadas como corpo disponível, método conhecido sem corpo ou método desconhecido.
- Permitir summaries versionados para BCL, LINQ, coleções, serializadores, streams, Tasks e APIs de concorrência.
- Modelar complexidade, alocações, cardinalidade, enumeração, materialização, efeitos de memória, exceções, retenção, bloqueio e spawn de trabalho.
- Permitir summaries condicionais por tipo concreto, interface, runtime e pré-condição.
- Usar `UnknownEffect` conservador quando não houver corpo nem summary.
- Registrar origem e versão de cada summary utilizada.

### 4.7 Modelo de CPU e complexidade (Prioridade: alta)

- Calcular custo estrutural por `Structural Cost Unit`, sem apresentá-lo como tempo absoluto.
- Inferir padrões como $O(1)$, $O(N)$, $O(NM)$, $O(N^2)$, $O(N^3)$, $O(N\log N)$, $O(2^N)$ e `unknown` quando suportado pelas evidências.
- Calcular custo por bloco e frequência estimada.
- Detectar loops aninhados, scans repetidos, busy waits, recursão e chamadas caras em loops.
- Permitir overlay de frequência observada via EventPipe/nettrace.
- Exibir diferença entre custo estático, custo calibrado e medição observada.

### 4.8 Modelo de memória e GC (Prioridade: crítica)

- Separar `AllocationVolume`, `LiveManagedMemory`, `RetainedMemory`, `PeakWorkingSetEstimate` e `NativeMemory`.
- Detectar `newobj`, `newarr`, `box`, boxing repetido, materialização LINQ, cópias e buffers duplicados.
- Modelar escape como local, método, thread, global ou desconhecido.
- Construir abstract heap com GC roots, static fields, coleções, objetos e referências.
- Detectar retenção potencial por static collections, eventos, closures, caches, singletons e `GCHandle`.
- Parametrizar o threshold de LOH, usando 85.000 bytes apenas como default documentado.
- Calcular amplificação de memória por pipeline e por concorrência.
- Diferenciar upper bound estático de working set observado.

### 4.9 Análise de I/O e cenários pessimistas (Prioridade: crítica)

- Suportar modos `typical`, `pessimistic` e `observed`.
- Propagar origem simbólica de valores provenientes de rede, arquivo, banco, parser, reflection ou desserialização.
- Classificar entradas como `BoundedByCode`, `BoundedByContract`, `BoundedByRuntime`, `UnknownExternal` ou `Unbounded`.
- No modo pessimista, usar o branch alcançável mais caro e calcular upper bounds.
- Detectar `ReadToEnd`, `ToArray`, `ToList`, desserialização direta e `newarr` dependente de payload.
- Emitir `MEM012 Unbounded I/O materialization` quando não houver limite adequado.
- Gerar cenários independentes `baseline`, `stress` e `untrusted`.
- Nunca transformar um valor desconhecido em número arbitrário sem registrar a hipótese.

### 4.10 Contratos e dicas do desenvolvedor (Prioridade: alta)

- Aceitar contratos em YAML/JSON, atributos no código, manifesto de ambiente ou configuração de CI.
- Registrar símbolo, limite, unidade, origem, escopo, validade, owner e nível de confiança.
- Permitir contratos para payload, coleção, concorrência, memória, CPU e limites de gateway/container.
- Classificar evidência baseada em contrato como `EXTERNAL_CONTRACT`, `STATIC_UPPER_BOUND` ou `PROVEN_UNDER_ASSUMPTIONS`.
- Permitir preservar um cenário `untrusted` mesmo quando existe contrato de produção.
- Invalidar ou degradar contratos expirados.

### 4.11 Findings, severidade e remediação (Prioridade: crítica)

Cada finding deve conter:

- ID estável e categoria;
- severidade;
- confidence operacional;
- tipo, método, token e intervalo IL;
- blocos e call path relevantes;
- evidência CIL;
- fórmula ou modelo;
- assumptions;
- classificação epistemológica;
- impacto;
- “Why this matters”;
- recomendação;
- tipo de remediação: `AUTO-FIX`, `REVIEW`, `SOURCE-CHANGE` ou `INFORMATIONAL`.

Categorias iniciais:

```text
CPU001–CPU010
MEM001–MEM012
REL001
ASYNC001–ASYNC004
CONC001–CONC004
GC001–GC003
```

### 4.12 Relatórios e integrações (Prioridade: crítica)

- Gerar `analysis.json` como contrato primário versionado.
- Gerar `analysis.sarif` para CI/CD.
- Gerar `analysis.md` para revisão humana.
- Gerar `analysis.html` com navegação por assembly, tipo, método, finding, CFG e call graph.
- Incluir sumário executivo com métodos, instruções, loops, allocations, riscos e candidatos de auto-fix.
- Permitir filtros por severidade, evidence kind, remediação, cenário e status de supressão.
- Preservar findings suprimidos no JSON e SARIF.

### 4.13 Supressões e políticas CI/CD (Prioridade: alta)

- Aceitar supressões versionadas com ID, alvo, razão, owner, ticket, escopo e expiração.
- Rejeitar supressões incompletas.
- Exibir supressões expiradas como falha de política.
- Permitir gates por severidade, finding sem limite, contrato expirado e ausência de owner/ticket.
- Manter o finding original mesmo quando suprimido.
- Emitir warning para supressões amplas de assembly.

### 4.14 Reescrita conservadora de IL (Prioridade: média, opt-in)

- Criar candidatos de transformação apenas para regras com pré-condições verificadas.
- Incluir inicialmente constant folding, constant propagation, branch folding, remoção de NOPs, blocos inalcançáveis e cópias locais redundantes.
- Gerar diff IL antes/depois.
- Gerar proof object com regra, offsets, pré-condições e resultado.
- Preservar EH, metadata, tipos, stack, API pública e efeitos observáveis.
- Nunca sobrescrever o assembly original por padrão.
- Bloquear automaticamente transformações quando houver `UnknownEffect` relevante.

### 4.15 Translation validation e verificação (Prioridade: média, opt-in)

- Modelar semântica simbólica da região original e transformada.
- Usar bit-vectors para largura fixa e signed/unsigned.
- Retornar `PROVEN`, `COUNTEREXAMPLE` ou `UNKNOWN/TIMEOUT`.
- Persistir contraexemplo com entradas e resultados divergentes.
- Executar ILVerify após a escrita.
- Executar testes diferenciais para retornos, exceções e efeitos observáveis modeláveis.
- Validar compatibilidade de metadata e API pública.

### 4.16 Assinatura e formatos especiais (Prioridade: média)

- Detectar assemblies strong-named.
- Nunca reassinar silenciosamente.
- Permitir fluxo explícito de reassinatura com chave fornecida pelo operador.
- Detectar ReadyToRun e permitir análise, mas bloquear rewrite inseguro sem rebuild/strip.
- Rejeitar ou classificar NativeAOT e mixed-mode como não suportados.
- Preservar PDB quando possível e reportar perda de alinhamento quando não for possível.

## 5. Experiência do usuário

### 5.1 Pontos de entrada e primeiro fluxo

- CLI local: `clrlens analyze application.dll`.
- Pipeline CI/CD com artefato DLL e referências de runtime.
- IDE/portal futuro para abrir `analysis.html`.
- Fluxo inicial:
  1. Selecionar DLL, PDB opcional e referências.
  2. Selecionar `cost-mode` e cenário.
  3. Informar contratos e supressões opcionais.
  4. Executar análise sem carregar o assembly alvo.
  5. Exibir sumário e findings ordenados.
  6. Abrir evidência IL, CFG, call graph e fórmula.
  7. Exportar relatório ou falhar o gate conforme política.

### 5.2 Experiência principal

- **Resumo executivo:** mostra riscos e contagens sem esconder incerteza.
- **Exploração por método:** mostra complexidade, blocos, loops, calls, allocations, EH e scores.
- **Finding detalhado:** mostra evidência IL, modelo, assumptions, impacto e recomendação.
- **Mapa de performance:** mostra call graph e findings associados a cada nó.
- **Cenários:** compara typical, pessimistic, baseline, stress, untrusted e observed.
- **Governança:** mostra contratos aplicados, supressões, owners e expirações.
- **Rewrite opcional:** apresenta candidatos e exige aprovação explícita antes de gerar DLL.

### 5.3 Funcionalidades avançadas e casos extremos

- Assemblies sem PDB devem continuar analisáveis por offsets IL.
- Dependências ausentes devem gerar `UnknownEffect`, não falha silenciosa.
- Timeout de solver deve resultar em `UNKNOWN`.
- Metadata malformada deve gerar diagnóstico seguro e não crash.
- Loops irreduzíveis devem ser preservados como ciclos.
- Bounds desconhecidos devem manter cenário `untrusted`.
- Contratos expirados devem ser destacados e não aplicados como garantia vigente.
- Supressões não devem apagar findings do relatório.
- R2R deve ser analisado sem prometer que a reescrita será segura.

### 5.4 Destaques de UI/UX

- Evidência IL com offsets destacados.
- Fórmulas renderizadas em Markdown/HTML.
- Badges para `PROVEN`, `STATIC_UPPER_BOUND`, `ESTIMATED`, `OBSERVED`, `EXTERNAL_CONTRACT` e `UNKNOWN`.
- Cores consistentes para severidade e tipo de remediação.
- Filtros por cenário, categoria e método.
- Links entre finding, call path, CFG e summary utilizado.
- Avisos claros para “worst case”, “not observed” e “depends on external contract”.

## 6. Narrativa

Uma equipe recebe uma DLL de produção e não possui o código-fonte completo nem um workload representativo. O engenheiro executa o CLR Lens no pipeline e recebe um relatório que identifica um `newarr` dependente do tamanho de um payload externo dentro de um loop concorrente. O finding mostra os offsets IL, o call path, o modelo $M(N,P)$, o possível impacto no LOH e a diferença entre o limite contratado do gateway e o cenário sem confiança.

O arquiteto percebe que o problema é uma decisão de materialização e marca a remediação como `SOURCE-CHANGE`. Outro finding de branch constante é classificado como `AUTO-FIX`, recebe prova local, passa no ILVerify e gera uma DLL separada. O SRE usa o cenário `stress` para comparar o upper bound com o limite de memória do container, enquanto o CI bloqueia novos findings críticos e contratos expirados.

O benefício principal não é apenas “otimizar IL”; é transformar um binário opaco em evidência técnica, modelo de risco e plano de ação verificável.

## 7. Métricas de sucesso

### 7.1 Métricas centradas no usuário

- Pelo menos 90% dos findings críticos devem conter localização IL, causa, modelo e recomendação.
- Um engenheiro deve conseguir abrir um finding e identificar a próxima ação em até 5 minutos.
- Pelo menos 95% das análises concluídas devem produzir saída JSON válida e determinística.
- 100% das supressões devem exibir owner, justificativa e expiração.
- 100% dos findings baseados em contrato devem mostrar a origem do contrato.

### 7.2 Métricas de negócio

- Número de pipelines que executam análise por release.
- Número de assemblies analisados por semana.
- Taxa de adoção de `analysis.json` e SARIF.
- Percentual de findings que resultam em correção de código-fonte, contrato ou configuração.
- Redução de incidentes associados a OOM, pressão de GC ou loops patológicos após adoção.
- Retenção e expansão de usuários por equipe/plataforma.

### 7.3 Métricas técnicas

- Cobertura de opcodes e fixtures CIL.
- Precisão da detecção de CFG, EH, loops e call graph.
- Tempo e memória do analisador por tamanho de assembly.
- Taxa de convergência do abstract interpreter.
- Taxa de `UNKNOWN` por ausência de summary.
- Taxa de falsos positivos por regra, medida por revisão humana.
- Taxa de transformações rejeitadas por counterexample.
- 100% dos assemblies reescritos que passam pelo ILVerify antes de serem publicados.
- 0 alterações acidentais no assembly original.

## 8. Considerações técnicas

### 8.1 Pontos de integração

- `System.Reflection.Metadata` e `PEReader` para leitura.
- Mono.Cecil atrás de uma abstração para reescrita.
- ILVerify para validação estrutural.
- Z3 ou solver equivalente para bit-vectors e tradução simbólica.
- EventPipe, `dotnet-counters` e `dotnet-trace` para calibração.
- GitHub Actions e Azure DevOps via SARIF/JSON.
- GitHub Code Scanning e dashboards compatíveis com SARIF.
- Kubernetes/container manifest para CPU, memória e concorrência.
- Gateway/API policies para contratos de payload.

### 8.2 Armazenamento e privacidade

- A análise local deve funcionar sem enviar assemblies para serviços externos.
- Relatórios podem conter nomes de tipos, métodos, strings e caminhos; o usuário deve controlar retenção e publicação.
- Chaves privadas de strong name nunca devem ser incluídas no relatório.
- Traces e PDBs devem ser tratados como artefatos potencialmente sensíveis.
- JSON deve permitir redaction configurável de caminhos, nomes e valores de entrada.
- Contratos e supressões devem ser versionados e auditáveis.
- O produto não deve executar código de assemblies não confiáveis durante a análise estática.

### 8.3 Escalabilidade e desempenho

- Processar métodos independentemente sempre que possível.
- Usar cache para metadata, summaries e resultados imutáveis.
- Permitir análise incremental por hash do assembly e método.
- Impor limites de recursos para evitar DoS por arquivos malformados ou grafos enormes.
- Permitir paralelismo controlado na análise do próprio CLR Lens.
- Registrar tempo por fase: leitura, IR, CFG, fixpoint, summaries, findings e relatório.
- Evitar afirmar que SCU corresponde a nanosegundos ou CPU de uma arquitetura específica.

### 8.4 Desafios potenciais

- O halting problem limita bounds exatos e provas universais.
- I/O e métodos externos podem tornar tamanho, cardinalidade e custo desconhecidos.
- O RyuJIT pode otimizar ou eliminar custos aparentes no CIL.
- Strong names, PDBs, reflection, EH, volatile semantics e ReadyToRun tornam a reescrita sensível.
- Summaries incorretos podem gerar conclusões erradas; sua origem e versão precisam ser rastreadas.
- O modo pessimista pode gerar fadiga por falsos positivos se contratos não forem mantidos.
- A análise interprocedural pode crescer rapidamente em assemblies grandes.
- Provas SMT podem atingir timeout e devem falhar para `UNKNOWN`, nunca para `PROVEN`.

## 9. Marcos e sequência

### 9.1 Estimativa do projeto

- **Tamanho:** grande, com pesquisa aplicada em compiladores, análise estática, CLR e métodos formais.
- **Estimativa inicial:** 9–15 meses para um produto V1 utilizável, dependendo da equipe e da profundidade dos summaries/verificação.
- **MVP de diagnóstico:** 3–5 meses para leitura, IR, CFG, regras iniciais e JSON/SARIF.

### 9.2 Tamanho e composição da equipe

- **Equipe recomendada:** 4–6 pessoas.
- **Composição:** engenheiro de compiladores/CIL, engenheiro .NET, engenheiro de análise estática/formal, engenheiro de tooling/CI, QA/benchmark e, opcionalmente, especialista de runtime/GC.

### 9.3 Fases sugeridas

- **Fase 1 — Fundamentos do analisador:** 6–8 semanas.
  - Solução .NET, ingestão PE, metadata, decoder, stack validation, IR e fixtures.
- **Fase 2 — CFG e diagnóstico estrutural:** 6–8 semanas.
  - CFG normal/excepcional, dominadores, loops, SCC, call graph e findings iniciais.
- **Fase 3 — Análise matemática e summaries:** 8–12 semanas.
  - Abstract interpretation, ranges, scalar evolution, cardinalidade, bounds e summaries.
- **Fase 4 — Memória, cenários e relatório:** 8–10 semanas.
  - Abstract heap, escape, LOH, amplificação, contratos, pessimistic mode, HTML e SARIF.
- **Fase 5 — Reescrita conservadora:** 8–12 semanas.
  - Passes seguros, proof objects, Cecil, strong name e políticas de R2R.
- **Fase 6 — Verificação e calibração:** 8–12 semanas.
  - ILVerify, SMT, counterexamples, testes diferenciais, EventPipe e benchmark A/B.
- **Fase 7 — Beta e endurecimento:** 6–8 semanas.
  - Performance do analisador, segurança, compatibilidade, documentação, CI e avaliação com assemblies reais.

## 10. Histórias de usuário

### 10.1 Analisar uma DLL sem executar o assembly

- **ID:** CL-001
- **Descrição:** Como engenheiro .NET, quero analisar uma DLL sem executar seu código para identificar riscos com segurança.
- **Critérios de aceite:**
  - A análise aceita uma DLL gerenciada e opcionalmente um PDB.
  - Nenhum método, construtor ou inicializador do assembly alvo é executado.
  - O relatório registra hash, framework, arquitetura, referências e status de suporte.
  - Assemblies inválidos ou não suportados geram diagnóstico controlado.

### 10.2 Visualizar evidência CIL de um finding

- **ID:** CL-002
- **Descrição:** Como desenvolvedor, quero ver os offsets e instruções CIL associados a um finding para validar a conclusão.
- **Critérios de aceite:**
  - O finding mostra tipo, método, metadata token, IL inicial e final.
  - O relatório mostra as instruções relevantes e blocos relacionados.
  - O finding informa quando o código-fonte ou PDB não está disponível.
  - A evidência é preservada no JSON e no relatório humano.

### 10.3 Detectar complexidade quadrática

- **ID:** CL-003
- **Descrição:** Como arquiteto, quero identificar scans aninhados e receber o modelo $T(N,M)$ para priorizar correções algorítmicas.
- **Critérios de aceite:**
  - O analisador detecta loops aninhados quando seus bounds são inferíveis.
  - O finding informa parâmetros, fórmula e nível de evidência.
  - O resultado distingue `PROVEN`, `INFERRED`, `ESTIMATED` e `UNKNOWN`.
  - O sistema não reescreve automaticamente uma coleção para `Dictionary`.

### 10.4 Detectar alocação repetida e pressão de GC

- **ID:** CL-004
- **Descrição:** Como engenheiro de performance, quero identificar alocações dentro de loops e estimar volume de alocação.
- **Critérios de aceite:**
  - `newobj`, `newarr` e `box` são associados ao loop dominante quando possível.
  - O finding separa alocação total de memória viva e pico.
  - O modelo mostra tamanho por execução e frequência simbólica ou inferida.
  - O finding recomenda código-fonte quando ownership ou pooling são necessários.

### 10.5 Detectar retenção potencial

- **ID:** CL-005
- **Descrição:** Como SRE, quero identificar objetos potencialmente retidos por static fields, eventos ou caches sem eviction.
- **Critérios de aceite:**
  - O relatório mostra o GC root e o caminho de referências abstratas.
  - O sistema procura operações de remoção, clear ou eviction.
  - A conclusão é marcada como potencial quando a prova não for possível.
  - A fórmula de crescimento $M(t)$ é exibida quando houver taxa e tamanho simbólicos.

### 10.6 Avaliar memória por concorrência

- **ID:** CL-006
- **Descrição:** Como engenheiro de plataforma, quero multiplicar memória de trabalho pela concorrência configurada para avaliar risco de OOM.
- **Critérios de aceite:**
  - A análise aceita paralelismo inferido, contratado ou fornecido por configuração.
  - O resultado é marcado como `STATIC_UPPER_BOUND` quando for worst case.
  - O relatório separa `PeakUpperBound` de working set observado.
  - O usuário pode informar limite de memória do container.

### 10.7 Analisar payload externo sem limite conhecido

- **ID:** CL-007
- **Descrição:** Como engenheiro de confiabilidade, quero ser alertado quando um payload externo é materializado sem limite verificável.
- **Critérios de aceite:**
  - `ReadToEnd`, `ToArray`, desserialização direta e arrays dependentes de payload são rastreados.
  - O finding `MEM012` mostra a origem do dado e o primeiro ponto de materialização.
  - O custo permanece simbólico quando o tamanho é desconhecido.
  - O sistema recomenda streaming, chunking, quota ou limite na borda.

### 10.8 Comparar cenários de entrada

- **ID:** CL-008
- **Descrição:** Como arquiteto, quero comparar cenários baseline, stress e untrusted para não confundir limite operacional com garantia universal.
- **Critérios de aceite:**
  - O relatório suporta pelo menos esses três cenários.
  - Cada cenário mantém seus próprios bounds e assumptions.
  - O cenário `untrusted` continua disponível mesmo com contrato de produção.
  - O relatório identifica claramente diferenças entre cenários.

### 10.9 Definir contrato de tamanho

- **ID:** CL-009
- **Descrição:** Como engenheiro de plataforma, quero informar que um gateway limita payloads para reduzir falsos positivos de upper bound.
- **Critérios de aceite:**
  - O contrato informa símbolo, valor, unidade, origem, escopo, owner e expiração.
  - O relatório marca o uso como `EXTERNAL_CONTRACT` ou `PROVEN_UNDER_ASSUMPTIONS`.
  - O contrato expirado não é aplicado como garantia vigente.
  - A ferramenta mantém a opção de executar o cenário sem confiança externa.

### 10.10 Controlar supressões no CI

- **ID:** CL-010
- **Descrição:** Como engenheiro de qualidade, quero suprimir um finding conhecido sem apagá-lo e sem criar exceções permanentes.
- **Critérios de aceite:**
  - Supressões exigem razão, owner, ticket, escopo e expiração.
  - Findings suprimidos continuam no JSON/SARIF.
  - Supressões expiradas podem falhar o pipeline.
  - Alterações em supressões podem exigir revisão de código.

### 10.11 Exportar relatório SARIF

- **ID:** CL-011
- **Descrição:** Como responsável pelo CI, quero publicar findings no formato SARIF para integrar quality gates e dashboards.
- **Critérios de aceite:**
  - O arquivo SARIF é válido.
  - Cada finding possui regra, localização, severidade e descrição.
  - Suppressions e contratos aplicados são representados ou referenciados.
  - O pipeline pode falhar conforme severidade e política configuradas.

### 10.12 Inspecionar método e grafo de chamadas

- **ID:** CL-012
- **Descrição:** Como desenvolvedor, quero inspecionar um método específico e seus caminhos de chamada.
- **Critérios de aceite:**
  - A CLI aceita filtro por tipo e método.
  - O relatório exibe CFG, call path, loops, allocations e chamadas relevantes.
  - O usuário pode exportar o grafo em formato visualizável.
  - Métodos externos desconhecidos são marcados como tal.

### 10.13 Aplicar uma transformação IL segura

- **ID:** CL-013
- **Descrição:** Como operador de release, quero aplicar uma transformação conservadora somente quando suas pré-condições forem satisfeitas.
- **Critérios de aceite:**
  - A operação é opt-in e nunca sobrescreve a DLL original.
  - O resultado inclui diff IL, proof object e hash da entrada.
  - A transformação bloqueia quando há efeitos desconhecidos relevantes.
  - O assembly resultante passa pelo ILVerify antes de ser considerado válido.

### 10.14 Rejeitar transformação com contraexemplo

- **ID:** CL-014
- **Descrição:** Como pesquisador de compiladores, quero receber um contraexemplo quando uma transformação não preserva a semântica.
- **Critérios de aceite:**
  - O solver pode retornar `COUNTEREXAMPLE`.
  - O relatório mostra entradas, resultado original, resultado transformado e motivo.
  - O resultado não é publicado como DLL otimizada aprovada.
  - Timeout ou falta de modelo retorna `UNKNOWN`, não `PROVEN`.

### 10.15 Validar assembly reescrito

- **ID:** CL-015
- **Descrição:** Como operador de release, quero verificar metadata, IL e comportamento diferencial antes de distribuir a DLL.
- **Critérios de aceite:**
  - ILVerify é executado com referências necessárias.
  - A validação verifica retorno, exceções e efeitos observáveis modeláveis.
  - Strong name, R2R, PDB e API pública têm status explícito.
  - Falha em qualquer gate impede a publicação automática.

### 10.16 Calibrar análise com runtime

- **ID:** CL-016
- **Descrição:** Como engenheiro de performance, quero sobrepor dados EventPipe ao CFG estático para comparar estimativa e comportamento observado.
- **Critérios de aceite:**
  - O sistema aceita trace compatível e registra runtime/workload.
  - Frequências observadas são distinguidas de frequências inferidas.
  - O relatório mostra divergências entre modelo e medição.
  - Benchmark A/B não é apresentado como prova semântica.

### 10.17 Diferenciar custo do CIL e do JIT

- **ID:** CL-017
- **Descrição:** Como arquiteto, quero entender quando um finding é estrutural no CIL e quando depende do runtime/JIT.
- **Critérios de aceite:**
  - Relatórios distinguem `Structural Cost Unit`, `Runtime Calibrated Cost` e `Observed`.
  - O relatório declara que RyuJIT/PGO podem alterar custo de máquina.
  - Não são prometidos nanosegundos a partir de opcodes isolados.
  - Findings mantêm assumptions de runtime e arquitetura.

### 10.18 Proteger assemblies e artefatos sensíveis

- **ID:** CL-018
- **Descrição:** Como administrador, quero analisar assemblies proprietários sem enviar conteúdo sensível para serviços externos.
- **Critérios de aceite:**
  - A análise local funciona sem dependência de upload.
  - Chaves privadas nunca aparecem em logs ou relatórios.
  - Caminhos, nomes e valores sensíveis podem ser redacted.
  - Traces, PDBs e relatórios têm política de retenção configurável.

## 11. Dependências e decisões de lançamento

- A V0.1 deve priorizar leitura, IR, CFG, findings e JSON/SARIF.
- Abstract interpretation, summaries e cenários pessimistas entram antes da reescrita automática.
- Reescrita, SMT, ILVerify e testes diferenciais devem ser liberados apenas após o contrato de relatório estar estável.
- A base de summaries deve ser versionada junto com suas fontes e testes.
- O schema `analysis.json` deve ser versionado desde a primeira release.
- Mudanças que afetam evidência, severidade ou classificação epistemológica exigem revisão de compatibilidade.

## 12. Critérios de lançamento

### Release V0.1

- Análise estática sem execução do alvo.
- PE/metadata/CIL decoder funcional.
- IR, CFG normal/excepcional e call graph inicial.
- Findings de loops, alocações e riscos básicos.
- JSON/SARIF determinísticos.
- Fixtures e testes de metadata/EH.

### Release V0.2

- Abstract interpretation inicial.
- Range, cardinality, scalar evolution e bounds.
- Summaries iniciais.
- Modo pessimista e entradas desconhecidas.
- Contratos e supressões auditáveis.

### Release V0.3

- Abstract heap, escape, retention, LOH e concorrência.
- Relatórios HTML/Markdown com CFG e call graph.
- Cenários baseline/stress/untrusted.

### Release V0.4

- Reescrita conservadora opt-in.
- Proof objects.
- Strong-name/R2R policies.
- ILVerify integrado.

### Release V1.0

- Translation validation com SMT.
- Counterexamples.
- Testes diferenciais.
- EventPipe/profile overlay.
- Benchmark A/B e documentação operacional.

## 13. Referências e fundamentos

- ECMA-335 / CLI: semântica CIL, metadata, stack e verificação.
- Cousot & Cousot: abstract interpretation, lattices e fixpoints.
- CFG, dominators, post-dominators, SCCs, loops naturais e SSA.
- Scalar Evolution e MemorySSA como referências de projeto.
- Z3 bit-vectors para aritmética de largura fixa.
- ILVerify para validação de MSIL.
- EventPipe, `dotnet-counters` e `dotnet-trace` para runtime profiling.
- Modelo de GC/LOH configurável do .NET.
- Strong-name, ReadyToRun e limitações de publicação.

## 14. Resumo executivo final

O CLR Lens será uma ferramenta de **diagnóstico de performance e confiabilidade de assemblies .NET**, não apenas um scanner de opcodes nem um substituto do JIT. Seu valor está em conectar:

$$
DLL \rightarrow CIL \rightarrow IR \rightarrow CFG \rightarrow DataFlow \rightarrow CostModel \rightarrow Diagnosis \rightarrow Proof \rightarrow Rewrite \rightarrow ILVerify \rightarrow A/B
$$

A primeira prioridade é gerar um relatório tecnicamente defensável para uma DLL arbitrária, distinguindo prova estática, upper bound, estimativa, contrato externo, medição observada e desconhecido. A reescrita automática deve permanecer conservadora, opt-in e subordinada à verificação semântica.
