# Olli API - Clyvo VET

API em .NET para o Challenge Clyvo VET.

Repositório: https://github.com/Olli-Pet/challenge_clyvo_dotnet.git <br>
Branch: Importar-projeto (Pull request com comparação entre projeto da Sprint 2 com Sprint 3)

A Olli e uma plataforma de acompanhamento preventivo da saude pet. O MVP conecta tutor, pet e clinica veterinaria por meio de cadastro, historico clinico, alertas preventivos e recomendacao simulada de IA.

## Integrantes

- Mirelly Sousa Alves - RM566299
- Gabriely Bonfim - RM566242
- Henrique Rodrigues - RM562917
- Andre Rosa Colombo - RM563112
- Ruan Luca Feliciano - RM562218

## Tecnologias

- C#
- ASP.NET Core Minimal API
- Entity Framework Core
- Oracle Entity Framework Core
- Banco em memoria para desenvolvimento local
- OpenAPI com Scalar
- Health Checks
- Rate Limiter
- xUnit para testes automatizados

O projeto esta configurado com `net10.0`.

## Estrutura

- `Olli.Api`: projeto principal da API.
- `Olli.Api/Domain`: entidades, DTOs, enums, contexto do banco e seed.
- `Olli.Api/Endpoints`: rotas da API.
- `Olli.Api/Migrations`: migrations do Entity Framework para Oracle.
- `Olli.Api.Tests`: testes automatizados.
- `docs`: documentacao auxiliar sobre Oracle e migrations.
- `evidencias`: prints e roteiro visual da entrega do challenge.

## Dominio

Principais entidades do MVP:

- `Tutor`: responsavel pelo pet.
- `Pet`: animal acompanhado pela Olli.
- `EventoSaude`: historico clinico longitudinal do pet.
- `AlertaPreventivo`: lembretes e cuidados preventivos.
- `RecomendacaoIaDTO`: resposta simulada da IA preventiva.

## Rotas Principais

Tutores:
- `GET /tutores`
- `GET /tutores/{id}`
- `POST /tutores`
- `PUT /tutores/{id}`
- `DELETE /tutores/{id}`

Pets:
- `GET /pets`
- `GET /pets/{id}`
- `POST /pets`
- `PUT /pets/{id}`
- `DELETE /pets/{id}`

Historico de saude:
- `GET /pets/{id}/historico`
- `GET /pets/{id}/historico/{idEvento}`
- `POST /pets/{id}/historico`
- `PUT /pets/{id}/historico/{idEvento}`
- `DELETE /pets/{id}/historico/{idEvento}`

Alertas preventivos:
- `GET /pets/{id}/alertas`
- `GET /pets/{id}/alertas/{idAlerta}`
- `POST /pets/{id}/alertas`
- `PUT /pets/{id}/alertas/{idAlerta}`
- `DELETE /pets/{id}/alertas/{idAlerta}`

IA simulada:
- `GET /pets/{id}/recomendacao-ia`

As rotas `POST` exigem o header `IdempotencyKey`.

## Status HTTP

A API usa status HTTP para representar os resultados:

- `200 OK`: consulta realizada com sucesso.
- `201 Created`: recurso criado com sucesso.
- `204 No Content`: atualizacao ou exclusao realizada com sucesso.
- `400 Bad Request`: dados obrigatorios ausentes ou header `IdempotencyKey` ausente.
- `404 Not Found`: recurso nao encontrado.
- `429 Too Many Requests`: limite de requisicoes excedido.

## Como executar a API

O projeto suporta **dois modos**. Use apenas **um por vez** na porta `5127` (se aparecer erro de porta em uso, encerre a instancia anterior com `Ctrl+C`).

| | Modo localhost (padrao) | Modo Oracle FIAP |
|---|-------------------------|------------------|
| Ambiente | `Development` | `Production` |
| Banco | Em memoria + seed | Oracle (persistente) |
| Configuracao | `appsettings.Development.json` | `appsettings.json` |
| Documentacao Scalar | Sim (`/scalar/v1`) | Nao (use Postman ou `OlliApi.http`) |
| Health check | `in-memory-db` | `oracle-fiap` |

### Pre-requisitos (ambos os modos)

```powershell
dotnet restore
```

### Modo 1 — Localhost (padrao / Development)

Comando recomendado para desenvolvimento, testes rapidos e interface Scalar:

```powershell
dotnet run --project Olli.Api
```

O `launchSettings.json` define `Development`, porta **5127** e banco **em memoria**.

URLs:

- API: `http://localhost:5127`
- Scalar (OpenAPI): `http://localhost:5127/scalar/v1`
- OpenAPI JSON: `http://localhost:5127/openapi/v1.json`
- Health: `http://localhost:5127/health` (entrada `in-memory-db`)
- Health UI: `http://localhost:5127/health-ui`

Este projeto usa **Scalar** como documentacao OpenAPI (nao Swagger UI).

**Dados iniciais (seed):** ao subir em Development, a API cria automaticamente (ver `Olli.Api/Domain/OlliDbSeed.cs`):

- Tutor: `Mirelly Santos`
- Pet: `Olli`

Os dados em memoria existem enquanto a API estiver rodando. Ao reiniciar, o banco e recriado com o seed.

### Modo 2 — Oracle FIAP (Production)

**1. Credenciais** — em `Olli.Api/appsettings.json`, configure sua connection string (nao commite senha real no GitHub):

```json
"DatabaseProvider": "Oracle",
"FiapOracle": "User Id=SEU_RM;Password=SUA_SENHA;Data Source=oracle.fiap.com.br:1521/ORCL;"
```

Detalhes em [docs/ORACLE_E_MIGRATIONS.md](docs/ORACLE_E_MIGRATIONS.md).

**2. Migrations** — crie/atualize as tabelas no Oracle (uma vez, ou quando houver nova migration):

```powershell
dotnet ef database update --project Olli.Api --startup-project Olli.Api
```

`ApplyMigrationsOnStartup` permanece `false`; a API nao altera o schema automaticamente ao iniciar.

**3. Subir a API com Oracle** — use `--no-launch-profile` para o ambiente Production nao ser sobrescrito pelo `launchSettings` (que forca Development):

```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run --project Olli.Api --no-launch-profile --urls "http://localhost:5127"
```

Sem `--urls`, a API sobe na porta **5000** por padrao. Com `--urls`, mantem **5127** (mesma porta do `OlliApi.http` e do Postman).

URLs no modo Oracle:

- API: `http://localhost:5127`
- Health: `http://localhost:5127/health` (entrada `oracle-fiap` com status Healthy)
- Health UI: `http://localhost:5127/health-ui`

**Testar CRUD no Oracle:** Postman, Insomnia ou `Olli.Api/OlliApi.http`. Toda rota `POST` exige o header `IdempotencyKey`. Em Production nao ha seed; crie tutores e pets via POST antes de consultar.

**Conferir no banco:** SQL Developer (ou similar), conectado com seu usuario FIAP:

```sql
SELECT * FROM "Tutores";
SELECT * FROM "Pets";
```

## Evidencias da entrega

Prints e fluxos documentados para o challenge estao em:

- Pasta: [`evidencias/`](evidencias/)
- Indice com imagens: [`evidencias/evidencias.md`](evidencias/evidencias.md)

Inclui demonstracao do **modo Oracle** (migration, health, Postman, SQL Developer) e do **modo localhost** (terminal, Scalar, CRUD em memoria).

## Health Check

- `GET /health` — status da API e do provedor de banco ativo.
- `/health-ui` — painel visual do health check.

| Modo | O que aparece em `/health` |
|------|----------------------------|
| Development (localhost) | `in-memory-db` — banco em memoria para desenvolvimento local |
| Production (Oracle) | `oracle-fiap` — conexao com Oracle FIAP |

## Como Usar o CRUD

CRUD significa:

- `POST`: criar.
- `GET`: listar ou buscar.
- `PUT`: atualizar.
- `DELETE`: deletar.

Exemplo para criar um tutor:

```http
POST http://localhost:5127/tutores
Content-Type: application/json
IdempotencyKey: tutor-001

{
  "nome": "Ana Souza",
  "email": "ana@email.com",
  "telefone": "+55 11 98888-0000"
}
```

Depois de criar, a API retorna `201 Created` e informa o `id` do tutor criado.

Exemplo para criar um pet vinculado a um tutor:

```http
POST http://localhost:5127/pets
Content-Type: application/json
IdempotencyKey: pet-001

{
  "idTutor": 1,
  "nome": "Nina",
  "especie": "Gato",
  "raca": "SRD",
  "dataNascimento": "2021-03-12T00:00:00",
  "pesoKg": 4.2,
  "scoreSaude": 88,
  "observacoes": "Pet castrada e ativa."
}
```

O CRUD pode ser testado pelo arquivo `Olli.Api/OlliApi.http`, pelo Scalar (`/scalar/v1`, apenas em Development), por Postman/Insomnia (recomendado no modo Oracle) ou por qualquer cliente HTTP.

## Testes

```powershell
dotnet test
```

Os testes validam rotas principais, criacao com `IdempotencyKey`, retorno `404`, recomendacao de IA simulada e rate limit.
