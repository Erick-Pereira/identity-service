# 🔐 IDENTITY-SERVICE — ARCHITECTURE GUIDE (CONDOMINIAL AUDIT CONTEXT)

O **Identity Service** é responsável por autenticação, autorização e gestão de identidade dos usuários do sistema.

Ele define **quem pode acessar o sistema** e **o que cada usuário pode fazer**, sendo a base de segurança para todo o ecossistema.

---

# 🧠 RESPONSABILIDADE PRINCIPAL

Gerenciar identidade e acesso, garantindo:

- autenticação segura
- emissão e validação de tokens
- controle de permissões (RBAC)
- isolamento de dados por condomínio (multi-tenant)

---

# ⚙️ FUNÇÕES ESSENCIAIS

- Registro de usuários
- Login/autenticação
- Emissão de JWT
- Refresh token
- Controle de roles:
  - ADMIN
  - SINDICO
  - CONSELHO
- Gestão de tenants (condomínios)
- Validação de token para outros serviços
- Exposição de endpoints de autenticação

---

# 🚫 FRONTEIRAS (O QUE NÃO DEVE CONTER)

- ❌ Não deve conter lógica de negócio financeiro
- ❌ Não deve acessar dados de outros serviços
- ❌ Não deve processar documentos
- ❌ Não deve consumir eventos de domínio
- ❌ Não deve executar IA

---

# 🔁 PADRÕES DE COMUNICAÇÃO

## 📥 Entrada

- HTTP (REST)
  - `/auth/login`
  - `/auth/register`
  - `/auth/refresh`

## 📤 Saída

- JWT (para Gateway e clientes)
- Respostas HTTP

## 🔄 Comunicação interna

- Outros serviços validam JWT localmente (sem chamada síncrona)

## ⚡ Redis

- Armazenamento de refresh tokens
- Blacklist de tokens
- Controle de sessão

---

# 🧱 ESTRUTURA INTERNA (CLEAN ARCHITECTURE)

---

## DOMAIN

### Responsabilidade Principal
Modelar identidade e regras de acesso.

### Funções Essenciais

- Entidades:
  - `User`
  - `Tenant`
- Value Objects:
  - `Email`
  - `PasswordHash`
  - `Role`
  - `TenantId`
- Regras:
  - validação de senha
  - unicidade de usuário
  - associação usuário ↔ condomínio

### Fronteiras

| Pode                          | Não Pode                          |
|-------------------------------|-----------------------------------|
| Modelar usuários              | Conhecer JWT                      |
| Definir roles                 | Conhecer banco                    |
| Validar identidade            | Conhecer HTTP                     |

---

## APPLICATION

### Responsabilidade Principal
Orquestrar autenticação e autorização.

### Funções Essenciais

- Use Cases:
  - `AuthenticateUserUseCase`
  - `RegisterUserUseCase`
  - `GenerateTokenUseCase`
  - `RefreshTokenUseCase`
- Coordenar:
  - validação de credenciais
  - geração de tokens
  - controle de sessão

### Fronteiras

| Pode                          | Não Pode                          |
|-------------------------------|-----------------------------------|
| Orquestrar login              | Implementar JWT diretamente       |
| Validar usuário               | Persistir diretamente             |
| Definir fluxo de auth         | Conhecer infraestrutura           |

---

## INFRASTRUCTURE

### Responsabilidade Principal
Implementar persistência e segurança.

### Funções Essenciais

- Persistência (PostgreSQL via EF Core):
  - Users
  - Tenants
- Hash de senha (BCrypt ou similar)
- Geração de JWT
- Integração com Redis (refresh/blacklist)

### Fronteiras

| Pode                          | Não Pode                          |
|-------------------------------|-----------------------------------|
| Gerar token                   | Definir regras de acesso          |
| Persistir dados               | Lógica de negócio                 |
| Validar hash                  | Orquestrar fluxo                  |

---

## API

### Responsabilidade Principal
Expor endpoints de autenticação.

### Funções Essenciais

- Endpoints:
  - POST `/auth/login`
  - POST `/auth/register`
  - POST `/auth/refresh`
- Validação de input
- Retorno de tokens

### Fronteiras

| Pode                          | Não Pode                          |
|-------------------------------|-----------------------------------|
| Expor autenticação            | Lógica de negócio                 |
| Validar dados                 | Processamento pesado              |

---

# 📊 FLUXO DO IDENTITY SERVICE

```text id="flow-identity-service"
[Client]
   ↓
[Login Request]
   ↓
[API]
   ↓
[Application Layer]
   ↓
[Validate Credentials]
   ↓
[Generate JWT + Refresh Token]
   ↓
[Return Token]
````

---

# 🔐 MODELO DE TOKEN

## JWT Payload

```text id="jwt-structure"
{
  userId,
  tenantId,
  role,
  exp
}
```

---

# 🧩 MULTI-TENANCY

## Regra obrigatória

```text id="tenant-rule"
Todo usuário pertence a um Tenant (Condomínio)
Todo dado no sistema deve ser filtrado por tenantId
```

---

# ⚡ CRITÉRIOS DE QUALIDADE

* Segurança forte (hash + JWT)
* Stateless authentication
* Baixa latência
* Escalabilidade
* Revogação de tokens (via Redis)

---

# 📊 PODE VS NÃO PODE

| Pode                | Não Pode                            |
| ------------------- | ----------------------------------- |
| Autenticar usuários | Processar dados financeiros         |
| Emitir JWT          | Executar IA                         |
| Gerenciar tenants   | Consumir eventos de domínio         |
| Controlar acesso    | Acessar outros serviços diretamente |

---

# 🐳 INFRAESTRUTURA (DOCKER)

* Container stateless (exceto Redis/Postgres)

## Dependências

* PostgreSQL
* Redis

## Variáveis obrigatórias:

* `JWT_SECRET`
* `JWT_EXPIRATION`
* `POSTGRES_CONNECTION`
* `REDIS_CONNECTION`

---

# 🔐 GARANTIAS DO SERVIÇO

* Nenhum acesso sem autenticação válida
* Nenhum dado acessado fora do tenant
* Tokens podem ser revogados

---

# 🎯 CONCLUSÃO

O Identity Service garante segurança e isolamento do sistema.

Fluxo:

```text id="identity-final-flow"
Autenticar → Gerar Token → Validar → Autorizar
```

Ele habilita:

* Gateway Service (controle de acesso)
* Segurança de todos os serviços
