# **ShopeeFlow \- Documentação Técnica**

### **Versão 1.0**

---

# **Objetivo**

Desenvolver uma aplicação Backend responsável por automatizar todo o fluxo de pesquisa, análise e divulgação de produtos do Programa de Afiliados da Shopee.

O sistema deverá consumir exclusivamente a Open API oficial da Shopee (GraphQL), realizar toda a análise dos produtos utilizando regras próprias e, futuramente, integrar-se ao WhatsApp para realizar a publicação automática das promoções.

Toda a arquitetura deverá ser modular para facilitar manutenção e futuras expansões.

Documentação oficial utilizada como referência:

[https://affiliate.shopee.com.br/open\_api/document?type=overview](https://affiliate.shopee.com.br/open_api/document?type=overview&utm_source=chatgpt.com)

---

# **Tecnologias**

Backend

* .NET 9  
* ASP.NET Core Web API

Persistência

* SQLite (primeira versão)

Integrações

* Shopee Open API (GraphQL)  
* WhatsApp API (futuro)

---

# **Arquitetura**

O projeto seguirá uma arquitetura inspirada em MVC, porém organizada por responsabilidades.

ShopeeFlow

│

├── Controllers

├── Services

├── Repositories

├── DTOs

├── Models

├── Interfaces

├── Integrations

│      ├── Shopee

│      └── WhatsApp

├── Jobs

├── Helpers

├── Enums

├── Configurations

├── Middleware

├── Resources

├── appsettings.json

└── Program.cs  
---

# **Padrões**

O projeto deverá seguir os seguintes padrões.

* Código totalmente em inglês.  
* Métodos pequenos e com responsabilidade única.  
* DTOs para entrada e saída.  
* Interfaces para todos os Services e Repositories.  
* Dependency Injection nativa do .NET.  
* IConfiguration utilizando appsettings.json.  
* Utilização de IStringLocalizer para mensagens futuras.  
* Separação completa entre regra de negócio e integração com APIs externas.

---

# **Fluxo Principal**

Authenticate  
↓

Search Products  
↓

Apply Filters  
↓

Calculate Score  
↓

Generate Affiliate Links  
↓

Retrieve Product Images  
↓

Persist Products  
↓

Create Publish Queue  
↓

Send WhatsApp Message

Cada etapa deverá ser implementada separadamente.

---

# **Etapas de Desenvolvimento**

## **Etapa 1**

### **Criação do Projeto**

Nome

ShopeeFlow

Criar uma Web API utilizando .NET.

Configurar:

* Swagger  
* appsettings  
* Dependency Injection  
* Estrutura de pastas  
* Localização (Resources)  
* Configurações iniciais

Nenhuma regra de negócio será implementada nesta etapa.

---

## **Etapa 2**

### **Estrutura Base**

Criar toda a estrutura do projeto.

Pastas:

Controllers

Services

Repositories

Interfaces

DTOs

Models

Jobs

Helpers

Enums

Configurations

Integrations

Resources

Criar também:

BaseResponse

PaginatedResponse

Result

AppSettings

---

## **Etapa 3**

### **Integração com Shopee**

Criar um módulo isolado para comunicação com a Open API.

Responsabilidades:

* Autenticação  
* Geração da assinatura SHA256  
* Comunicação GraphQL  
* Tratamento de erros  
* Retry  
* Timeout

Toda comunicação com a Shopee deverá passar exclusivamente por este módulo.

---

## **Etapa 4**

### **Buscar Produtos**

Consumir a Query responsável pela listagem de produtos.

Aplicar filtros configuráveis.

Exemplos:

* Categoria  
* Comissão mínima  
* Avaliação mínima  
* Quantidade mínima de vendas  
* Ordenação

O resultado deverá ser convertido para DTOs internos.

---

## **Etapa 5**

### **Motor de Score**

Receber a lista de produtos.

Calcular uma pontuação baseada em:

* Comissão  
* Avaliação  
* Quantidade de vendas  
* Desconto  
* Categoria

Os pesos deverão ser configuráveis futuramente.

O resultado deverá ser uma lista ordenada pelo maior Score.

---

## **Etapa 6**

### **Geração dos Links**

Após selecionar os melhores produtos.

Gerar os links de afiliado em lote.

Utilizar a API oficial responsável pela geração dos links.

---

## **Etapa 7**

### **Imagens**

Após possuir a lista final.

Recuperar a imagem de cada produto.

**Pendente de validação**

Necessário confirmar se existe um endpoint em lote ou apenas consulta individual utilizando o ImageId retornado na listagem.

---

## **Etapa 8**

### **Persistência**

Inicialmente será utilizado SQLite.

O banco armazenará apenas informações necessárias.

Tabelas previstas:

Products

PublishQueue

PublishedProducts

Settings

Não será armazenado histórico completo de todos os produtos.

O objetivo será apenas evitar duplicidade de publicação e manter a fila.

---

## **Etapa 9**

### **Fila**

Após gerar Score e Link.

Os produtos serão adicionados à fila.

A fila será responsável por controlar a ordem de publicação.

Não ocorrerá envio imediato ao WhatsApp.

---

## **Etapa 10**

### **Publicação**

Um processo independente verificará a fila.

Caso exista uma promoção pendente.

Será enviada ao WhatsApp.

O intervalo entre publicações será configurável.

Valor inicial:

10 minutos

Posteriormente poderá ser reduzido para 5 minutos.

---

## **Etapa 11**

### **Controle de Duplicidade**

Será criada uma tabela contendo:

* ProductId  
* PublishedDate

Antes de publicar um produto será realizada uma consulta.

Caso o produto tenha sido publicado nos últimos 14 dias.

Ele será ignorado.

Após cada coleta diária será executada uma limpeza.

Todos os registros com mais de 14 dias serão removidos.

Dessa forma não será necessário manter um Job exclusivo apenas para limpeza.

---

# **Futuras Evoluções**

* Dashboard  
* Telegram  
* Instagram  
* TikTok  
* Inteligência Artificial  
* Histórico de preços  
* Histórico de Score  
* Múltiplos marketplaces

