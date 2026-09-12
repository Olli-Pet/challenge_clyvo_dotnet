# Olli API — Clyvo VET

API REST em ASP.NET Core para acompanhamento preventivo da saúde de pets. A Olli conecta tutor, pet e clínica veterinária por meio de cadastro, histórico clínico, alertas preventivos e uma recomendação simulada de IA.

**Repositório:** <https://github.com/Olli-Pet/challenge_clyvo_dotnet.git> <br>
**Branch:** Importar-projeto (Pull request com comparação entre projeto da Sprint 2 com Sprint 3)

## Integrantes

- Mirelly Sousa Alves — RM566299
- Gabriely Bonfim — RM566242
- Henrique Rodrigues — RM562917
- Andre Rosa Colombo — RM563112
- Ruan Luca Feliciano — RM562218

## Entrega desta sprint

Esta versão contempla monitoramento, observabilidade e testes automatizados:

- health checks da API, do banco e de serviços externos;
- logging estruturado com Serilog, em console e arquivo;
- correlation ID por requisição;
- tracing entre a camada HTTP e os serviços de aplicação com `ActivitySource`;
- métricas de quantidade, erros e duração em formato Prometheus;
- testes unitários com xUnit e Moq;
- testes de integração HTTP com `WebApplicationFactory<Program>`;
- documentação de execução local, Oracle, observabilidade e testes.

#
` O fluxo completo de testes e apresentação está no final deste README. `
#

## Tecnologias e estrutura

O projeto usa C#/.NET 10, ASP.NET Core Controllers, Entity Framework Core, Oracle EF Core, EF Core InMemory, Scalar, Health Checks, Serilog, OpenTelemetry, rate limiting, idempotência, xUnit e Moq.

```text
Olli.Api/Application          DTOs, mapeamentos e serviços
Olli.Api/Controllers          camada HTTP
Olli.Api/Domain               entidades e enums
Olli.Api/Filters              filtros HTTP
Olli.Api/Infrastructure       observabilidade e persistência
Olli.Api.Tests/Unit           testes unitários
Olli.Api.Tests/Integration    testes de integração HTTP
README.md                     documentação, execução e fluxo de apresentação
```

## Domínio e endpoints

Entidades principais: `Tutor`, `Pet`, `EventoSaude` e `AlertaPreventivo`. A recomendação em `RecomendacaoIaDTO` é orientativa e simulada; não substitui avaliação veterinária.

| Recurso | Endpoints |
|---|---|
| Tutores | `GET /tutores`, `GET /tutores/{id}`, `POST /tutores`, `PUT /tutores/{id}`, `DELETE /tutores/{id}` |
| Pets | `GET /pets`, `GET /pets/{id}`, `POST /pets`, `PUT /pets/{id}`, `DELETE /pets/{id}` |
| Histórico | `GET/POST /pets/{id}/historico`, `GET/PUT/DELETE /pets/{id}/historico/{idEvento}` |
| Alertas | `GET/POST /pets/{id}/alertas`, `GET/PUT/DELETE /pets/{id}/alertas/{idAlerta}` |
| IA simulada | `GET /pets/{id}/recomendacao-ia` |

Todas as rotas `POST` exigem o header `IdempotencyKey`. Status principais: `200 OK` (consulta), `201 Created` (criação), `204 No Content` (atualização/exclusão), `400 Bad Request` (dados inválidos ou idempotência ausente), `404 Not Found` e `429 Too Many Requests`.

## Pré-requisitos

- SDK do .NET 10 instalado e disponível no `PATH` (`dotnet --version`);
- acesso ao Oracle FIAP somente para executar a aplicação com o provider Oracle.

Na raiz da solução:

```powershell
dotnet restore
```

## Executar localmente com InMemory

O perfil `Development` usa `appsettings.Development.json`, banco em memória e seed automático. É o modo recomendado para desenvolvimento e demonstração.

```powershell
dotnet run --project Olli.Api
```

O seed cria o tutor `Mirelly Santos` e o pet `Olli`. Como o banco é InMemory, os dados são recriados quando a aplicação reinicia.

Ao usar o provider Oracle, ele é selecionado por `DatabaseProvider` e a conexão é lida de `ConnectionStrings:FiapOracle`. O `appsettings.json` contém somente credenciais de exemplo.
No modo Oracle, `SeedDatabase` permanece desabilitado e `ApplyMigrationsOnStartup=false`, portanto a API não altera o schema automaticamente. O health check `oracle-fiap` executa `SELECT 1 FROM DUAL` com timeout de cinco segundos. A rota `/health/database` é a verificação específica da conectividade Oracle.

## Health checks

| Rota | Escopo |
|---|---|
| `GET /health` | API e todas as dependências configuradas |
| `GET /health/live` | disponibilidade do processo da API |
| `GET /health/ready` | API, banco e serviços necessários para atender tráfego |
| `GET /health/database` | banco ativo: Oracle ou InMemory |
| `GET /health/external` | URLs de `ExternalServices:Urls` |
| `GET /health-ui` | painel visual do Health Checks UI |

Em desenvolvimento, `in-memory-db` confirma que o banco local está ativo. Em produção, `oracle-fiap` valida a conexão Oracle. O payload detalhado identifica cada check e sua descrição; checks não saudáveis podem produzir status HTTP não sucedido.

## Observabilidade

### Logging estruturado

O Serilog grava eventos no console e em `Olli.Api/logs/olli-AAAA-MM-DD.log`. O arquivo é ignorado pelo Git e fica retido por até 14 dias. O log de requisição contém método, rota, status, duração, `CorrelationId` e `TraceId`; exceções não tratadas são registradas com stack trace.

### Correlation ID

O middleware lê `X-Correlation-ID` quando o valor contém caracteres seguros e tem até 128 caracteres. Quando o cliente não envia um valor válido, a API gera um identificador novo. O mesmo valor é devolvido no response header e incluído no contexto dos logs e dos spans.

```http
GET http://localhost:5127/pets
X-Correlation-ID: atendimento-2026-001
```

### Tracing

O `ActivitySource` `Olli.Api` cria um span para a requisição HTTP e spans internos para operações dos serviços, como `PetService.Listar` e `TutorService.Criar`. O OpenTelemetry exporta esses spans no console, permitindo acompanhar o fluxo entre a camada HTTP e a aplicação; as operações de persistência ficam dentro do span do serviço responsável.

### Métricas

O meter `Olli.Api` registra `olli_api_requests_total` (total), `olli_api_errors_total` (respostas `4xx` ou `5xx`) e `olli_api_request_duration_ms` (duração em milissegundos, com soma e contagem). Consulte `GET /metrics` para a exposição em formato Prometheus. Os contadores são mantidos em memória; para histórico permanente, o endpoint deve ser coletado por Prometheus ou outro backend.

## Testes automatizados

A suíte segue o padrão AAA (Arrange, Act, Assert):

- `Olli.Api.Tests/Unit`: valida mapeamentos, controllers e regras do `PetService`, usando banco InMemory isolado e Moq para dependências;
- `Olli.Api.Tests/Integration/OlliApiTests.cs`: valida o fluxo HTTP real com `WebApplicationFactory<Program>`, incluindo health checks, correlation ID, métricas, CRUD, idempotência, respostas `404` e rate limit.
- `Olli.Api.Tests/Integration/OracleSmokeTests.cs`: executa o health check Oracle real somente quando `ORACLE_TEST_CONNECTION_STRING` estiver configurada.

Os testes de integração usam o ambiente `Testing`, forçam InMemory, removem dependências externas e usam um banco único por cenário. Não precisam de Oracle, internet ou dados prévios, e não dependem de `Task.Delay` para sincronização.

## Fluxo de uso recomendado

Este fluxo pode ser usado tanto para validar a entrega quanto para apresentar as funcionalidades da API. A ordem começa pelo funcionamento local, passa pelo CRUD e pela observabilidade e termina com a validação do Oracle.

### 1. Preparação

Na pasta da solução, validar o SDK e restaurar os pacotes:

```powershell
dotnet --version
dotnet restore
dotnet test
```

Para a demonstração local, não é necessário Oracle. O ambiente Development utiliza o banco InMemory e o seed automático.

### 2. Visão geral da arquitetura

```text
Cliente HTTP
    ↓
Middleware de observabilidade
    ↓
Rate limiter
    ↓
Controller
    ↓
Service
    ↓
OlliDb / Entity Framework Core
    ↓
InMemory ou Oracle
```

O relacionamento principal do domínio é:

```text
Tutor
  └── Pet
        ├── Histórico de saúde
        ├── Alertas preventivos
        └── Recomendação simulada de IA
```

### 3. Executar em Development

Em um terminal:

```powershell
dotnet run --project Olli.Api
```

URLs utilizadas durante a demonstração:

| URL | Finalidade |
|---|---|
| `http://localhost:5127/scalar/v1` | documentação interativa |
| `http://localhost:5127/openapi/v1.json` | contrato OpenAPI |
| `http://localhost:5127/health` | health completo |
| `http://localhost:5127/health/live` | disponibilidade da API |
| `http://localhost:5127/health/ready` | prontidão para atender tráfego |
| `http://localhost:5127/health/database` | status do banco ativo |
| `http://localhost:5127/health/external` | status dos serviços externos |
| `http://localhost:5127/health-ui` | painel visual |
| `http://localhost:5127/metrics` | métricas Prometheus |

O seed inicial cria o tutor `Mirelly Santos` e o pet `Olli`.

### 4. Validar os health checks

Em um segundo terminal:

```powershell
$baseUrl = "http://localhost:5127"
Invoke-RestMethod "$baseUrl/health" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/health/live" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/health/ready" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/health/database" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/health/external" | ConvertTo-Json -Depth 6
```

No modo local, a resposta de `/health/database` deve conter `in-memory-db` com status `Healthy`. No payload de `/health`, o campo `entries` apresenta o status individual de cada verificação.

### 5. Executar o fluxo principal do CRUD

#### Criar tutor

```powershell
$tutorBody = @{
    nome = "Ana Souza"
    email = "ana.souza@example.com"
    telefone = "+55 11 98888-0000"
} | ConvertTo-Json

$tutor = Invoke-RestMethod `
    -Uri "$baseUrl/tutores" `
    -Method Post `
    -Headers @{ IdempotencyKey = "apresentacao-tutor-001" } `
    -ContentType "application/json" `
    -Body $tutorBody

$tutorId = $tutor.id
$tutor | ConvertTo-Json
```

Resultado esperado: `201 Created` e um identificador para o tutor.

Sem o header `IdempotencyKey`, a mesma operação deve retornar `400 Bad Request`:

```powershell
Invoke-WebRequest `
    -Uri "$baseUrl/tutores" `
    -Method Post `
    -ContentType "application/json" `
    -Body (@{ nome = "Sem chave" } | ConvertTo-Json)
```

#### Consultar e atualizar tutor

```powershell
Invoke-RestMethod "$baseUrl/tutores" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/tutores/$tutorId" | ConvertTo-Json -Depth 6

$updatedTutorBody = @{
    nome = "Ana Souza Atualizada"
    email = "ana.atualizada@example.com"
    telefone = "+55 11 97777-0000"
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "$baseUrl/tutores/$tutorId" `
    -Method Put `
    -ContentType "application/json" `
    -Body $updatedTutorBody
```

A atualização deve retornar `204 No Content`.

#### Criar e consultar pet

```powershell
$petBody = @{
    idTutor = $tutorId
    nome = "Nina"
    especie = "Gato"
    raca = "SRD"
    dataNascimento = "2021-03-12T00:00:00"
    pesoKg = 4.2
    scoreSaude = 88
    observacoes = "Pet ativa e acompanhada preventivamente."
} | ConvertTo-Json

$pet = Invoke-RestMethod `
    -Uri "$baseUrl/pets" `
    -Method Post `
    -Headers @{ IdempotencyKey = "apresentacao-pet-001" } `
    -ContentType "application/json" `
    -Body $petBody

$petId = $pet.id
Invoke-RestMethod "$baseUrl/pets" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/pets/$petId" | ConvertTo-Json -Depth 6
```

O campo `idTutor` cria o vínculo entre o pet e o tutor. O `scoreSaude` é normalizado para permanecer entre 0 e 100.

#### Atualizar e excluir pet

```powershell
$updatedPetBody = @{
    idTutor = $tutorId
    nome = "Nina Atualizada"
    especie = "Gato"
    raca = "SRD"
    dataNascimento = "2021-03-12T00:00:00"
    pesoKg = 4.5
    scoreSaude = 95
    observacoes = "Acompanhamento em dia."
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "$baseUrl/pets/$petId" `
    -Method Put `
    -ContentType "application/json" `
    -Body $updatedPetBody

Invoke-WebRequest "$baseUrl/pets/$petId" -Method Delete
```

As duas operações devem retornar `204 No Content`. Para continuar a demonstração do histórico e dos alertas, execute essas etapas antes de excluir o pet.

### 6. Demonstrar histórico de saúde

```powershell
$eventBody = @{
    tipo = "Consulta"
    titulo = "Consulta preventiva"
    descricao = "Consulta de rotina para acompanhamento."
    dataEvento = "2026-09-12T10:00:00"
    nomeVeterinario = "Dra. Clyvo"
    precisaRetorno = $true
    dataRetornoSugerida = "2026-10-12T10:00:00"
} | ConvertTo-Json

$event = Invoke-RestMethod `
    -Uri "$baseUrl/pets/$petId/historico" `
    -Method Post `
    -Headers @{ IdempotencyKey = "apresentacao-evento-001" } `
    -ContentType "application/json" `
    -Body $eventBody

$eventId = $event.id
Invoke-RestMethod "$baseUrl/pets/$petId/historico" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/pets/$petId/historico/$eventId" | ConvertTo-Json -Depth 6
```

O histórico é ordenado do evento mais recente para o mais antigo. Atualização e exclusão:

```powershell
$eventBody = @{
    tipo = "Consulta"
    titulo = "Consulta atualizada"
    descricao = "Retorno registrado."
    dataEvento = "2026-09-12T10:00:00"
    nomeVeterinario = "Dra. Clyvo"
    precisaRetorno = $false
} | ConvertTo-Json

Invoke-WebRequest `
    -Uri "$baseUrl/pets/$petId/historico/$eventId" `
    -Method Put `
    -ContentType "application/json" `
    -Body $eventBody

Invoke-WebRequest "$baseUrl/pets/$petId/historico/$eventId" -Method Delete
```

### 7. Demonstrar alertas preventivos

```powershell
$alertBody = @{
    tipo = "Vacina"
    titulo = "Reforço de vacina"
    descricao = "Verificar carteira de vacinas."
    dataPrevista = "2026-10-30T10:00:00"
    status = "Pendente"
    recomendacaoIa = "Enviar lembrete ao tutor."
} | ConvertTo-Json

$alert = Invoke-RestMethod `
    -Uri "$baseUrl/pets/$petId/alertas" `
    -Method Post `
    -Headers @{ IdempotencyKey = "apresentacao-alerta-001" } `
    -ContentType "application/json" `
    -Body $alertBody

$alertId = $alert.id
Invoke-RestMethod "$baseUrl/pets/$petId/alertas" | ConvertTo-Json -Depth 6
Invoke-RestMethod "$baseUrl/pets/$petId/alertas/$alertId" | ConvertTo-Json -Depth 6
```

Atualize o status para `Concluido` e depois remova o alerta. Os retornos esperados são `204 No Content`.

### 8. Demonstrar recomendação de IA

```powershell
Invoke-RestMethod "$baseUrl/pets/$petId/recomendacao-ia" | ConvertTo-Json -Depth 6
```

A recomendação considera o score de saúde, alertas pendentes ou atrasados e o último evento clínico. O resultado informa prioridade, resumo e próximas ações. A funcionalidade é simulada e não substitui avaliação veterinária.

### 9. Demonstrar observabilidade

Enviar um correlation ID:

```powershell
$response = Invoke-WebRequest `
    -Uri "$baseUrl/pets" `
    -Headers @{ "X-Correlation-ID" = "apresentacao-2026-001" }

$response.Headers["X-Correlation-ID"]
```

O valor retornado deve ser o mesmo enviado. Em seguida, conferir os logs:

```powershell
Get-Content Olli.Api\logs\olli-$(Get-Date -Format yyyyMMdd).log -Tail 20
```

Os registros contêm método, rota, status, duração, `CorrelationId` e `TraceId`.

Para demonstrar tracing, mantenha o terminal da API visível e execute uma listagem ou recomendação. O console exibirá o span HTTP e, quando aplicável, o span interno do serviço, como `PetService.GerarRecomendacao`.

Para consultar as métricas:

```powershell
Invoke-RestMethod "$baseUrl/metrics"
```

As métricas principais são `olli_api_requests_total`, `olli_api_errors_total` e `olli_api_request_duration_ms`. Uma chamada para um recurso inexistente permite mostrar que respostas `404` também são contabilizadas como erro.

### 10. Demonstrar rate limiting

A API permite até 10 requisições por segundo por origem. O comando abaixo deve produzir pelo menos uma resposta `429`:

```powershell
1..12 | ForEach-Object {
    try {
        (Invoke-WebRequest "$baseUrl/pets").StatusCode
    }
    catch {
        $_.Exception.Response.StatusCode.value__
    }
}
```

### 11. Executar a suíte de testes

```powershell
# Todos os testes
dotnet test

# Testes unitários
dotnet test --filter "Category=Unit"

# Testes de integração
dotnet test --filter "Category=Integration"

# Cobertura
dotnet test --collect:"XPlat Code Coverage"
```

Os testes unitários ficam em `Olli.Api.Tests/Unit` e verificam mapeamentos, controllers e regras do `PetService`. Os testes de integração ficam em `Olli.Api.Tests/Integration` e sobem a aplicação com `WebApplicationFactory<Program>`.

Durante a integração, o ambiente `Testing` força InMemory, remove dependências externas e cria um banco exclusivo para cada cenário. A suíte não depende de Oracle, internet ou de dados criados manualmente.

### 12. Executar com Oracle

Pare a API local com `Ctrl+C` e configure a sessão do PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:DatabaseProvider = "Oracle"
$env:ConnectionStrings__FiapOracle = "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
```

Na primeira execução, aplique as migrations:

```powershell
dotnet ef database update --project Olli.Api --startup-project Olli.Api
```

Depois execute:

```powershell
dotnet run --project Olli.Api --no-launch-profile --urls "http://localhost:5127"
```

Valide a conexão antes do CRUD:

```powershell
Invoke-RestMethod "$baseUrl/health/database" | ConvertTo-Json -Depth 6
```

No Oracle, a resposta deve apresentar `oracle-fiap` como `Healthy`. O check executa `SELECT 1 FROM DUAL` e possui timeout de cinco segundos. Como `SeedDatabase` é `false` em Production, os dados devem ser cadastrados pelos endpoints.

Para conferir a persistência no SQL Developer:

```sql
SELECT * FROM "Tutores";
SELECT * FROM "Pets";
SELECT * FROM "EventosSaude";
SELECT * FROM "AlertasPreventivos";
```

O smoke test Oracle é opcional e não expõe credenciais:

```powershell
$env:ORACLE_TEST_CONNECTION_STRING = $env:ConnectionStrings__FiapOracle
dotnet test --filter "Category=Oracle"
```

Sem `ORACLE_TEST_CONNECTION_STRING`, esse teste é ignorado. Com a variável configurada, ele executa o health check real contra o Oracle.
