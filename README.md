# 🚀 ShopeeFlow

> **Intelligent backend service for automating product discovery, analysis, ranking and publication for the Shopee Affiliate Program.**

![.NET](https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge&logo=.net)
![C#](https://img.shields.io/badge/C%23-12-239120?style=for-the-badge&logo=c-sharp)
![GraphQL](https://img.shields.io/badge/GraphQL-E10098?style=for-the-badge&logo=graphql)
![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite)
![License](https://img.shields.io/badge/License-MIT-success?style=for-the-badge)

---

## 📖 About

ShopeeFlow is a personal backend project created to automate one of the most repetitive tasks for Shopee affiliates: finding profitable products.

Instead of manually browsing hundreds of products every day, ShopeeFlow automatically:

- Authenticates with the Shopee Affiliate Platform
- Collects available products
- Applies configurable business rules
- Calculates a dynamic score for every product
- Generates affiliate links in batches
- Stores approved products locally
- Creates a publication queue
- Automatically publishes promotions to WhatsApp (planned)

The goal is to transform hours of repetitive work into a fully automated workflow while keeping the project clean, modular and easy to extend.

---

## ✨ Highlights

- 🔐 Shopee Open API Authentication (SHA256)
- 📦 GraphQL Integration
- 📊 Intelligent Product Scoring
- ⚡ Background Processing
- 🔗 Batch Affiliate Link Generation
- 🗄 SQLite Persistence
- ⏳ Scheduled Publishing Queue
- 📤 WhatsApp Integration (Planned)
- 🧩 Modular Architecture

---

## 💡 Why?

Searching for profitable affiliate products is a repetitive and time-consuming process.

Every day, affiliates need to:

- Browse hundreds of products
- Compare commissions
- Check discounts
- Evaluate store reputation
- Analyze sales volume
- Generate affiliate links
- Create promotional messages
- Publish them manually

ShopeeFlow automates this entire workflow using business rules, allowing new products to be discovered, analyzed and prepared for publication with minimal manual intervention.

---

## 🏗️ Architecture

```text
                  Shopee Open API
                          │
            ┌─────────────▼─────────────┐
            │ Authentication Service    │
            └─────────────┬─────────────┘
                          │
                  Product Collector
                          │
                  Product Analyzer
                          │
                    Score Engine
                          │
             Affiliate Link Generator
                          │
                  SQLite Repository
                          │
                  Publication Queue
                          │
                WhatsApp Publisher
```

---

## 🧠 Score Engine

Every product receives a dynamic score calculated using configurable business rules.

Current scoring factors include:

- ✅ Commission Rate
- ✅ Discount Percentage
- ✅ Product Rating
- ✅ Store Reputation
- ✅ Sales Volume
- ✅ Product Category
- ✅ Exclusive Campaign Eligibility

Each factor has its own configurable weight, allowing the scoring strategy to evolve without changing the application's core logic.

Future versions may also include:

- Price history
- Seasonal trends
- Sales growth
- Conversion history
- AI-assisted recommendations

---

## ✅ Current Features

- Shopee Open API Authentication
- Product Search
- Product Filtering
- Dynamic Score Calculation
- Batch Affiliate Link Generation
- SQLite Local Persistence
- Duplicate Product Prevention
- Background Processing

---

## 🚧 Planned Features

- WhatsApp Integration
- AI-generated Promotional Messages
- Monitoring Dashboard
- Price History
- Product Trend Detection
- Telegram Integration
- Instagram Integration
- Multi-marketplace Support
- Scheduled Product Collection
- Export to Excel/PDF

---

## 🛠️ Technologies

- .NET 9
- ASP.NET Core Web API
- C#
- GraphQL
- SQLite
- BackgroundService
- Dependency Injection
- HttpClient
- REST APIs

---

## 📂 Project Structure

```text
ShopeeFlow

src
│
├── ShopeeFlow.Api
│
├── ShopeeFlow.Application
│
├── ShopeeFlow.Domain
│
├── ShopeeFlow.Infrastructure
│
└── ShopeeFlow.Worker
```

Each project has a single responsibility, making the solution easier to maintain and extend.

---

## ⚙️ Configuration

```json
{
  "Shopee": {
    "AppId": "YOUR_APP_ID",
    "Secret": "YOUR_SECRET"
  }
}
```

---

## 📦 Example Response

```json
{
  "productId": "23893231630",
  "title": "Wireless Vacuum Cleaner",
  "score": 94.8,
  "commission": 18.5,
  "discount": 42,
  "sales": 5120,
  "affiliateUrl": "https://s.shopee.com.br/..."
}
```

---

## 🗺️ Roadmap

- [x] Project planning
- [x] Reverse engineering of Shopee Affiliate API
- [x] Open API research
- [ ] Authentication implementation
- [ ] Product collection
- [ ] Score Engine
- [ ] SQLite persistence
- [ ] Batch affiliate link generation
- [ ] Publication queue
- [ ] WhatsApp integration
- [ ] AI-assisted message generation

---

## 🚀 Getting Started

```bash
git clone https://github.com/yourusername/ShopeeFlow.git

cd ShopeeFlow

dotnet restore

dotnet run
```

---

## 📌 Project Status

🚧 **Under active development**

This project is being developed as a personal study and automation project focused on backend architecture, API integration, background processing and business rule implementation.

---
