# DOSSIÊ TÉCNICO — SISTEMA RDO (Relatório Diário de Obra)

**Versão:** 1.0  
**Data:** Abril de 2026  
**Projeto:** TesteAPI / RDO.app  
**Solução:** TesteAPI.sln  

---

## SUMÁRIO

1. [Visão Geral do Sistema](#1-visão-geral-do-sistema)
   - 1.1 [Introdução — Problemática Anterior e Solução Atual](#11-introdução--problemática-anterior-e-solução-atual)
   - 1.2 [Escopo de Atuação](#12-escopo-de-atuação)
2. [Arquitetura da Solução](#2-arquitetura-da-solução)
   - 2.1 [Diagrama Geral](#21-diagrama-geral)
   - 2.2 [Fluxo de Dados](#22-fluxo-de-dados)
   - 2.3 [Tecnologias Implementadas — Stack Utilizada](#23-tecnologias-implementadas--stack-utilizada)
3. [Configurações de Acesso — Segurança](#3-configurações-de-acesso--segurança)
   - 3.1 [Configuração da VPN](#31-configuração-da-vpn)
   - 3.2 [Configuração do Roteador](#32-configuração-do-roteador)
   - 3.3 [Políticas de Regras QNAP — Firewall](#33-políticas-de-regras-qnap--firewall)
4. [Banco de Dados](#4-banco-de-dados)
   - 4.1 [Tipos de Bancos](#41-tipos-de-bancos)
   - 4.2 [Docker](#42-docker)
   - 4.3 [Relacionamentos](#43-relacionamentos)
   - 4.4 [Estratégia de Backup](#44-estratégia-de-backup)
5. [Back-End — API](#5-back-end--api)
   - 5.1 [Arquitetura da API](#51-arquitetura-da-api)
   - 5.2 [Métodos HTTP](#52-métodos-http)
   - 5.3 [Tratamento de Erros](#53-tratamento-de-erros)
   - 5.4 [Endpoints Principais](#54-endpoints-principais)
6. [Front-End](#6-front-end)
   - 6.1 [Fluxo de Navegação](#61-fluxo-de-navegação)
   - 6.2 [Integração com API](#62-integração-com-api)
7. [Configuração e Deploy](#7-configuração-e-deploy)
   - 7.1 [Pré-Requisitos](#71-pré-requisitos)
   - 7.2 [Como Rodar Localmente](#72-como-rodar-localmente)
   - 7.3 [Processo de Deploy](#73-processo-de-deploy)
8. [Performance e Limitações](#8-performance-e-limitações)
   - 8.1 [Limites Conhecidos](#81-limites-conhecidos)
   - 8.2 [Estratégias de Otimização](#82-estratégias-de-otimização)
9. [Gestão de Usuários](#9-gestão-de-usuários)
   - 9.1 [Como Criar Novos Usuários](#91-como-criar-novos-usuários)
   - 9.2 [Reset de Senha](#92-reset-de-senha)
10. [Manutenção e Evolução](#10-manutenção-e-evolução)
    - 10.1 [Versionamento](#101-versionamento)
    - 10.2 [Boas Práticas para Mudanças](#102-boas-práticas-para-mudanças)
11. [Troubleshooting](#11-troubleshooting)
    - 11.1 [Problemas Comuns Reportados em Logs](#111-problemas-comuns-reportados-em-logs)
    - 11.2 [Soluções Rápidas](#112-soluções-rápidas)

---

## 1. VISÃO GERAL DO SISTEMA

### 1.1 Introdução — Problemática Anterior e Solução Atual

#### Contexto e Problema

Antes da implantação do sistema RDO, o registro diário de obras era realizado de forma manual — em papel ou planilhas Excel descentralizadas. Esse modelo apresentava diversas deficiências operacionais:

- **Perda de informação:** documentos físicos sujeitos a extravio, rasuras e deterioração.
- **Falta de padronização:** cada técnico ou engenheiro adotava seu próprio formato de registro.
- **Ausência de rastreabilidade:** impossibilidade de auditar alterações, datas de preenchimento ou responsáveis.
- **Dificuldade de consolidação:** relatórios espalhados em e-mails, pastas de rede e dispositivos pessoais, sem visão centralizada.
- **Acesso remoto inviável:** gestores e clientes não tinham acesso em tempo real ao andamento das obras.
- **Geração de PDF trabalhosa:** a montagem manual de relatórios para envio ao cliente consumia horas de trabalho.

#### Solução Implementada

O sistema RDO é uma solução híbrida composta por três camadas integradas:

1. **Aplicativo Desktop Windows (RDO.app):** interface principal de uso pelos técnicos em campo. Funciona offline com banco de dados local (SQLite) e sincroniza automaticamente com o servidor central quando há conectividade.

2. **API REST Central (TesteAPI):** back-end em ASP.NET Core hospedado em servidor QNAP via Docker, responsável por centralizar todos os dados no banco PostgreSQL e expor endpoints para sincronização e CRUD.

3. **Banco de Dados Remoto (PostgreSQL):** repositório central de todos os dados de obras, relatórios, funcionários, equipamentos e acompanhantes, acessível via VPN WireGuard.

A solução resolve os problemas anteriores ao:
- Padronizar o preenchimento via formulário estruturado no app.
- Garantir persistência local mesmo sem internet (modo offline).
- Sincronizar automaticamente ao reconectar, sem intervenção do usuário.
- Gerar PDFs profissionais dos relatórios com um clique.
- Centralizar todos os dados em servidor seguro com acesso remoto via VPN.

---

### 1.2 Escopo de Atuação

O sistema RDO cobre as seguintes funcionalidades:

| Módulo | Descrição |
|--------|-----------|
| **Obras (Projetos)** | Cadastro e gestão de obras com dados de cliente, contrato, ART, responsáveis e status |
| **Relatórios Diários** | Criação, edição e publicação de RDOs com suporte a rascunho e revisão |
| **Funcionários** | Cadastro de mão de obra com função, empresa e tipo (próprio/terceirizado) |
| **Equipamentos** | Registro de equipamentos utilizados por obra e por relatório |
| **Acompanhantes** | Registro de fiscais, clientes e visitantes presentes na obra |
| **Atividades** | Descrição das atividades executadas no dia, com localização e status |
| **Ocorrências** | Registro de eventos, incidentes e não-conformidades com tags e horários |
| **Materiais** | Controle de materiais utilizados com quantidade e unidade |
| **Fotos** | Registro fotográfico vinculado ao relatório e às atividades |
| **Assinaturas** | Coleta de assinaturas digitais dos responsáveis |
| **Condições Climáticas** | Registro do clima por período (manhã/tarde/noite) |
| **Exportação PDF** | Geração de relatório PDF formatado para envio ao cliente |
| **Sincronização** | Mecanismo automático de push/pull entre SQLite local e PostgreSQL remoto |
| **Gestão de Usuários** | Cadastro, autenticação e controle de perfis de acesso |

**Fora do escopo atual:**
- Módulo financeiro / controle de custos
- Integração com sistemas ERP externos
- Aplicativo mobile (iOS/Android)
- Portal web para clientes

---

## 2. ARQUITETURA DA SOLUÇÃO

### 2.1 Diagrama Geral

```
┌─────────────────────────────────────────────────────────────────────┐
│                        REDE LOCAL / VPN WireGuard                   │
│                                                                     │
│  ┌──────────────────────┐          ┌──────────────────────────────┐ │
│  │   MÁQUINA DO TÉCNICO │          │     SERVIDOR QNAP NAS        │ │
│  │   (Windows 10/11)    │          │                              │ │
│  │                      │  HTTP    │  ┌────────────────────────┐  │ │
│  │  ┌────────────────┐  │◄────────►│  │  Docker Container      │  │ │
│  │  │  RDO.app       │  │          │  │  ASP.NET Core API      │  │ │
│  │  │  (WinUI 3)     │  │          │  │  Porta: 5043           │  │ │
│  │  │                │  │          │  └──────────┬─────────────┘  │ │
│  │  │  ┌──────────┐  │  │          │             │                │ │
│  │  │  │ SQLite   │  │  │          │  ┌──────────▼─────────────┐  │ │
│  │  │  │ (local)  │  │  │          │  │  Docker Container      │  │ │
│  │  │  └──────────┘  │  │          │  │  PostgreSQL 16         │  │ │
│  │  └────────────────┘  │          │  │  Porta: 5432           │  │ │
│  └──────────────────────┘          │  │  BD: RDO_FOCUS         │  │ │
│                                    │  └────────────────────────┘  │ │
│                                    └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

**Componentes principais:**

| Componente | Tecnologia | Localização |
|------------|-----------|-------------|
| App Desktop | WinUI 3 / .NET 8 | Máquina do técnico |
| Banco Local | SQLite (EF Core) | `%LocalAppData%\RDOApp\rdo_local.db` |
| API REST | ASP.NET Core / .NET 10 | Docker no QNAP |
| Banco Remoto | PostgreSQL 16 | Docker no QNAP |
| Acesso Remoto | WireGuard VPN | Roteador / QNAP |

---

### 2.2 Fluxo de Dados

#### Fluxo de Sincronização (Push → Pull)

O mecanismo de sincronização é baseado em **timestamp incremental** (`UpdatedAt`). A cada ciclo de sync, o app executa duas operações em sequência:

```
PUSH (Local → Servidor)
─────────────────────────────────────────────────────────────
1. App lê o timestamp da última sincronização bem-sucedida
   (armazenado em %LocalAppData%\RDOApp\sync_state.json)

2. Coleta do SQLite local todos os registros com:
   - UpdatedAt >= lastSync  OU
   - IsSynced == false (relatórios nunca enviados)

3. Envia payload JSON via POST /api/sync/push

4. API recebe e executa UPSERT no PostgreSQL:
   - Se registro não existe → INSERT
   - Se UpdatedAt do incoming >= UpdatedAt existente → UPDATE
   - Caso contrário → SKIP (dado local é mais antigo)

5. API retorna contagem: { Inserted, Updated, Skipped }

PULL (Servidor → Local)
─────────────────────────────────────────────────────────────
6. App envia GET /api/sync/pull?since={lastSync}

7. API retorna todos os registros com UpdatedAt >= since
   de todas as 15 entidades, incluindo ServerTime

8. App executa UPSERT no SQLite local para cada entidade:
   - Se não existe → INSERT
   - Se UpdatedAt do servidor >= UpdatedAt local → UPDATE

9. App salva ServerTime como novo lastSync

10. Logs gravados em %LocalAppData%\RDOApp\Logs\sync_YYYY-MM-DD.log
```

#### Fluxo de Criação de Relatório

```
Técnico abre o app
       │
       ▼
Login (autenticação local via SQLite)
       │
       ▼
Seleciona Obra → Novo RDO
       │
       ▼
Preenche formulário (atividades, clima, funcionários, fotos...)
       │
       ├─── Salvar Rascunho → IsDraft=true, salvo localmente
       │
       └─── Publicar → IsDraft=false, IsSynced=false
                │
                ▼
         Próximo ciclo de sync automático
                │
                ▼
         Push envia para PostgreSQL
                │
                ▼
         IsSynced=true marcado localmente
```

#### Fluxo de Acesso Remoto

```
Técnico fora da rede local
       │
       ▼
Conecta ao WireGuard VPN
       │
       ▼
Túnel criptografado até o QNAP (IP: 192.168.0.89)
       │
       ▼
Roteador encaminha para QNAP na porta configurada
       │
       ▼
API responde normalmente (mesmo fluxo de sync)
```

---

### 2.3 Tecnologias Implementadas — Stack Utilizada

#### Back-End (API)

| Tecnologia | Versão | Função |
|-----------|--------|--------|
| .NET | 10.0 | Runtime da API |
| ASP.NET Core | 10.0 | Framework web / MVC |
| Entity Framework Core | 10.0.5 | ORM para acesso ao banco |
| Npgsql EF Provider | 10.0.1 | Driver PostgreSQL para EF Core |
| Swashbuckle (Swagger) | 10.1.7 | Documentação interativa da API |
| Microsoft.AspNetCore.OpenApi | 10.0.5 | Suporte OpenAPI |

#### Front-End / Desktop (RDO.app)

| Tecnologia | Versão | Função |
|-----------|--------|--------|
| .NET | 8.0 | Runtime do app desktop |
| WinUI 3 | Windows App SDK 1.8 | Framework de UI nativo Windows |
| Windows App SDK | 1.8.260317003 | APIs modernas do Windows |
| EF Core SQLite | 8.0.0 | Banco de dados local |
| QuestPDF | 2026.2.4 | Geração de relatórios PDF |
| System.Drawing.Common | 8.0.0 | Processamento de imagens |

#### Camada de Dados Compartilhada (RDO.Data)

| Tecnologia | Versão | Função |
|-----------|--------|--------|
| .NET | 8.0 | Runtime da biblioteca |
| EF Core SQLite | 8.0.0 | Modelos e migrations SQLite |
| Npgsql EF Provider | 8.0.0 | Suporte a PostgreSQL (design-time) |

#### Infraestrutura

| Componente | Tecnologia | Detalhes |
|-----------|-----------|---------|
| Servidor | QNAP NAS | IP local: 192.168.0.89 |
| Containerização | Docker | Containers para API e PostgreSQL |
| Banco Remoto | PostgreSQL 16 | BD: `RDO_FOCUS`, usuário: `postgres` |
| VPN | WireGuard | Acesso remoto seguro |
| Firewall | QNAP QuFirewall | Regras de acesso por porta |

---

## 3. CONFIGURAÇÕES DE ACESSO — SEGURANÇA

### 3.1 Configuração da VPN

O sistema utiliza **WireGuard** como solução de VPN para permitir que técnicos acessem o servidor QNAP de fora da rede local. O instalador `wireguard-installer.exe` está disponível na raiz do repositório.

#### Por que WireGuard?

- Protocolo moderno com criptografia de ponta (ChaCha20, Poly1305, Curve25519)
- Configuração simples baseada em par de chaves (pública/privada)
- Baixa latência e alto desempenho comparado a OpenVPN/IPSec
- Suporte nativo no QNAP via pacote VPN Server

#### Configuração no Servidor QNAP

1. Acesse o QNAP via interface web (`http://192.168.0.89`)
2. Abra o **VPN Server** no App Center
3. Selecione a aba **WireGuard**
4. Crie um novo servidor WireGuard:
   - **Porta UDP:** (ex: 51820 — confirme com o administrador de rede)
   - **Endereço IP do túnel:** (ex: 10.0.0.1/24)
   - **DNS:** IP do servidor ou 8.8.8.8
5. Gere o par de chaves do servidor (automático)
6. Anote a **chave pública do servidor** para configurar os clientes

#### Adicionando um Novo Cliente (Peer)

1. No VPN Server → WireGuard → **Adicionar Peer**
2. Preencha:
   - **Nome:** identificação do técnico (ex: `vinicius.toledo`)
   - **Chave pública:** gerada no dispositivo do técnico
   - **IP permitido:** endereço IP do túnel para este cliente (ex: `10.0.0.2/32`)
3. Exporte o arquivo de configuração `.conf` e envie ao técnico

#### Configuração no Dispositivo do Técnico

1. Instale o WireGuard: `wireguard-installer.exe` (disponível na raiz do projeto)
2. Abra o WireGuard → **Importar túnel do arquivo**
3. Selecione o arquivo `.conf` fornecido pelo administrador
4. Clique em **Ativar** para conectar

**Arquivo de configuração típico do cliente:**
```ini
[Interface]
PrivateKey = <chave-privada-do-cliente>
Address = 10.0.0.2/32
DNS = 8.8.8.8

[Peer]
PublicKey = <chave-publica-do-servidor>
AllowedIPs = 192.168.0.0/24
Endpoint = <ip-publico-do-roteador>:<porta-udp>
PersistentKeepalive = 25
```

> **Nota:** O campo `AllowedIPs = 192.168.0.0/24` garante que apenas o tráfego destinado à rede local do escritório passe pelo túnel VPN (split tunneling), sem afetar o acesso à internet do técnico.

---

### 3.2 Configuração do Roteador

Para que o WireGuard funcione externamente, o roteador precisa encaminhar o tráfego UDP da porta VPN para o QNAP.

#### Port Forwarding (Redirecionamento de Porta)

| Protocolo | Porta Externa | IP Destino | Porta Interna | Serviço |
|-----------|--------------|------------|---------------|---------|
| UDP | 51820 (ou configurada) | 192.168.0.89 | 51820 | WireGuard VPN |
| TCP | (opcional) | 192.168.0.89 | 5043 | API RDO (acesso direto sem VPN) |

**Passos gerais (varia por modelo de roteador):**
1. Acesse o painel do roteador (geralmente `192.168.0.1` ou `192.168.1.1`)
2. Navegue até **NAT / Port Forwarding / Virtual Server**
3. Adicione a regra conforme tabela acima
4. Salve e reinicie o roteador se necessário

#### IP Público Dinâmico (DDNS)

Se o IP público do roteador muda periodicamente (IP dinâmico), configure um serviço DDNS:
- **QNAP myQNAPcloud:** serviço gratuito integrado ao QNAP que fornece um hostname fixo (ex: `suaempresa.myqnapcloud.com`)
- Configure em: QNAP → myQNAPcloud → Ativar DDNS
- Use o hostname no campo `Endpoint` do arquivo WireGuard dos clientes

---

### 3.3 Políticas de Regras QNAP — Firewall

O QNAP possui o **QuFirewall** para controle de acesso por IP e porta.

#### Regras Recomendadas

| Prioridade | Ação | Protocolo | Porta | Origem | Descrição |
|-----------|------|-----------|-------|--------|-----------|
| 1 | PERMITIR | UDP | 51820 | Qualquer | WireGuard VPN |
| 2 | PERMITIR | TCP | 5043 | Rede local (192.168.0.0/24) | API RDO |
| 3 | PERMITIR | TCP | 5432 | Rede local (192.168.0.0/24) | PostgreSQL (apenas interno) |
| 4 | PERMITIR | TCP | 8080/443 | Qualquer | Interface web QNAP |
| 5 | NEGAR | TCP | 5432 | Externo | Bloquear acesso direto ao PostgreSQL |
| 6 | NEGAR | TCP | 5043 | Externo | Bloquear API sem VPN (opcional) |

#### Configuração no QuFirewall

1. Acesse QNAP → **Painel de Controle** → **Segurança** → **QuFirewall**
2. Ative o firewall
3. Adicione as regras na ordem de prioridade acima
4. Aplique as alterações

> **Importante:** A porta do PostgreSQL (5432) **nunca deve ser exposta** diretamente à internet. Todo acesso externo ao banco deve ocorrer exclusivamente via VPN.

---
