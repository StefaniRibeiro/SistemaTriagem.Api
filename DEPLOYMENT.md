# Production setup: Render, Aiven, Vercel and Groq

The API, frontend and database schema are in separate Git repositories. The API repository has its Dockerfile at its root; the frontend uses Vite from its repository root.

## 1. Aiven MySQL

Create or start a MySQL service. From its service overview, note the host, port, database, username and password. Configure network access so Render can reach MySQL; restrict access where the Aiven plan allows it.

The API does not create tables and this repository has no EF Core migrations. Run `schema.sql` from the separate `database-SistemaTriagem` repository once, using Aiven's SQL editor or a MySQL client connected to the service. The script creates/selects `triagem_saude` and inserts a demo administrator. If the Aiven user cannot create databases, create `triagem_saude` in the panel first and run the remaining schema in that database.

For an existing database, apply `database-SistemaTriagem/migrations/2026-11-27_status_atendimento.sql` before deploying the API. It adds `EM_ATENDIMENTO` and `ATENDIDA` to the `triagem.status` enum, which are used by the professional dashboard.

The schema includes a demo `admin` account. Replace or remove it before allowing public access. Do not use real patient data in this demo deployment.

## 2. Render API

Configure the Render service as **Docker**, using the API repository root (`.`). Add these environment variables in the Render dashboard; do not commit their values:

| Variable | Value |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ConnectionStrings__Default` | `Server=<host>;Port=<port>;Database=triagem_saude;User=<user>;Password=<password>;SslMode=Required;` |
| `Jwt__Key` | A random, unique, long secret for this environment (at least 32 bytes) |
| `Jwt__Issuer` | `SistemaTriagemInteligente` |
| `Jwt__Audience` | `SistemaTriagemInteligenteClient` |
| `Jwt__ExpiryMinutes` | `480` |
| `Ollama__BaseUrl` | `https://api.groq.com/openai/v1` |
| `Ollama__ApiKey` | Your Groq API key (secret) |
| `Ollama__Model` | A chat model ID currently enabled in your Groq account |
| `Ollama__TimeoutSeconds` | `60` |
| `Cors__AllowedOriginsCsv` | Exact public Vercel origin, e.g. `https://your-project.vercel.app` |
| `Cadsus__MockMode` | `true` for the demo; the real RNDS integration is not implemented yet |

`SslMode=Required` encrypts the MySQL connection. If Aiven requires strict CA validation, configure the service CA certificate as described in the current Aiven dashboard instructions.

The API health endpoint is `GET /api/health`. Swagger is enabled only in `Development`.

## 3. Vercel frontend

Because the frontend is a separate repository, configure the Vercel project with root directory `.` and the Vite framework. Set this build environment variable:

| Variable | Value |
| --- | --- |
| `VITE_API_URL` | `https://sistematriagem-api-ft2v.onrender.com/api` (or the current Render service URL, ending in `/api`) |

Vite embeds this value in the frontend build. Redeploy after changing it. `vercel.json` rewrites SPA routes to `index.html`.

## 4. Manual smoke check

1. Confirm the database contains `paciente`, `triagem`, `sintoma`, `mensagem`, `usuario` and `fluxo_automacao` tables.
2. Open `https://<render-service>/api/health` and confirm the response contains `status: ok`.
3. Open the Vercel frontend, use fictitious patient details, and confirm the triage is saved and the chatbot responds.
4. Check Render logs if MySQL, CORS or Groq fails.

## Limits before real use

- `Cadsus:MockMode` is enabled by default and returns simulated patient data. The RNDS implementation in this code is a stub.
- Chat messages and history are sent to the provider configured in `Ollama__BaseUrl` (Groq in this setup). Do not include CPF, CNS, names or identifiable clinical details in a demo using an external provider.
- The schema seeds a demo administrator. Set a valid BCrypt password hash before exposing the admin panel.
- Public patient identification and chatbot endpoints do not require authentication. Do not store or process real health data on this public deployment until access controls and appropriate protections are in place.
- ChromaDB is optional; the API falls back to local knowledge files when it is unavailable. n8n is optional; configure `N8n__WebhookBaseUrl` if you use it.
