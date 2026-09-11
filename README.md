# TriagemFacil – Back‑End
## Descrição
Este repositório contém a **API back‑end** do projeto **TriagemFacil**, responsável por gerenciar a triagem de pacientes/emergências. A aplicação foi desenvolvida em **.NET 7** utilizando **ASP.NET Core Web API**, **Entity Framework Core** e segue a arquitetura limpa (Clean Architecture) para facilitar a manutenção e a escalabilidade.
## Tecnologias
- **.NET 8**
- **ASP.NET Core 8** (Web API)
- **Entity Framework Core** (code‑first)
- **SQL Server** (ou outro provedor suportado pelo EF Core)
- **Swagger** para documentação automática da API
- **GitHub Actions** (CI simples – opcional)
## Pré‑requisitos
- [.NET SDK 8.x](https://dotnet.microsoft.com/download) instalado
- **Git**
- **SQL Server** (ou SQLite para testes locais)
- (Opcional) **Visual Studio 2022** ou **VS Code** com extensões C#
## Como rodar o projeto localmente
1. **Clonar o repositório**  
   ```bash
   git clone https://github.com/StefaniRibeiro/TriagemFacil-Back-End.git
   cd TriagemFacil-Back-End/backend/SistemaTriagem.Api
Restaurar dependências

bash


dotnet restore
Configurar a connection string

Copie o arquivo appsettings.example.json para appsettings.json e ajuste a string de conexão DefaultConnection apontando para seu banco de dados.
Aplicar migrações e criar o banco

bash


dotnet ef database update
Executar a API

bash


dotnet run --project SistemaTriagem.Api.csproj
A aplicação ficará disponível em https://localhost:5001 (ou http://localhost:5000).

Acessar a documentação Swagger
Abra o navegador e acesse https://localhost:5001/swagger para testar os endpoints.

Estrutura de pastas (relevante)


backend/
└─ SistemaTriagem.Api/
   ├─ Controllers/        # Controllers da API
   ├─ DTOs/               # Data Transfer Objects
   ├─ Models/             # Entidades do domínio
   ├─ Services/           # Serviços de negócio
   ├─ Data/               # Contexto do EF Core e migrações
   ├─ Program.cs          # Configuração da aplicação
   └─ appsettings.json    # Configurações (connection string, JWT, etc.)
Testes
Os testes (se houver) estão localizados em um projeto de teste separado, normalmente SistemaTriagem.Api.Tests. Para executá‑los:

bash


dotnet test
Contribuição
Fork este repositório
Crie uma branch para sua feature ou correção (git checkout -b feature/minha-feature)
Commit suas mudanças (git commit -m "Descrição da mudança")
Push para seu fork (git push origin feature/minha-feature)
Abra um Pull Request descrevendo a mudança
Dica: mantenha o código formatado com o dotnet format e siga as convenções de naming do projeto.

Licença
Este projeto está licenciado sob a MIT License – veja o arquivo LICENSE para detalhes.
