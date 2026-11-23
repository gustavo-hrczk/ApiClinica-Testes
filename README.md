# ApiClinica — Testes unitários e testes de carga

Projeto prático de **testes de software** sobre a ApiClinica, uma API REST para gerenciamento de uma clínica médica (pacientes, médicos e consultas).

A API foi desenvolvida originalmente na disciplina de Programação Server-Side ([repositório original](https://github.com/beatrice-fischer/ApiClinica)). Este repositório parte dela e acrescenta:

- **172 testes unitários** (xUnit + Moq), com **100% de cobertura de linhas** do código da aplicação;
- **três testes de carga com Grafana k6**: smoke, average load e stress;
- a análise dos resultados e as instruções para reproduzir tudo.

## Sumário

1. [Tecnologias](#tecnologias)
2. [Estrutura do repositório](#estrutura-do-repositório)
3. [Configuração do ambiente](#configuração-do-ambiente)
4. [Executando a API](#executando-a-api)
5. [A API: endpoints e regras](#a-api-endpoints-e-regras)
6. [Testes unitários e cobertura](#testes-unitários-e-cobertura)
7. [Testes de carga com k6](#testes-de-carga-com-k6)
8. [Resultados e análise](#resultados-e-análise)
9. [O que foi adaptado na API](#o-que-foi-adaptado-na-api)
10. [Limitações conhecidas](#limitações-conhecidas)

## Tecnologias

| Finalidade         | Ferramenta                                             |
| ------------------ | ------------------------------------------------------ |
| API                | C# / .NET 10, ASP.NET Core, Entity Framework Core      |
| Banco de dados     | SQLite                                                 |
| Autenticação       | JWT Bearer, com perfis `admin` e `usuario`             |
| Testes unitários   | xUnit, Moq, SQLite em memória                          |
| Cobertura          | Coverlet (coleta) + ReportGenerator (relatório HTML)   |
| Testes de carga    | Grafana k6                                             |

## Estrutura do repositório

```
├── src/ApiClinica/            # a API
│   ├── Controllers/           #   Auth, Pacientes, Medicos e Consultas
│   ├── Services/              #   regras de negócio + Resultado
│   ├── Interfaces/            #   contratos dos services e mappers
│   ├── Mappers/               #   conversão entre DTO e entidade
│   ├── Validators/            #   CpfValidator, ContatoValidator
│   ├── DTOs/  Models/  Data/  Migrations/
│   └── Program.cs
├── tests/ApiClinica.Tests/    # testes unitários
│   ├── Controllers/  Services/  Mappers/  Validators/
│   ├── Helpers/               #   banco em memória e utilitários
│   └── coverlet.runsettings   #   o que entra e o que sai da medição de cobertura
├── k6/                        # testes de carga
│   ├── smoke.js  average-load.js  stress.js
│   ├── lib/                   #   setup, geração de dados e cenários compartilhados
│   └── results/               #   resultados das execuções completas
├── scripts/                   # atalhos em PowerShell
├── docs/                      # collection do Postman
└── ApiClinica.slnx
```

## Configuração do ambiente

Pré-requisitos:

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Grafana k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) — no Windows: `winget install GrafanaLabs.k6` (os resultados deste repositório foram obtidos com a versão 2.4.0)

```bash
git clone <url-deste-repositorio>
cd <pasta-do-repositorio>

dotnet restore          # dependências da API e dos testes
dotnet tool restore     # ReportGenerator (relatório de cobertura)
```

## Executando a API

```bash
dotnet run --project src/ApiClinica
```

A API sobe em `http://localhost:5052`. O banco SQLite (`src/ApiClinica/clinica.db`) é criado e as migrations são aplicadas automaticamente na inicialização.

### Demonstração do CRUD

Todos os endpoints de pacientes, médicos e consultas exigem token. PATCH e DELETE exigem perfil `admin`.

```bash
# 1. Criar um admin (só na primeira vez) e fazer login
curl -X POST http://localhost:5052/api/auth/register -H "Content-Type: application/json" \
  -d '{"Login":"admin","Senha":"admin123","Perfil":"admin"}'

curl -X POST http://localhost:5052/api/auth/login -H "Content-Type: application/json" \
  -d '{"Login":"admin","Senha":"admin123"}'
# -> { "token": "eyJhbGciOi..." }    Use nas próximas chamadas: -H "Authorization: Bearer <token>"

# 2. Create  -> 201 + header Location
curl -X POST http://localhost:5052/api/pacientes -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"Nome":"Ana Souza","Email":"ana.souza@example.com","Telefone":"(47) 98888-8888","DataNasc":"1995-03-24","Cpf":"529.982.247-25"}'

# 3. Read    -> 200
curl http://localhost:5052/api/pacientes     -H "Authorization: Bearer <token>"
curl http://localhost:5052/api/pacientes/1   -H "Authorization: Bearer <token>"

# 4. Update  -> 200
curl -X PATCH http://localhost:5052/api/pacientes/1 -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" -d '{"Nome":"Ana Souza Lima"}'

# 5. Delete  -> 204
curl -X DELETE http://localhost:5052/api/pacientes/1 -H "Authorization: Bearer <token>"
```

Também há uma collection do Postman em `docs/ApiClinica-N2.postman_collection.json`. Ela foi escrita antes da autenticação: configure um token de `admin` na aba **Authorization** da collection (tipo _Bearer Token_) antes de executar.

## A API: endpoints e regras

| Método | Rota                        | Descrição              | Acesso  |
| ------ | --------------------------- | ---------------------- | ------- |
| POST   | `/api/auth/register`        | Cadastra um usuário    | Público |
| POST   | `/api/auth/login`           | Devolve o token JWT    | Público |
| GET    | `/api/{recurso}`            | Lista todos            | Token   |
| GET    | `/api/{recurso}/{id}`       | Busca por id           | Token   |
| POST   | `/api/{recurso}`            | Cria                   | Token   |
| PATCH  | `/api/{recurso}/{id}`       | Atualiza parcialmente  | `admin` |
| DELETE | `/api/{recurso}/{id}`       | Remove                 | `admin` |

`{recurso}` é `pacientes`, `medicos` ou `consultas` — três CRUDs completos.

| Código | Quando                                                                           |
| ------ | -------------------------------------------------------------------------------- |
| 200    | Consulta bem-sucedida; atualização de paciente (devolve o paciente atualizado)   |
| 201    | Recurso criado (com cabeçalho `Location`)                                        |
| 204    | Exclusão bem-sucedida; atualização de médico e de consulta                       |
| 400    | Corpo inválido ou regra de validação violada                                     |
| 401    | Token ausente, inválido ou expirado; credenciais de login incorretas             |
| 403    | Usuário sem perfil `admin` tentando PATCH ou DELETE                              |
| 404    | Recurso não encontrado                                                           |
| 409    | Conflito com o estado atual (CPF duplicado, horário ocupado, exclusão bloqueada) |

Regras de negócio (todas cobertas por testes unitários):

- **Paciente:** e-mail válido; telefone com DDD de 2 dígitos e número de 8 ou 9 dígitos; data de nascimento não pode ser futura; CPF com dígitos verificadores válidos e único; o CPF não é alterável nem devolvido nas respostas.
- **Médico:** e-mail e telefone nos mesmos formatos.
- **Consulta:** paciente e médico precisam existir; não pode ser no passado; o mesmo médico ou o mesmo paciente não pode ter duas consultas a menos de 30 minutos uma da outra (horários que apenas encostam, como 10:00 e 10:30, são permitidos); no PATCH, as regras são revalidadas com os valores finais.
- **Geral:** no PATCH, campos não enviados não são alterados; não é possível excluir paciente ou médico com consulta futura.

Camadas: o **Controller** traduz HTTP e decide o código de status; o **Service** concentra as regras e devolve um `Resultado` (sucesso, `NaoEncontrado`, `Invalido` ou `Conflito`); o **Mapper** converte entre DTO e entidade; os **Validators** guardam regras reutilizadas.

## Testes unitários e cobertura

### Como executar

```bash
# Só os testes
dotnet test

# Testes + relatório de cobertura (PowerShell)
.\scripts\cobertura.ps1
```

O script equivale a:

```bash
dotnet test --settings tests/ApiClinica.Tests/coverlet.runsettings \
  --collect:"XPlat Code Coverage" --results-directory TestResults

dotnet reportgenerator "-reports:TestResults/*/coverage.cobertura.xml" \
  "-targetdir:coverage-report" "-reporttypes:Html;TextSummary"
```

O relatório fica em `coverage-report/index.html` (navegável, linha a linha) e o resumo em `coverage-report/Summary.txt`.

### Resultado

| Métrica               | Valor                  |
| --------------------- | ---------------------- |
| Testes                | 172 aprovados, 0 falhas |
| Cobertura de linhas   | **100%** (523 de 523)  |
| Cobertura de branches | 100% (168 de 168)      |
| Cobertura de métodos  | 100% (138 de 138)      |

A exigência do trabalho é de 90% das linhas.

### O que é testado, e como

| Camada      | Estratégia                                                                 | Exemplos de cenários                                                                                         |
| ----------- | -------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------ |
| Validators  | Funções puras, testadas com tabelas de entradas (`[Theory]`)               | CPF válido com e sem pontuação; dígito verificador errado; todos os dígitos iguais; telefones e e-mails inválidos |
| Mappers     | Objetos em memória                                                         | Cópia de todos os campos; o CPF não aparece no DTO de leitura; PATCH com campo nulo não sobrescreve          |
| Services    | Service e mapper reais sobre um **SQLite em memória**, novo a cada teste   | CPF duplicado (conflito); data de nascimento futura; janela de 30 minutos nos limites (29 min conflita, 30 min não); reagendar para o próprio horário; exclusão bloqueada por consulta futura |
| Controllers | Service substituído por **mock (Moq)**; verifica-se só a tradução para HTTP | 200, 201 com `Location`, 204, 400 e 409 com mensagem, 404, 500 para falha inesperada                         |
| Auth        | Controller real sobre SQLite em memória e configuração em memória          | Registro; perfil padrão `usuario`; login duplicado; token com issuer, audience, claims e validade de 2 h; senha errada (401) |

Os três tipos de cenário pedidos estão presentes em todas as camadas: **sucesso**, **falha** (não encontrado, conflito, não autorizado) e **validação de dados**.

Nos services optamos por SQLite em memória em vez de simular o `DbContext`: as regras de conflito de horário são consultas LINQ traduzidas para SQL, e um mock não verificaria se a consulta está correta. Cada teste abre o próprio banco, então são independentes e nenhum toca o `clinica.db`.

### O que fica fora da medição

Definido em `tests/ApiClinica.Tests/coverlet.runsettings`:

- **`Migrations/`** — código gerado pelo `dotnet ef migrations add`, não escrito pela equipe.
- **`Program.cs`** — apenas composição da aplicação (injeção de dependência, pipeline HTTP, aplicação das migrations). Não tem regra de negócio e só roda com o servidor de pé; é exercitado de ponta a ponta pelos testes de carga.
- **Código emitido por geradores do compilador** (`GeneratedCodeAttribute`), que não existe no repositório.

Controllers, services, mappers, validators, DTOs, models e o `AppDbContext` estão todos dentro da medição.

## Testes de carga com k6

### Como executar

Terminal 1 — sobe a API em modo Release com um **banco descartável** (`src/ApiClinica/carga.db`), para a carga não poluir o banco de desenvolvimento:

```powershell
.\scripts\api-carga.ps1
```

Terminal 2 — executa o teste desejado:

```powershell
.\scripts\carga.ps1 smoke          # ~40 s
.\scripts\carga.ps1 average-load   # ~5 min
.\scripts\carga.ps1 stress         # ~6 min
```

Durante a execução, o dashboard do k6 fica disponível em `http://localhost:5665`. Ao final são gravados em `k6/results/` o resumo em JSON e o dashboard em HTML.

Sem os scripts (qualquer sistema operacional):

```bash
# Terminal 1
cd src/ApiClinica
ASPNETCORE_URLS=http://localhost:5052 ConnectionStrings__Default="Data Source=carga.db" \
  dotnet run -c Release --no-launch-profile

# Terminal 2
k6 run k6/smoke.js
k6 run k6/average-load.js
k6 run k6/stress.js
```

Se a API estiver em outro endereço: `k6 run -e BASE_URL=http://host:porta k6/smoke.js`.

> Se um teste for interrompido no meio, sobram dados no banco de carga e o próximo `setup` pode falhar com 409. Basta subir a API de novo com `scripts\api-carga.ps1`, que recria o banco.

### Cenário avaliado

Os três testes usam a mesma jornada, para os resultados serem comparáveis. Ela simula uma **recepcionista da clínica** e cobre o CRUD completo de dois recursos — 11 requisições por iteração (5 leituras e 6 escritas), com pausas de 0,5 a 1,5 s entre as etapas para simular o tempo de uso da tela:

| Etapa                     | Requisições                                                                        |
| ------------------------- | ---------------------------------------------------------------------------------- |
| 1. Consultar listagens    | `GET /api/pacientes`, `GET /api/medicos`, `GET /api/consultas`                     |
| 2. Cadastrar paciente     | `POST /api/pacientes`, `GET /api/pacientes/{id}`, `PATCH /api/pacientes/{id}`      |
| 3. Agendar consulta       | `POST /api/consultas`, `GET /api/consultas/{id}`, `PATCH /api/consultas/{id}`      |
| 4. Cancelar e remover     | `DELETE /api/consultas/{id}`, `DELETE /api/pacientes/{id}`                         |

Por que esses endpoints: são o fluxo principal do sistema e misturam os dois perfis de custo da API — leituras (incluindo as listagens, que não têm paginação) e escritas com validação (CPF único, conflito de horário), que fazem várias consultas ao banco antes de gravar.

Decisões dos scripts:

- **Autenticação:** o `setup()` registra um usuário `admin`, faz login uma vez e repassa o token aos usuários virtuais. O login não entra na carga porque, no uso real, acontece uma vez por turno.
- **Dados sem colisão:** cada usuário virtual gera CPFs válidos e únicos e usa um médico próprio com um horário por iteração. Assim nenhum 409 é esperado, e qualquer erro que apareça é um problema real.
- **Massa inicial:** o `setup()` cria 200 pacientes (20 no smoke) para as listagens terem volume. O `teardown()` remove tudo.
- **Verificações (`check`):** cada resposta tem o status conferido e, quando há corpo, também o conteúdo (por exemplo: o id devolvido confere, o telefone foi alterado, o CPF não é exposto).
- **Tags:** `tipo` (leitura ou escrita) permite limites e análise separados; `name` agrupa as URLs que têm id.

### Os três testes

|                       | Smoke                                   | Average load                                  | Stress                                                 |
| --------------------- | --------------------------------------- | --------------------------------------------- | ------------------------------------------------------ |
| **Objetivo**          | Validar os scripts e o funcionamento básico | Desempenho em uso normal                  | Estabilidade e degradação acima do normal              |
| **Usuários virtuais** | 1                                       | 20                                            | 100 (5x a média), com degrau em 50                     |
| **Duração**           | 30 s                                    | 5 min: 1 min subindo, 3 min constantes, 1 min descendo | 6 min: 1 min até 50, 1 min em 50, 1 min até 100, 2 min em 100, 1 min descendo |
| **Cenário**           | Jornada + respostas de erro (401, 404, 400, 409) | Jornada                              | Jornada                                                |
| **Falhas HTTP**       | 0%                                      | menos de 1%                                   | menos de 5%                                            |
| **Verificações**      | 100%                                    | mais de 99%                                   | mais de 95%                                            |
| **p95 geral**         | menos de 300 ms                         | menos de 500 ms                               | menos de 2 s                                           |
| **p95 por tipo**      | —                                       | leitura < 400 ms, escrita < 600 ms            | leitura < 2 s, escrita < 2 s                           |

Justificativas:

- **Smoke — 1 usuário, 30 s.** Não mede capacidade: serve para descobrir erro de script ou de ambiente antes de gastar minutos com os testes longos. Por isso o critério é rígido (nenhuma falha) e é o único que também verifica as respostas de erro. Com um usuário não há fila, então o p95 deve ficar bem abaixo do limite usado sob carga.
- **Average load — 20 usuários.** Premissa: uma rede de clínicas com cerca de 20 recepcionistas usando o sistema ao mesmo tempo no horário de pico. A subida gradual evita medir o efeito de todos chegarem no mesmo instante, e 3 minutos constantes são suficientes para o p95 estabilizar (mais de 10 mil requisições). O limite de 500 ms no p95 é o ponto até onde o uso ainda parece imediato na tela; a escrita tem limite maior porque valida e grava.
- **Stress — até 100 usuários.** Cinco vezes a média, valor escolhido para ultrapassar com folga um pico plausível. O degrau em 50 mostra onde a degradação começa, e a descida no fim mostra se a API se recupera. Os limites são mais frouxos porque, sob estresse, aceita-se degradação — o que não se aceita é colapso (erros em massa ou respostas acima de 2 s para a maioria).

## Resultados e análise

Execuções completas de 23/11/2026, com a API e o k6 na mesma máquina (computador de desenvolvimento, Windows 11), API em Release e banco SQLite em disco. Os arquivos estão em `k6/results/`: a saída do terminal (`.txt`), o resumo (`-summary.json`) e o dashboard com os gráficos ao longo do tempo (`-dashboard.html`).

### Comparativo

| Métrica                         | Smoke      | Average load | Stress        |
| ------------------------------- | ---------- | ------------ | ------------- |
| Usuários virtuais (máx.)        | 1          | 20           | 100           |
| Requisições                     | 180        | 16.348       | 43.678        |
| Requisições por segundo (média) | 5,4        | 53,6         | 117,6         |
| Iterações completas             | 8          | 1.446        | 3.916         |
| Duração média da iteração       | 3,95 s     | 3,36 s       | 6,24 s        |
| **Taxa de falhas HTTP**         | 0%         | 0%           | 0%            |
| Verificações corretas           | 100% (193) | 100% (26.029) | 100% (70.489) |
| Tempo médio                     | 15 ms      | 31 ms        | 290 ms        |
| Mediana                         | 7 ms       | 6 ms         | 37 ms         |
| p90                             | 28 ms      | 63 ms        | 517 ms        |
| **p95 geral**                   | **47 ms**  | **170 ms**   | **1,08 s**    |
| p95 leitura                     | —          | 27 ms        | 347 ms        |
| p95 escrita                     | —          | 238 ms       | **2,34 s**    |
| Tempo máximo                    | 309 ms     | 2,75 s       | 23,2 s        |
| **Critérios de aceitação**      | ✅ todos   | ✅ todos     | ❌ p95 de escrita (2,34 s, limite 2 s) |

### Leitura dos resultados

**Smoke.** Passou em todos os critérios. Os scripts funcionam, o CRUD responde com os códigos corretos e os cenários de erro devolvem 401, 404, 400 e 409 como esperado. Com um usuário, a API responde em poucos milissegundos (mediana de 7 ms).

**Average load.** Passou com folga: p95 de 170 ms para um limite de 500 ms, sem nenhuma falha em mais de 16 mil requisições. A mediana (6 ms) praticamente não mudou em relação ao smoke, ou seja, a requisição típica não sente a carga. Mas a cauda já aponta onde está o problema: o p95 das **escritas (238 ms) é quase nove vezes o das leituras (27 ms)**, e houve picos isolados de até 2,75 s.

**Stress.** A API **não caiu e não devolveu nenhum erro**, mas degradou bastante e **reprovou em um critério**: o p95 das escritas chegou a 2,34 s, acima do limite de 2 s. Os demais critérios passaram (p95 geral de 1,08 s, leituras em 347 ms, 0% de falhas).

Comparando com a carga média:

- **A vazão não acompanhou a carga.** Considerando as rampas, o número médio de usuários ativos foi cerca de 4 vezes maior que no average load, mas as requisições por segundo só aumentaram 2,2 vezes (de 53,6 para 117,6). O restante virou espera.
- **A latência cresceu muito mais que a carga.** O p95 geral subiu 6 vezes (170 ms para 1,08 s) e o p95 de escrita, 10 vezes (238 ms para 2,34 s). A duração média da iteração quase dobrou (3,36 s para 6,24 s) e a pior requisição levou 23 s.
- **A degradação foi em latência, não em erros.** As requisições entraram em fila e foram todas atendidas corretamente — as 70.489 verificações passaram.

### Gargalos

1. **Escrita no banco é o gargalo principal.** A diferença entre leitura e escrita aparece já na carga média e explode no estresse. A causa mais provável é o SQLite: ele permite **um único escritor por vez** e cada gravação é confirmada em disco, então as escritas simultâneas formam fila. Reforça essa hipótese o fato de a API devolver zero erros — as requisições esperam a vez em lugar de falhar. Não fizemos _profiling_ para confirmar a causa; a conclusão vem do contraste entre leituras e escritas.
2. **Cada escrita faz várias idas ao banco.** Criar uma consulta executa quatro consultas antes de gravar (paciente existe, médico existe, conflito do médico, conflito do paciente); criar um paciente verifica o CPF duplicado. Como não há índice em `Cpf`, nem em `MedicoId`/`PacienteId` + `DataHora`, essas verificações varrem a tabela e ficam mais caras conforme o banco cresce.
3. **Listagens sem paginação.** `GET /api/pacientes`, `/medicos` e `/consultas` devolvem a tabela inteira a cada chamada. No teste isso gerou 199 MB de tráfego em 6 minutos, com apenas 200 a 300 pacientes cadastrados. O custo cresce linearmente com o volume de dados, então em um banco real o p95 de leitura pioraria com o tempo.
4. **Verificar e depois gravar não é atômico.** As regras de CPF único e de conflito de horário consultam o banco e só depois inserem, sem restrição de unicidade no banco nem transação. Os scripts evitam colisões de propósito, então isso não apareceu nos resultados, mas sob concorrência real duas requisições simultâneas poderiam passar pela verificação e gravar dados duplicados.

### Oportunidades de melhoria

- **Trocar o SQLite por um banco cliente-servidor** (PostgreSQL ou SQL Server), que aceita escritas concorrentes. É a mudança com maior efeito esperado no resultado do stress.
- **Criar índices**: único em `Pacientes.Cpf` e compostos em `Consultas (MedicoId, DataHora)` e `Consultas (PacienteId, DataHora)`. O índice único também resolve a duplicidade sob concorrência (gargalo 4).
- **Paginar as listagens** (`?pagina=1&tamanho=50`) e usar `AsNoTracking()` nas consultas somente leitura.
- **Reduzir idas ao banco** na criação de consulta, unindo as duas verificações de conflito em uma só.
- **Repetir o stress após cada mudança**, para confirmar o ganho, e acrescentar um teste que continue subindo a carga até a API começar a falhar, para conhecer o limite real.

### Ressalvas

- O k6 e a API rodaram na mesma máquina e disputaram CPU e disco. Os números absolutos valem para comparar os três testes entre si, não como capacidade de um servidor de produção.
- Cada teste foi executado uma vez. Os valores variam entre execuções, principalmente os máximos.

## O que foi adaptado na API

O comportamento da API não mudou. As alterações foram só para viabilizar os testes:

- **Estrutura de pastas:** o projeto foi movido para `src/ApiClinica/`, com os testes em `tests/` e a solução na raiz.
- **Banco configurável:** a connection string pode vir de `ConnectionStrings:Default` (appsettings ou variável de ambiente `ConnectionStrings__Default`). Sem ela, vale o comportamento original (`clinica.db` na pasta do projeto). Isso permite rodar a carga em um banco descartável.
- **Banco fora do versionamento:** o `clinica.db` deixou de ser versionado; ele é recriado pelas migrations na inicialização.
- **Log do EF Core em `Warning`** no `appsettings.json`, para o registro de cada comando SQL no console não distorcer as medições de carga.
- **`launchSettings.json`:** removida uma vírgula sobrando que deixava o JSON inválido.

## Limitações conhecidas

Herdadas do projeto original, de fins didáticos:

- Senhas armazenadas em texto puro, sem hash.
- Chave JWT versionada no `appsettings.json`.
- `/api/auth/register` é público e aceita qualquer `Perfil`, inclusive `admin`.
- A comparação de CPF duplicado é textual: `529.982.247-25` e `52998224725` são tratados como diferentes.
- Conflitos de horário usam `DateTime.Now` (horário local do servidor). Envie `DataHora` sem o `Z` no final.

## Equipe

- Beatrice Fischer
- Gabriele Maria Freiberger
- Gustavo Hreczuck
- Lucas de Carvalho Ziele
- Raul Schmitz

Engenharia de Software — Centro Universitário Católica de Santa Catarina.
