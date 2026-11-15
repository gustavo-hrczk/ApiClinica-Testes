# 📋ApiClinica

API REST para gerenciamento de uma clínica médica, desenvolvida para a disciplina de **Programação Server-Side** — Engenharia de Software, Centro Universitário Católica de Santa Catarina.

Projeto do Trabalho N2: evolução da ApiClinica para uso de **ORM (Entity Framework Core)** e **DTOs**.
Projeto do Trabalho N3: introdução da camada de Services (regras de negócio fora dos controllers) e **autenticação com JWT**, com controle de acesso por perfil.

## ✨Tecnologias

- C# / .NET 10
- Entity Framework Core (provider SQLite)
- Autenticação JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer)
- Testes de endpoints via Postman

## ✨Estrutura do projeto

```
ApiClinica/
├── Controllers/      # Auth, Pacientes, Medicos e Consultas
├── Services/         # Regras de negócio (um service por entidade) + Resultado
├── Interfaces/       # Contratos dos Services e dos Mappers
├── Models/           # Paciente, Medico, Consulta, Usuario
├── Data/             # AppDbContext
├── DTOs/             # DTOs de Create, Read e Update + Login e Register
├── Mappers/          # PacienteMapper, MedicoMapper, ConsultaMapper (manuais)
├── Validators/       # CpfValidator, ContatoValidator (regras reutilizáveis)
├── Migrations/       # Migrations do EF Core
├── Program.cs
└── appsettings.json
```

## ✨Entidades

**Paciente**

- Id, Nome, Email, Telefone, DataNasc, Cpf

**Médico**

- Id, Nome, Email, Telefone, CRM

**Consulta**

- Id, PacienteId, MedicoId, DataHora

**Usuário (autenticação)**

- Id, Login, Senha, Perfil (admin ou usuario)

## ✨Camadas

O fluxo de uma requisição atravessa quatro camadas, e nenhuma delas invade a outra:

- **Controller** recebe o HTTP, aplica a autorização, chama o Service e traduz o resultado em código de status. É o único lugar que decide o status da resposta.
- **Service** concentra as regras de negócio (validações, conflitos de horário, bloqueios de exclusão) e é o único que fala com o AppDbContext. Não conhece HTTP: devolve um Resultado (ou Resultado<T>) indicando sucesso, ou o tipo do erro — NaoEncontrado, Invalido ou Conflito.
- **Mapper** traduz entre DTO e entidade. Cada entidade tem `ToEntity`, `ToReadDTO` e `ApplyUpdate`.
- **DTO** define o contrato público — o que entra e o que sai pela rede.
- **Validator** concentra as regras reutilizáveis, usadas por mais de um Service.

A entidade nunca chega ao cliente e o DTO nunca chega ao banco — o Mapper é a única fronteira entre os dois. Services e Mappers são injetados por interface (`Interfaces/`) e registrados em `Program.cs`.

## ✨Autenticação e autorização

Todos os endpoints de Pacientes, Medicos e Consultas exigem um token JWT. Apenas /api/auth/register e /api/auth/login são públicos.

### Fluxo

1. **Cadastre um usuário** — `POST /api/auth/register`. Se `Perfil` for omitido, o usuário é criado como `usuario`. Login já existente retorna 400.
2. **Faça login** — `POST /api/auth/login`. Resposta 200: `{ "token": "eyJhbGciOi..." }`. Credenciais inválidas retornam 401.
3. **Envie o token** nas demais requisições, no header: `Authorization: Bearer <token>`. O token expira em 2 horas. Depois disso, é preciso fazer login novamente.

### Permissões por perfil

| Operação                       | Sem token | `usuario` | `admin` |
| ------------------------------ | :-------: | :-------: | :-----: |
| `GET` (listar e buscar por id) |    401    |    ✅     |   ✅    |
| `POST` (criar)                 |    401    |    ✅     |   ✅    |
| `PATCH` (atualizar)            |    401    |    403    |   ✅    |
| `DELETE` (excluir)             |    401    |    403    |   ✅    |

## ✨Funcionalidades

Cada controller (`Pacientes`, `Medicos`, `Consultas`) expõe os métodos `GET`, `GET by Id`, `POST`, `PATCH` e `DELETE`, recebendo e retornando apenas DTOs — nunca as entidades do banco diretamente.

### Validações compartilhadas

Regras usadas por mais de um controller ficam em `Validators/`, como classes estáticas:

- **`CpfValidator.EhValido(cpf)`** — confere os dois dígitos verificadores e rejeita CPFs com todos os dígitos iguais. Aceita o CPF com ou sem pontuação.
- **`ContatoValidator.ValidarEmail(email)`** e **`ContatoValidator.ValidarTelefone(telefone)`** — devolvem `null` quando o valor é válido, ou a mensagem de erro quando não é.

### Regras de validação

**Paciente**

- Email em formato válido.
- Telefone com DDD de 2 dígitos e número de 8 ou 9 dígitos, ex.: `(47) 98888-7777` (parênteses, espaço e hífen são opcionais).
- Data de nascimento não pode ser no futuro.
- CPF numericamente válido (não pode ser alterado via PATCH).
- Não é possível cadastrar um paciente com CPF já existente.

**Médico**

- Email em formato válido.
- Telefone no mesmo formato do paciente.

**Consulta**

- PacienteId e MedicoId devem existir.
- Não é possível agendar consulta no passado.
- Não pode haver sobreposição de horários para o mesmo médico nem para o mesmo paciente: cada consulta ocupa 30 minutos, então duas consultas precisam ter ao menos esse intervalo entre si. Horários que apenas encostam (10:00 e 10:30) são permitidos.
- Ao alterar PacienteId ou MedicoId via PATCH, as regras de conflito de horário são revalidadas com os valores finais.

**Regras gerais**

- No PATCH, campos recebidos como `null` são ignorados (não atualizados).
- Não é possível excluir (DELETE) um paciente ou médico que tenha consultas futuras agendadas.
- Consultas podem ser excluídas livremente.

### Códigos de status

| Código | Quando                                                                           |
| ------ | -------------------------------------------------------------------------------- |
| 200    | Consulta bem-sucedida; atualização de paciente (devolve o paciente atualizado)   |
| 201    | Recurso criado (com cabeçalho `Location`)                                        |
| 204    | Exclusão bem-sucedida; atualização de médico e de consulta (sem corpo)           |
| 400    | Corpo da requisição inválido ou regra de validação violada                       |
| 401    | Token ausente, inválido ou expirado; credenciais de login incorretas             |
| 403    | Usuário autenticado sem perfil `admin` tentando fazer PATCH ou DELETE            |
| 404    | Recurso não encontrado                                                           |
| 409    | Conflito com o estado atual (CPF duplicado, horário ocupado, exclusão bloqueada) |

## ✨Como executar

Pré-requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Passos

```bash
# Clonar o repositório
git clone https://github.com/beatrice-fischer/ApiClinica.git
cd ApiClinica

# Restaurar dependências
dotnet restore

# Executar a API
dotnet run
```

O banco SQLite é criado e as migrations são aplicadas automaticamente na inicialização — o `Program.cs` chama `Database.Migrate()`, então não é necessário rodar `dotnet ef database update` manualmente.

O arquivo `clinica.db` fica na raiz do projeto, versionado no repositório, para que toda a equipe trabalhe sobre a mesma base. O caminho é calculado a partir da pasta do `.csproj`, não do diretório de trabalho, de modo que a API encontra o mesmo banco seja executada pelo Visual Studio, por `dotnet run` ou pelo executável.

A API sobe em `http://localhost:{porta}` — confira a porta na linha `Now listening on:` exibida no console ao iniciar.

### Configuração do JWT

A assinatura dos tokens é configurada na seção `Jwt` do `appsettings.json`:

```json
"Jwt": {
  "Key": "<chave secreta>",
  "Issuer": "ApiClinica",
  "Audience": "ApiClinicaUsuarios"
}
```

A API depende de `Jwt:Key`: sem ela, o login e a validação dos tokens falham. A chave que acompanha o repositório serve apenas para desenvolvimento e estudo — veja [Limitações conhecidas](#limitações-conhecidas).

### Testando os endpoints

**Antes de qualquer teste, obtenha um token** (veja [Autenticação e autorização](#autenticação-e-autorização)). Em resumo, com a API rodando:

```bash
# 1. Criar um admin (só na primeira vez)
curl -X POST http://localhost:{porta}/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"Login":"admin","Senha":"admin123","Perfil":"admin"}'

# 2. Fazer login e copiar o token da resposta
curl -X POST http://localhost:{porta}/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"Login":"admin","Senha":"admin123"}'

# 3. Usar o token
curl http://localhost:{porta}/api/pacientes \
  -H "Authorization: Bearer <token>"
```

#### Postman

A collection do Postman está na raiz do projeto: **`ApiClinica-N2.postman_collection.json`**.

Importe em **Import → File** e execute pelo **Runner**, de cima para baixo — a ordem importa, porque os IDs criados nas primeiras requisições alimentam as seguintes. Ajuste a variável `baseUrl` para a porta exibida no console.

A collection cobre os cinco métodos nas três entidades, os casos de erro de cada validação, a janela de 30 minutos e a regressão das rotas no singular. Ela gera CPF válido e datas futuras a cada execução e remove ao final tudo o que criou.

> ⚠️ **Atenção:** a collection foi escrita na etapa N2, antes da autenticação, e **ainda não envia token**. Sem ajuste, todas as requisições retornam `401`. Para usá-la, faça login como `admin` e configure o token na aba **Authorization** da collection (tipo _Bearer Token_) — é preciso ser `admin`, porque a collection executa PATCH e DELETE.

#### Datas nas consultas

Envie `DataHora` **sem o `Z`** no final (ex.: `2026-12-10T10:00:00`). Com `Z` o .NET interpreta como UTC e a comparação com o horário atual erra em algumas horas.

#### Rotas disponíveis

| Método | Rota                  | Descrição                   | Acesso  |
| ------ | --------------------- | --------------------------- | ------- |
| POST   | `/api/auth/register`  | Cadastra um usuário         | Público |
| POST   | `/api/auth/login`     | Autentica e devolve o token | Público |
| GET    | `/api/pacientes`      | Lista todos os pacientes    | Token   |
| GET    | `/api/pacientes/{id}` | Busca paciente por Id       | Token   |
| POST   | `/api/pacientes`      | Cadastra um novo paciente   | Token   |
| PATCH  | `/api/pacientes/{id}` | Atualiza um paciente        | `admin` |
| DELETE | `/api/pacientes/{id}` | Remove um paciente          | `admin` |

(Rotas análogas para `/api/medicos` e `/api/consultas`, com as mesmas regras de acesso.)

## ✨Limitações conhecidas

O projeto tem fins didáticos e, por isso, alguns pontos **não** estão prontos para produção:

- **Senhas são armazenadas em texto puro** no banco, sem hash. Em um sistema real, use hash com salt (por exemplo, `PasswordHasher` do ASP.NET Core Identity ou BCrypt).
- **A chave JWT está versionada** no `appsettings.json`. Em produção, ela deve vir de variável de ambiente, _user secrets_ ou um cofre de segredos — e a chave atual deve ser trocada.
- **`/api/auth/register` é público e aceita qualquer `Perfil`**, então qualquer pessoa pode criar um usuário `admin`. Em produção, o cadastro de administradores deve ser restrito.
- **A comparação de CPF duplicado é textual**: `529.982.247-25` e `52998224725` são tratados como CPFs diferentes.
- **Conflito de horário e consultas passadas usam `DateTime.Now`** (horário local do servidor).

## ✨Equipe

- Beatrice Fischer
- Gabriele Maria Freiberger
- Gustavo Hreczuck
- Lucas de Carvalho Ziele
- Raul Schmitz

## ✨Disciplina

- **Curso:** Engenharia de Software
- **Disciplina:** Programação Server-Side
- **Professora:** Beatriz M. Reichert
- **Instituição:** Centro Universitário Católica de Santa Catarina
