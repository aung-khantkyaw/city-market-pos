# CityMarketPOS

A robust, enterprise-grade Point of Sale (POS) & Inventory Management System built using **ASP.NET Core MVC**, **Entity Framework Core**, and the **Repository Pattern**. Designed to streamline retail operations, manage stock logistics, track purchase orders, and handle staff access control securely.

---

## Architecture & Tech Stack

- **Framework:** .NET 8.0 / .NET 9.0 (ASP.NET Core MVC)
- **Database ORM:** Entity Framework Core
- **Database Server:** SQL Server (LocalDB / Azure SQL)
- **Security & Auth:** ASP.NET Core Identity (Role-Based Access Control)
- **Design Pattern:** Repository Pattern (Decoupled Data Access Layer)
- **Frontend UI:** AdminLTE 3 Template / Bootstrap 5, FontAwesome, JavaScript (Dynamic UI Rows)

---

## Key Features

### 1. Admin & Staff Management (RBAC)

- **Role-Based Access Control:** Pre-configured roles for `Manager`, `Inventory`, and `Cashier`.
- **User CRUD:** Dedicated management console for Managers to recruit staff, update profile credentials, and assign business roles securely.

### 2. Catalog & Product Setup

- **Inventory Metadata:** Granular CRUD workflows for managing **Categories**, **Brands**, and **Units of Measurement (UOM)** grouped under a clean treeview layout.
- **Smart Barcode Engine:** Automates stock ingestion by generating structured cryptographic barcodes formatted as `CM-yyMMddHHmm-XXX`.

### 3. Purchasing & GRN Logistics (Inbound Inventory)

- **Supplier Directory:** Comprehensive management of vendor profiles, contact touchpoints, and transactional logistics.
- **Atomic Purchase Orders (PO):** Advanced multi-item relational forms backed by a dynamic JavaScript engine to append raw item matrices (Product, Qty, Cost) seamlessly in a single HTTP POST request.
- **Goods Received Note (GRN): (Pending)** Evaluates ongoing PO processing, flags arrivals, flips order states to `Received`, and automatically updates system `StockQuantity` records.

---

## Getting Started & Project Setup

Follow these architectural setup guidelines to run the ecosystem locally on your workstation.

### Prerequisites

### 1. Clone the Repository

```bash
git clone https://github.com/your-username/CityMarketPOS.git
cd CityMarketPOS

```

### 2. Configure the Connection String

Open `appsettings.json` located at the root of the project and update the connection string to target your local SQL Server instance:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CityMarketPOS_DB;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Database Migrations & Initial Seeding

The system relies on EF Core Migrations to initialize schema architectures. Run the following commands to provision the database.

#### Option A: Via Visual Studio Package Manager Console (PMC)

```powershell
Update-Database

```

#### Option B: Via .NET Core CLI (Terminal)

```bash
dotnet ef database update

```

### 4. Seed Initial Roles & Master Account

When the migration executes successfully, the application triggers data seeding components inside `Program.cs` or your initialization classes to register necessary structural assets:

- Creates roles automatically: `Manager`, `Inventory`, `Cashier`.
- Sets up a root system account if none exists.

#### Default Credentials for Testing

| Role          | Username     | Default Password |
| ------------- | ------------ | ---------------- |
| **Manager**   | `manager1`   | `Manager@123`    |
| **Inventory** | `inventory1` | `Inventory@123`  |
| **Cashier**   | `cashier1`   | `Cashier@123`    |

---

## Core Directory Structural Layout

```text
CityMarketPOS/
│
├── Controllers/            # Orchestrates application HTTP requests & endpoints
│   ├── UserController.cs   # Identity Account management engine (Manager Only)
│   ├── Product.cs          # Catalog and structural barcode generation logic
│   └── PurchaseOrder.cs    # Master-Detail procurement controller
│
├── Data/           # EF Core DbContext and database seeding logic
│   ├── ApplicationDbContext.cs
│   └── DbInitializer.cs
|
├── Repositories/           # Data Abstraction Layers (Repository Pattern)
│   ├── IProductRepository.cs / ProductRepository.cs
│   └── IGRNRepository.cs / GRNRepository.cs
│
├── Models/                 # DB Entities & Context definition matrices
│   ├── ViewModels/             # Strongly typed data transfer contracts for views
│   ├── ApplicationUser.cs  # Custom user model inherited from IdentityUser
│   ├── Product.cs          # Base product data blueprints
│   └── PurchaseOrder.cs    # Transactional schema configurations
│
└── Views/                  # Blade-equivalent Razor rendering engine layers
    ├── User/               # Administrative staff onboarding components
    └── PurchaseOrder/      # Dynamic JavaScript client tabular frameworks

```

---
