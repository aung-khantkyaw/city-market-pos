# CityMarketPOS — Project Plan (Next.js Rebuild Blueprint)

> Source: reverse-engineered from the existing ASP.NET Core MVC + EF Core + SQL Server codebase (`Controllers/`, `Models/`, `Repositories/`, `Data/`, `Views/`).
> Purpose: single source of truth for re-implementing CityMarketPOS in **Next.js (TypeScript)** with a REST API layer.
> Legacy stack: .NET 10, ASP.NET Core MVC, EF Core, SQL Server (LocalDB), ASP.NET Core Identity, Session state (8h), AdminLTE 3 / Bootstrap 5.
> Routing (legacy): conventional only — `{controller}/{action}/{id?}`, default `Account/Login`. No attribute routes.

---

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Roles & Authorization Matrix](#2-roles--authorization-matrix)
3. [Tech Stack Proposal (Next.js)](#3-tech-stack-proposal-nextjs)
4. [Proposed Next.js Project Structure](#4-proposed-nextjs-project-structure)
5. [Database Design](#5-database-design)
6. [Business Rules & Calculations](#6-business-rules--calculations)
7. [Project Flows](#7-project-flows)
8. [API Endpoints Specification](#8-api-endpoints-specification)
9. [UI / Page List (App Router)](#9-ui--page-list-app-router)
10. [Validation Rules](#10-validation-rules)
11. [Audit Logging Specification](#11-audit-logging-specification)
12. [Session, Cart & Counter Rules](#12-session-cart--counter-rules)
13. [Reports Specification](#13-reports-specification)
14. [Seed Data](#14-seed-data)
15. [Migration Notes (MVC → Next.js)](#15-migration-notes-mvc--nextjs)
16. [Implementation Phases](#16-implementation-phases)

---

## 1. System Overview

CityMarketPOS is a retail **Point of Sale + Inventory Management System** with 5 functional domains:

| # | Domain | Description |
|---|--------|-------------|
| 1 | Identity & Staff (RBAC) | Login/logout, staff CRUD, 3 roles |
| 2 | Catalog / Master Data | Category, UOM, Product, Supplier, Counter (all soft-deletable except Supplier delete which is a hard cascade) |
| 3 | Purchasing (Inbound) | Supplier ↔ Product M2M, Purchase Orders (master-detail), GRN receiving with batch creation |
| 4 | Inventory & Stock | Batch-level stock (`GRNDetail` is the stock unit), stock grouping by PO, low-stock alerts, expiry management, ground-balance (stock-take, count-only) |
| 5 | POS & Sales (Outbound) | Counter selection → POS session open → terminal (search/cart/checkout with FEFO batch selection) → session close → sales/inventory reports, audit trail |

Key architectural fact for the rebuild: **stock is tracked per GRN batch (`GRNDetail.CurrentStockQuantity`), NOT per product.** Product stock = `SUM(CurrentStockQuantity)` over its batches. Every sale line references a specific batch (`SaleDetail.GRNDetailId`).

---

## 2. Roles & Authorization Matrix

Seeded roles: `Manager`, `Inventory`, `Cashier`.

| Capability | Manager | Inventory | Cashier |
|---|---|---|---|
| Login / Logout / Home dashboard | ✅ | ✅ | ✅ |
| Staff management (create/edit/delete users) | ✅ | ❌ | ❌ |
| Category / UOM / Product CRUD | ✅ | ✅ | ❌ |
| Supplier CRUD (+ hard-cascade delete) | ✅ | ✅ | ❌ |
| Purchase Order create/list/details + lookup APIs | ✅ | ✅ | ❌ |
| GRN create/list/details/history | ✅ | ✅ | ❌ |
| Stock view + batch details | ✅ | ✅ | ❌ |
| Expiry view / write-off / reorder | ✅ | ✅ | ❌ |
| Ground Balance (stock-take) CRUD + lookups | ✅ | ✅ | ❌ |
| Counter CRUD | ✅ | ❌ | ❌ |
| POS: Select counter, open/close session, terminal, cart, checkout, today-sales | ✅ | ❌ | ✅ |
| Sales report, session report, inventory valuation | ✅ | ❌ | ❌ |
| Audit log view | ✅ | ❌ | ❌ |

> ⚠️ Legacy gaps to fix in rebuild: `Account/Index|Edit|Delete` and `Home/Index` had **no `[Authorize]`** attribute. In Next.js, enforce auth on **every** route + API route via middleware and server-side session checks. Default-deny.

Default seed accounts (change in production):

| Role | Username | Password | Email |
|---|---|---|---|
| Manager | `manager1` | `Manager@123` | manager@citymarket.com |
| Inventory | `inventory1` | `Inventory@123` | inventory@citymarket.com |
| Cashier | `cashier1` | `Cashier@123` | cashier@citymarket.com |

---

## 3. Tech Stack Proposal (Next.js)

| Layer | Recommendation |
|---|---|
| Framework | Next.js 14+ (App Router), TypeScript strict |
| UI | Tailwind CSS + shadcn/ui, TanStack Table for all index tables, `react-hook-form` + `zod` for forms |
| Auth | NextAuth.js (Auth.js v5) credentials provider, bcrypt password hashing, JWT or database sessions; role stored in session token (`Manager \| Inventory \| Cashier`) |
| DB | Keep **SQL Server** if ops requires it (Prisma `sqlserver` connector), otherwise **PostgreSQL**. Prisma ORM replaces EF Core + Repositories |
| API style | Next.js Route Handlers (`/api/...`) returning JSON, or separate NestJS/ASP.NET API — Route Handlers are sufficient for this size |
| State (POS) | Server-persisted cart is recommended (see §12); short-term: HTTP-only session cookie + Redis/DB cart store. Do NOT use localStorage as source of truth for cart/session |
| Printing | Receipt component with `window.print()` CSS; generate sale number server-side only |
| Validation | `zod` schemas shared between client forms and API handlers |
| Audit | Server-side audit helper called from every mutating API (see §11) |
| Time | Store UTC (`DateTimeOffset` / `timestamptz`); render in viewer timezone. Legacy used `DateTime.Now` (server-local) — do not copy this |

---

## 4. Proposed Next.js Project Structure

```
citymarket-pos-next/
├── app/
│   ├── (auth)/login/page.tsx
│   ├── (dashboard)/
│   │   ├── page.tsx                  # role dashboard (Home/Index)
│   │   ├── staff/page.tsx            # Account/Index
│   │   ├── staff/new/page.tsx        # Account/CreateUser
│   │   ├── staff/[id]/edit/page.tsx  # Account/EditUser
│   │   ├── categories/page.tsx
│   │   ├── uoms/page.tsx
│   │   ├── products/page.tsx
│   │   ├── suppliers/page.tsx
│   │   ├── purchase-orders/page.tsx
│   │   ├── purchase-orders/new/page.tsx (modal or page)
│   │   ├── purchase-orders/[id]/page.tsx
│   │   ├── grn/page.tsx
│   │   ├── grn/new/page.tsx          # ?poId=
│   │   ├── grn/[id]/page.tsx
│   │   ├── grn/by-po/[poId]/page.tsx
│   │   ├── stock/page.tsx
│   │   ├── stock/[batchId]/page.tsx
│   │   ├── expiry/page.tsx
│   │   ├── expiry/[batchId]/page.tsx
│   │   ├── ground-balance/page.tsx
│   │   ├── ground-balance/new/page.tsx
│   │   ├── ground-balance/[batchNo]/page.tsx
│   │   ├── ground-balance/[batchNo]/edit/page.tsx
│   │   ├── counters/page.tsx + CRUD pages
│   │   ├── pos/select-counter/page.tsx
│   │   ├── pos/terminal/page.tsx
│   │   ├── pos/today-sales/page.tsx
│   │   ├── pos/end-session/page.tsx
│   │   ├── reports/sales/page.tsx
│   │   ├── reports/inventory-valuation/page.tsx
│   │   ├── reports/sessions/[sessionId]/page.tsx
│   │   └── audit-logs/page.tsx + [id]
│   ├── api/  (mirrors §8)
│   └── layout.tsx
├── components/ (tables, modals, pos-cart, receipt, filters)
├── lib/ (auth, prisma, audit, numbering, money, dates)
├── prisma/schema.prisma + seed.ts
└── middleware.ts (role-based route guard)
```

---

## 5. Database Design

### 5.1 Entity-Relationship Overview

```
Category 1──* Product *──* Supplier (M2M: ProductSupplier join)
UOM 1──* Product
Supplier 1──* PurchaseOrder 1──* PurchaseOrderDetail *──1 Product
PurchaseOrder 1──* GRN 1──* GRNDetail *──1 Product
GRNDetail 1──* SaleDetail *──1 Sale *──1 POSSession *──1 Counter
GRNDetail 1──* SaleDetail (batch link, NoAction delete)
Counter 1──* Sale | 1──* POSSession
ApplicationUser 1──* POSSession | 1──* Sale (via CashierId string)
GroundBalance 1──* GroundBalanceDetail (FK = BatchNo string)
AuditLog (standalone, no FK)
```

Delete behaviors (must replicate in Prisma):

- `Sale.POSSessionId → POSSession`: **NoAction / Restrict** (legacy fixed a cascade cycle).
- `SaleDetail.ProductId → Product`: **NoAction / Restrict**.
- `SaleDetail.GRNDetailId → GRNDetail`: **NoAction / Restrict**.
- Soft-delete + global filter (`WHERE IsDeleted = false`): `Category, Product, UOM, Counter, Supplier(flag exists)`.
- Hard deletes: `Supplier` (manual cascade, see §5.3), `GroundBalance`, Identity user.
- Everything else: no delete endpoint (status-driven).

### 5.2 Tables — Full Column Spec

Conventions: `Id INT IDENTITY(1,1) PK` unless noted. Money = `decimal(18,2)`. Timestamps below marked `(UTC*)` should be UTC in the rebuild (legacy was server-local `DateTime.Now`).

#### 5.2.1 Identity (ASP.NET Identity — replace with NextAuth tables OR keep compatible)

Legacy: `AspNetUsers (+ ApplicationUser.FullName required)`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims/Logins/Tokens`.
Rebuild option A (recommended): NextAuth Prisma adapter tables (`User, Account, Session, VerificationToken`) + app-level `role` enum on `User`.
Rebuild option B: keep Identity-compatible schema if sharing DB during migration.

`User` (app-level):

| Column | Type | Constraints |
|---|---|---|
| id | string (cuid/uuid) | PK |
| userName | string | unique, required |
| email | string | unique, required |
| fullName | string | required |
| passwordHash | string | required (bcrypt) |
| role | enum `Manager,Inventory,Cashier` | required |
| emailConfirmed | boolean | default true (seed parity) |

#### 5.2.2 Category

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(100) | required |
| ShortName | nvarchar(10) | required, **unique index** — used in ItemCode generation |
| Description | nvarchar(max) | nullable |
| IsDeleted | bit | default false, soft-delete; all queries filter `false` |

#### 5.2.3 UOM (Unit of Measurement)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(50) | required (e.g. Kilogram) |
| ShortName | nvarchar(10) | required (e.g. KG) |
| IsDeleted | bit | default false + filter |

#### 5.2.4 Product

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(150) | required |
| CategoryId | int | FK → Category, nullable in nav (`Category?`) — treat as required in UI |
| UOMId | int | FK → UOM, nullable in nav — treat as required in UI |
| MinStockLevel | int | default 5 |
| CreatedAt | datetime | default now (UTC*) |
| IsDeleted | bit | default false + filter |

Join: `ProductSupplier (ProductId, SupplierId)` composite PK, M2M.

#### 5.2.5 Supplier

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(150) | required |
| ContactPerson | nvarchar(max) | nullable |
| Phone | nvarchar(max) | nullable; multi-numbers stored joined with `", "` |
| Email | nvarchar(max) | nullable; unique-checked case-insensitively on create/edit |
| Address | nvarchar(max) | nullable |
| IsDeleted | bit | flag + filter (but Delete endpoint hard-deletes, see §5.3) |

#### 5.2.6 Counter

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| Name | nvarchar(50) | required (Counter 1/2/3) |
| Location | nvarchar(100) | nullable |
| Status | nvarchar(max) | required, default `Active`; allowed `Active, Inactive, Maintenance` (only `Active` offered to cashiers) |
| AssignedUserId | nvarchar(100) | nullable — **never written by app logic** (keep column for compat, ignore) |
| AssignedUserName | nvarchar(100) | nullable — same |
| Description | nvarchar(255) | nullable |
| CreatedDate | datetime | set on create (UTC*) |
| CreatedByUserId / CreatedByUserName | nvarchar(100) | set on create |
| ModifiedDate | datetime? | set on edit |
| ModifiedByUserId / ModifiedByUserName | nvarchar(100) | set on edit |
| IsDeleted | bit | default false + filter |

#### 5.2.7 PurchaseOrder (header)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| PONumber | nvarchar(50) | required, unique-ish; format `PO-yyyyMMddHHmmss` (create) / `PO-yyyyMMdd-HHmmss` (reorder prefill) |
| SupplierId | int | FK → Supplier, required |
| OrderDate | datetime | required, `Now` on create (UTC*) |
| ExpectedDate | datetime | required, `OrderDate + 7 days` |
| Status | nvarchar(max) | required; `Pending → Partially Received → Received`; `Cancelled` defined in filter dropdown but **no cancel action exists** (decide: implement or drop) |
| TotalAmount | decimal(18,2) | computed `Σ(qty × unitPrice)` |

#### 5.2.8 PurchaseOrderDetail (lines)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| PurchaseOrderId | int | FK → PurchaseOrder, required |
| ProductId | int | FK → Product, required |
| Quantity | int | required, ≥ 1 |
| UnitPrice | decimal(18,2) | required (cost price) |

#### 5.2.9 GRN (header — one per receiving event against a PO)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| GRNNumber | nvarchar(50) | required; format `GRN-yyMMddHHmm` |
| PurchaseOrderId | int | FK → PurchaseOrder, required |
| ReceivedDate | datetime | default now (UTC*) |
| ReceivedByUserId | nvarchar(max) | required (fallback `"System"`) |
| Remarks | nvarchar(max) | nullable (`"-"` if empty in legacy) |

#### 5.2.10 GRNDetail (batch / THE stock unit)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| GRNId | int | FK → GRN, required |
| ProductId | int | FK → Product, required |
| ReceivedQuantity | int | required (qty received in this GRN line) |
| StockQuantity | int | snapshot = ReceivedQuantity at creation |
| CurrentStockQuantity | int | **live balance**; decremented on sale, zeroed on write-off |
| PurchasePrice | decimal(18,2) | copied from PO line |
| SellingPrice | decimal(18,2) | entered on GRN form |
| ItemCode | nvarchar(100) | auto-generated: `{Category.ShortName \| "UNK"}{ProductId}{SupplierId}{yyMMddHHmm}` e.g. `DRY122601231430` |
| ExpiryDate | datetime? | nullable |

Indexes (recommended): `(ProductId)`, `(CurrentStockQuantity)`, `(ExpiryDate)`, `(ItemCode)` unique-ish.

#### 5.2.11 POSSession

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| UserId | nvarchar(100) | required (cashier/owner) |
| UserName | nvarchar(100) | nullable |
| CounterId | int | FK → Counter, required |
| StartTime | datetime | required, default now (UTC*) |
| EndTime | datetime? | set on close |
| Status | nvarchar(max) | required, default `Active`; `Active \| Closed` |
| Notes | nvarchar(255) | nullable |
| OpeningBalance | decimal(18,2) | always `0` in legacy (no UI) — keep or drop |
| ClosingBalance | decimal(18,2) | always `0` — keep or drop |

Uniqueness (app-enforced, add partial unique indexes): one `Active` session per user; one `Active` session per counter.

#### 5.2.12 Sale (header)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| SaleNumber | nvarchar(50) | required; format `SALE-yyyyMMdd-HHmmss` |
| SaleDate | datetime | required, default now (UTC*) |
| POSSessionId | int | FK → POSSession, required, **NoAction** |
| CashierId | nvarchar(100) | required |
| CashierName | nvarchar(100) | nullable |
| CounterId | int | FK → Counter, required |
| Subtotal | decimal(18,2) | `Σ line TotalPrice` |
| Tax | decimal(18,2) | `5% × Subtotal` |
| Total | decimal(18,2) | `Subtotal + Tax` |
| Discount | decimal(18,2) | always `0` (no UI) — keep column, accept future use |
| GrandTotal | decimal(18,2) | `Total − Discount` |
| PaymentMethod | nvarchar(50) | required, default `Cash`; `Cash \| Card \| Mobile Payment` |
| CardNumber | nvarchar(100) | nullable; masked `**** **** **** 1234` for cards, raw reference otherwise |
| Status | nvarchar(max) | default `Pending`; only `Completed` ever created (`Cancelled/Refunded` defined, unused) |
| Notes | nvarchar(255) | nullable (cash tendered/change text or payment reference) |

#### 5.2.13 SaleDetail (lines — batch-specific)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| SaleId | int | FK → Sale, required |
| ProductId | int | FK → Product, required, **NoAction** |
| GRNDetailId | int | FK → GRNDetail (batch), required, **NoAction** |
| Quantity | int | required, ≥ 1 |
| UnitPrice | decimal(18,2) | required (batch selling price) |
| TotalPrice | decimal(18,2) | `qty × unitPrice` |
| Discount | decimal(18,2) | always `0` |
| LineTotal | decimal(18,2) | `TotalPrice − Discount` |

#### 5.2.14 GroundBalance (stock-take header, string PK)

| Column | Type | Constraints |
|---|---|---|
| BatchNo | nvarchar(25) | **PK (string)**; format `GB-yyyyMMddHHmmss` |
| GBDate | datetime | required |
| Status | nvarchar(10) | required; `Pending \| Completed \| Cancelled` |
| Remark | nvarchar(100) | nullable (legacy made nullable in later migration) |
| CreatedDate | datetime | default now (UTC*) |
| CreatedUser | nvarchar(50) | userId |
| UpdatedDate | datetime? | nullable |
| UpdatedUser | nvarchar(50) | nullable/`""` |

#### 5.2.15 GroundBalanceDetail (count lines)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| BatchNo | nvarchar(25) | FK → GroundBalance.BatchNo |
| ItemCode | nvarchar(25) | required — GRN batch ItemCode being counted |
| ActualQty | float | physical count |
| InventoryQty | float | system qty, **re-looked-up live** from `GRNDetail.CurrentStockQuantity` (ignore client-sent value) |
| DifferentQty | float | `ActualQty − InventoryQty` |
| Remark | nvarchar(100) | nullable |
| CreatedDate | datetime | default now (UTC*) |
| CreatedUser | nvarchar(50) | userId |
| UpdatedDate | datetime? | nullable |
| UpdatedUser | nvarchar(50) | nullable |

#### 5.2.16 AuditLog (append-only)

| Column | Type | Constraints |
|---|---|---|
| Id | int | PK |
| EntityType | nvarchar(50) | required (Category, Product, Supplier, UOM, Counter, PurchaseOrder, GRN, Sale, POSSession, GroundBalance, ExpiryManagement) |
| EntityId | nvarchar(max) | required (stringified PK, or BatchNo) |
| Action | nvarchar(20) | required (`Create, Update, Edit, Delete, Soft Delete, Close, WriteOff, Reorder`) |
| UserId | nvarchar(max) | required |
| UserName | nvarchar(100) | set always (nullable in DB) |
| Description | nvarchar(255) | nullable |
| OldValues / NewValues | nvarchar(max) | nullable (JSON/text; only populated on master-data updates) |
| Timestamp | datetime | default now (UTC*) |
| IpAddress | nvarchar(50) | nullable — **never populated in legacy** (populate from request in rebuild) |

> Immutability: no edit/delete API for audit logs. Manager read-only.

#### 5.2.17 Counter (numbering helper — legacy `Counter` table is POS counters; numbering below refers to code-gen)

Number formats are generated in code, not from a DB sequence. For Next.js, either keep code-gen (with unique constraint + retry) or introduce a `Sequence` table. Recommended: add DB unique constraints on `PONumber, GRNNumber, SaleNumber, ItemCode, BatchNo` and retry on collision.

### 5.3 Dangerous Operation — Supplier Delete (must re-confirm)

Legacy `Supplier/Delete` performs a **hard cascade**: deletes `GRNDetails → GRNs → PurchaseOrderDetails → PurchaseOrders → Supplier`. This destroys inventory history. For the rebuild, choose one:

- (Recommended) Convert to soft-delete (`IsDeleted = true`, block if open POs exist), or
- Keep hard delete but require typed confirmation + Manager-only + audit + block if any `CurrentStockQuantity > 0` or sales reference batches.

### 5.4 Prisma Sketch (illustrative, adapt to chosen DB)

```prisma
enum Role { Manager Inventory Cashier }
enum POStatus { Pending PartiallyReceived Received Cancelled }
enum SessionStatus { Active Closed }
enum GBStatus { Pending Completed Cancelled }

model Category { id Int @id @default(autoincrement()) name String @db.NVarChar(100) shortName String @unique @db.NVarChar(10) description String? isDeleted Boolean @default(false) products Product[] }
model UOM { id Int @id @default(autoincrement()) name String @db.NVarChar(50) shortName String @db.NVarChar(10) isDeleted Boolean @default(false) products Product[] }
model Product { id Int @id @default(autoincrement()) name String categoryId Int uomId Int minStockLevel Int @default(5) createdAt DateTime @default(now()) isDeleted Boolean @default(false) category Category @relation(...) uom UOM @relation(...) suppliers Supplier[] ... }
model Supplier { ... products Product[] purchaseOrders PurchaseOrder[] }
// ... PurchaseOrder, PurchaseOrderDetail, GRN, GRNDetail (with currentStockQuantity), POSSession, Sale, SaleDetail (NoAction → @relation(onDelete: NoAction)), GroundBalance(@id BatchNo String), GroundBalanceDetail, Counter, AuditLog
```

---

## 6. Business Rules & Calculations

1. **ItemCode (batch barcode):** `{Category.ShortName or "UNK"}{ProductId}{SupplierId}{yyMMddHHmm}` (legacy `GRNRepository`). Dead code `CM-yyMMddHHmm-XXX` in `ProductRepository.GenerateBarcode()` was never called — do not implement unless required.
2. **Numbering:** `PO-yyyyMMddHHmmss` (reorder prefill `PO-yyyyMMdd-HHmmss`), `GRN-yyMMddHHmm`, `SALE-yyyyMMdd-HHmmss`, `GB-yyyyMMddHHmmss`. Uniqueness via DB constraint + retry.
3. **PO totals:** `TotalAmount = Σ(qty × unitPrice)`. `ExpectedDate = OrderDate + 7 days`.
4. **PO status recompute (after each GRN):** per product `SUM(ReceivedQuantity) >= orderedQty` for all lines → `Received`, else `Partially Received`. Starts `Pending`.
5. **GRN lines:** only lines with `ReceivedQty > 0` are saved. `StockQuantity = CurrentStockQuantity = ReceivedQuantity`. `PurchasePrice` ← PO line; `SellingPrice, ExpiryDate` ← form.
6. **Checkout math:** `Subtotal = Σ(qty × unitPrice)`; `Tax = 5% × Subtotal`; `Total = Subtotal + Tax`; `Discount = 0`; `GrandTotal = Total − Discount`.
7. **Payments:** Cash requires `cashReceived >= Total` (compute change server-side, store text in `Notes`). Card requires `cardNumber` (store masked `**** **** **** LAST4`). Other methods store raw `paymentReference` in `CardNumber` + `Notes`.
8. **Stock decrement:** `GRNDetail.CurrentStockQuantity -= qty` per sale line inside a DB transaction with re-validation (live stock check, row locking). `WriteOff` sets batch stock to `0`.
9. **FEFO + cheapest-price hints:** terminal suggests batches `CurrentStockQuantity > 0 ORDER BY ExpiryDate ASC`; product card price = `MIN(SellingPrice)`; available = `SUM(CurrentStockQuantity)`.
10. **Low stock:** per batch `CurrentStockQuantity <= Product.MinStockLevel` (legacy compares per-batch, not summed — dashboard widget groups/sums; preserve exact semantics or document change).
11. **Expiry buckets:** `Expired: ExpiryDate < Now`; `ExpiringSoon: Now <= ExpiryDate <= Now + threshold` (default 30d; home widget 7d; stock filter 30d window).
12. **Ground balance:** count-only. `DifferentQty = ActualQty − InventoryQty` (live lookup). **No stock write-back.** Edit wipes + re-inserts lines.
13. **Sessions:** one `Active` per user + one `Active` per counter (both enforced). `Opening/ClosingBalance` unused (always 0).
14. **Timestamps:** legacy `DateTime.Now` (server-local). Rebuild: UTC everywhere, display in Asia/Yangon (or viewer TZ).

---

## 7. Project Flows

### 7.1 Auth & Staff Flow

```
Login (AllowAnonymous) → credential check → role redirect → Home dashboard (role-specific widgets)
Manager → Staff list → Create user (username/password/fullName/role) → Edit → Delete (hard delete, confirm)
Logout: if Cashier/any with Active session → auto-close session (Closed + EndTime) + audit + clear cart/session → sign out
```

### 7.2 Catalog Setup Flow (Manager/Inventory)

```
Category (Name, ShortName unique) → UOM (Name, ShortName) → Product (Name, Category, UOM, MinStock=5)
→ Supplier (contact, phones[], email unique-check, address, linked Products M2M via category→product cascade)
→ Counter (Manager only: Name, Location, Status, Description)
All (except Supplier delete) are soft-deletes. Every mutation → audit log.
```

### 7.3 Purchasing (PO) Flow

```
PO Create: pick Supplier → filter Categories by supplier → pick Products → rows (Product, Qty≥1, UnitPrice)
  → PONumber auto → OrderDate=now, Expected=+7d, Status=Pending, Total=Σ → audit Create
PO Index: filters (supplier, orderDate, status, product, category) → Details → [Create GRN] or [GRN History]
```

### 7.4 GRN / Receiving Flow

```
GRN Create (?poId=): load PO lines + already-received map → per line enter ReceivedQty, ExpiryDate?, SellingPrice, Remarks
→ only ReceivedQty>0 saved → GRNNumber auto → transaction: insert GRN + GRNDetails (ItemCode auto) + recompute PO status → audit Create
GRN Index: filters (poNumber contains, receivedDate) → Details (batch lines) → DetailsByPO (1 GRN → redirect details; else history list)
```

### 7.5 Stock & Expiry Flow

```
Stock Index: query GRNDetails+joins → filters (search name/ItemCode, category, lowStock flag, expiringSoon 30d) → group by PO → batch Details
Expiry Index (?daysThreshold=30): batches with Expiry <= now+threshold AND stock>0 → Expired vs Soon badges + counts
  → WriteOff (confirm + reason): stock=0 + audit WriteOff
  → Reorder: prefill new PO (supplier, items, qty, price) → ConfirmReorder creates Pending PO (+7d) + audit Reorder → PO Details
```

### 7.6 Ground Balance (Stock-Take) Flow

```
Create: BatchNo auto (GB-...), date, remark → cascade Supplier→Category→Item (only batches with stock>0) → enter ActualQty per line
  → server re-looks-up InventoryQty live, computes Different → save Pending + audit Create
Index (date/status filters) → Details (Actual/Inventory/Difference) → Edit (wipe+re-insert lines, updatable Remark/Date/Status + audit Edit) → Delete (hard + audit)
Helpers: GetInventoryQty(itemCode), GetSuppliers, GetCategories(supplierId?), GetFilteredItems(supplierId?, categoryId?)
```

### 7.7 POS Sale Flow (Cashier/Manager) — most critical

```
1. SelectCounter: if user has Active session → restore + go Terminal; else list Active counters → POST counterId
   (reject: counter inactive / counter already in use) → create Active session + audit Create → Terminal
2. Terminal: load session + categories + products + per-product {availableStock SUM, startingPrice MIN}
   Search (name/ItemCode, ≤20) → GetProductStock (batches stock>0 ORDER BY Expiry) → AddToCart (validate session, qty, batch match, live stock; merge same batch)
   → UpdateCart / RemoveFromCart / GetCart (subtotal, tax 5%, total, itemCount) / ClearCart
3. CompleteSale (TRANSACTION): re-validate session + each line live stock → SaleNumber auto → compute totals →
   insert Sale (Completed) + SaleDetails → decrement batch stock → clear cart → audit Create → return receipt JSON → print
4. TodaySales: today's sales for cashier + totals
5. EndSession: show SessionReport (session, sales, totalSales, totalAmount, totalItems) → ConfirmEndSession → Closed + EndTime + audit Close + clear session
```

### 7.8 Reporting & Audit Flow (Manager)

```
Sales Report (default today): filters (start, end inclusive, cashier, counter) → sales + totals
Inventory Valuation: group stock>0 by Category → per-category {totalStock, cost value, potential revenue, profit} + grand totals
Session Report: any session → SessionReportViewModel
Audit Log: filters (entityType, action, user, search) → Details (old/new values)
```

---

## 8. API Endpoints Specification

Conventions: `GET` list/detail, `POST` create/action, `PATCH/PUT` update, `DELETE` delete. Auth: `M`=Manager, `I`=Inventory, `C`=Cashier. All mutating endpoints validate with zod + audit. Pagination: `?page=&pageSize=` on all index endpoints. Errors: `400` validation, `401` unauthenticated, `403` wrong role, `404` not found, `409` conflict (duplicate unique / insufficient stock / counter in use).

### 8.1 Auth & Staff — `/api/auth`, `/api/staff`

| Method | Path | Roles | Request | Response | Notes (legacy action) |
|---|---|---|---|---|---|
| POST | `/api/auth/login` | anon | `{userName, password}` | `{user:{id,userName,fullName,role}}` + session cookie | `Account/Login` |
| POST | `/api/auth/logout` | any | — | `{ok:true}` | auto-closes Active session + audit `POSSession/Close`; `Account/Logout` |
| GET | `/api/staff` | M | — | `StaffListViewModel[] {id,fullName,userName,role}` | `Account/Index` (add missing auth) |
| POST | `/api/staff` | M | `{userName,password,fullName,role}` | created user | `Account/CreateUser` |
| GET | `/api/staff/:id` | M | — | `EditUserViewModel {id,fullName,userName,role}` | `Account/EditUser GET` |
| PATCH | `/api/staff/:id` | M | `{fullName,userName,role}` | updated user | `Account/EditUser POST` |
| DELETE | `/api/staff/:id` | M | confirm | `{ok:true}` | hard delete; `Account/DeleteUser` |

### 8.2 Categories — `/api/categories` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/categories` | — | `Category/Index` |
| POST | `/api/categories` | `{name, shortName, description?}` — shortName unique | audit `Create` |
| PATCH | `/api/categories/:id` | same — unique excl. self | audit `Update` (old/new) |
| DELETE | `/api/categories/:id` | — | soft-delete; audit `Soft Delete` |

### 8.3 UOMs — `/api/uoms` (M,I) — same shape as categories

`GET /api/uoms`, `POST /api/uoms {name, shortName}`, `PATCH /api/uoms/:id`, `DELETE /api/uoms/:id` (soft). Audit `Create/Update/Soft Delete`.

### 8.4 Products — `/api/products` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/products` | `?search=&categoryId=` optional | with Category+UOM; `Product/Index` |
| POST | `/api/products` | `{name, categoryId, uomId, minStockLevel=5}` | audit `Create` |
| GET | `/api/products/:id` | — | for edit form |
| PATCH | `/api/products/:id` | `{name, categoryId, uomId, minStockLevel}` | audit `Update` |
| DELETE | `/api/products/:id` | — | soft-delete; audit `Soft Delete` |

### 8.5 Suppliers — `/api/suppliers` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/suppliers` | — | `Supplier/Index` |
| POST | `/api/suppliers` | `{name, contactPerson?, phoneNumbers: string[], email?, address?, productIds: int[]}` — phones joined `", "`, email/phone unique-checked | audit `Create` |
| GET | `/api/suppliers/:id` | — | + selected products |
| PATCH | `/api/suppliers/:id` | same as POST | replace M2M; audit `Update` |
| DELETE | `/api/suppliers/:id` | typed confirm | ⚠️ hard cascade (see §5.3); audit `Delete` |

### 8.6 Purchase Orders — `/api/purchase-orders` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/purchase-orders` | `?supplierId=&orderDate=&status=&productId=&categoryId=` | `PurchaseOrder/Index` |
| POST | `/api/purchase-orders` | `{poNumber?, supplierId, items:[{productId, quantity≥1, unitPrice}]}` — server default number/date/+7d/Total | audit `Create` |
| GET | `/api/purchase-orders/:id` | — | with Supplier+Details+Category; `Details` |
| GET | `/api/purchase-orders/lookups/categories?supplierId=` | — | distinct via M2M |
| GET | `/api/purchase-orders/lookups/products?supplierId=&categoryId=` | — | |
| GET | `/api/purchase-orders/lookups/all-products` | — | |
| GET | `/api/purchase-orders/lookups/products-by-category?categoryId=` | — | |

### 8.7 GRN — `/api/grn` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/grn` | `?poNumber=&receivedDate=` | `GRN/Index` |
| GET | `/api/grn/:id` | — | `GRN/Details` |
| GET | `/api/grn/by-po/:poId` | — | 1 GRN → single; else history; `DetailsByPO` |
| GET | `/api/grn/new?poId=` | — | PO + details + received-qty map; `Create GET` |
| POST | `/api/grn` | `{poId, lines:[{productId, receivedQty, expiryDate?, sellingPrice, remarks?}]}` — only qty>0 saved | transaction + PO status recompute; audit `Create` |

### 8.8 Stock — `/api/stock` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/stock` | `?search=&categoryId=&lowStock=bool&expiringSoon=bool` | grouped by PO (`StockGroupViewModel`); `Stock/Index` |
| GET | `/api/stock/:batchId` | — | single batch + joins; `Stock/Details` |

### 8.9 Expiry — `/api/expiry` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/expiry` | `?daysThreshold=30` | `Expiry <= now+threshold AND stock>0` + expired/soon counts |
| GET | `/api/expiry/:batchId` | — | `Details` |
| POST | `/api/expiry/:batchId/write-off` | `{reason?}` | `CurrentStock=0`; audit `WriteOff` (legacy GET → must be POST) |
| GET | `/api/expiry/:batchId/reorder` | — | prefill PO view data |
| POST | `/api/expiry/reorder` | `{grnDetailId, poNumber, supplierId, items:[{productId,quantity,unitPrice}]}` | creates Pending PO +7d; audit `Reorder` |

### 8.10 Ground Balance — `/api/ground-balance` (M,I)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/ground-balance` | `?gbDate=&status=` | `GroundBalance/Index` |
| POST | `/api/ground-balance` | `{batchNo?, gbDate, remark?, lines:[{itemCode, actualQty}]}` — server re-lookups inventoryQty, computes different | audit `Create` |
| GET | `/api/ground-balance/:batchNo` | — | `Details` |
| PATCH | `/api/ground-balance/:batchNo` | `{gbDate?, remark?, status?, lines:[{itemCode, actualQty}]}` — wipe+re-insert lines | audit `Edit` |
| DELETE | `/api/ground-balance/:batchNo` | confirm | hard delete; audit `Delete` |
| GET | `/api/ground-balance/lookups/inventory-qty?itemCode=` | — | `{inventoryQty}` |
| GET | `/api/ground-balance/lookups/suppliers` | — | `[{value,text}]` |
| GET | `/api/ground-balance/lookups/categories?supplierId=` | — | |
| GET | `/api/ground-balance/lookups/items?supplierId=&categoryId=` | — | `[{value=ItemCode,text}]` stock>0 only |

### 8.11 Counters — `/api/counters` (M)

`GET /api/counters`, `POST /api/counters {name, location?, status, description?}`, `GET /api/counters/:id`, `PATCH /api/counters/:id`, `DELETE /api/counters/:id` (soft). Audit `Create/Edit/Soft Delete`.

### 8.12 POS Sessions — `/api/pos-sessions` (C,M)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/pos-sessions/select` | — | if user Active session → `{restore:true, session}` else `{counters: Active[]}` |
| POST | `/api/pos-sessions/select` | `{counterId}` — reject inactive/in-use | create Active + audit `Create` |
| GET | `/api/pos-sessions/current` | — | resolve session (cookie → fallback latest Active) |
| GET | `/api/pos-sessions/:id/report` | — | `SessionReportViewModel`; `EndSession GET` |
| POST | `/api/pos-sessions/:id/close` | — | `Closed+EndTime`, clear cart/session; audit `Close` |

### 8.13 POS Terminal / Cart / Checkout — `/api/pos` (C,M)

| Method | Path | Request | Response | Notes |
|---|---|---|---|---|
| GET | `/api/pos/bootstrap` | — | `{session, categories, products, stockSummaries:{productId:{stock, price}}}` | `POSTerminal/Index` data |
| POST | `/api/pos/search` | `{searchTerm?, categoryId?}` | products ≤20 with `availableStock, startingPrice` | `SearchProducts` |
| POST | `/api/pos/stock` | `{productId}` | batches stock>0 ORDER BY Expiry (FEFO) | `GetProductStock` |
| POST | `/api/pos/cart` | `{productId, grnDetailId, quantity}` | cart | merge same batch; live-stock check |
| PATCH | `/api/pos/cart` | `{grnDetailId, quantity}` (0 removes) | cart | `UpdateCart` |
| DELETE | `/api/pos/cart/:grnDetailId` | — | cart | `RemoveFromCart` |
| GET | `/api/pos/cart` | — | `{cart, subtotal, tax, total, itemCount}` | `GetCart` |
| DELETE | `/api/pos/cart` | — | `{ok:true}` | `ClearCart` |
| POST | `/api/pos/checkout` | `{paymentMethod, cardNumber?, cashReceived?, paymentReference?}` | receipt JSON `{sale, lines, change?}` | **transaction**; `CompleteSale` |
| GET | `/api/pos/today-sales` | — | today's sales + totals for cashier | `TodaySales` |

Checkout request/response detail:

```jsonc
// POST /api/pos/checkout
{ "paymentMethod": "Cash|Card|Mobile Payment", "cashReceived": 50000, "cardNumber": "4111...", "paymentReference": "..." }
// 200 receipt:
{ "saleId": 12, "saleNumber": "SALE-20250115-001", "subtotal": 10000, "tax": 500, "total": 10500, "discount": 0, "grandTotal": 10500, "change": 39500, "lines": [{"productId":1,"grnDetailId":5,"quantity":2,"unitPrice":5000,"lineTotal":10000}] }
// 409: { "error": "INSUFFICIENT_STOCK", "grnDetailId": 5, "available": 1 }
```

### 8.14 Reports — `/api/reports` (M)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/reports/sales` | `?startDate=&endDate=&cashierId=&counterId=` (default today; end inclusive) | `SalesReport/Index` |
| GET | `/api/reports/inventory-valuation` | — | by-category `{totalStock, costValue, potentialRevenue, profit}` + totals |
| GET | `/api/reports/sessions/:sessionId` | — | `SessionReportViewModel` |
| GET | `/api/reports/stock-movement` | — | ⚠️ legacy view exists with **no action** — define requirements before building |

### 8.15 Audit Logs — `/api/audit-logs` (M)

| Method | Path | Request | Notes |
|---|---|---|---|
| GET | `/api/audit-logs` | `?entityType=&action=&userId=&search=&page=` | `AuditLog/Index` |
| GET | `/api/audit-logs/:id` | — | `AuditLog/Details` |

---

## 9. UI / Page List (App Router)

### 9.1 Auth & Shell

| Route | Roles | Legacy view | Contents |
|---|---|---|---|
| `/login` | anon | `Account/Login` | username+password form, error display, redirect to `/` |
| `/` | all | `Home/Index` | **Role dashboard.** All: low-stock widget (group sums ≤ min), expiring-7d widget. Inventory: + counts. Manager: + GRN/PO/stock qty/value/recent GRNs. Cashier: Active session card or available counters |
| `/privacy` | all | `Home/Privacy` | static |

### 9.2 Staff (Manager)

| Route | Legacy | Contents |
|---|---|---|
| `/staff` | `Account/Index` | table (fullName, userName, role) + New/Edit/Delete |
| `/staff/new` | `Account/CreateUser` | fullName, userName, password, role select |
| `/staff/[id]/edit` | `Account/EditUser` | fullName, userName, role; delete with confirm (hard) |

### 9.3 Master Data (M,I unless noted)

| Route | Legacy | Contents |
|---|---|---|
| `/categories` | `Category/Index` | table + create/edit modals (ShortName unique error handling) |
| `/uoms` | `UOM/Index` | table + create/edit modals |
| `/products` | `Product/Index` + partials | table (name, category, UOM, minStock) + create/edit modals with dropdowns |
| `/suppliers` | `Supplier/Index` + partials | table + create/edit modals: multi-phone dynamic inputs, category→product cascading checkboxes, email/phone uniqueness errors |
| `/counters` (M) | `Counter/*` (5 views) | table + create/edit/details/delete-confirm pages |

### 9.4 Purchasing & GRN (M,I)

| Route | Legacy | Contents |
|---|---|---|
| `/purchase-orders` | `PurchaseOrder/Index` + `_CreatePartial` | filters (supplier/date/status/product/category) + create modal (supplier→category→product cascading rows: product, qty, unitPrice) |
| `/purchase-orders/[id]` | `PurchaseOrder/Details` | header + lines + [Create GRN] + [GRN History] |
| `/grn?poNumber=&receivedDate=` | `GRN/Index` | filterable list (also reused for by-PO history) |
| `/grn/new?poId=` | `GRN/Create` | PO lines with ReceivedQty/ExpiryDate/SellingPrice/Remarks + remaining-qty hints |
| `/grn/[id]` | `GRN/Details` | header + batch lines (ItemCode/expiry/prices) |
| `/grn/by-po/[poId]` | `GRN/DetailsByPO` | history list for one PO |

### 9.5 Stock, Expiry, Ground Balance (M,I)

| Route | Legacy | Contents |
|---|---|---|
| `/stock?search=&categoryId=&lowStock=&expiringSoon=` | `Stock/Index` | grouped-by-PO cards + batch rows; low-stock & 30d-expiry toggles |
| `/stock/[batchId]` | `Stock/Details` | batch + product/category/UOM/GRN/PO/supplier |
| `/expiry?daysThreshold=30` | `ExpiryManagement/Index` | threshold selector, Expired/Soon badges + counts, WriteOff/Reorder/Details buttons |
| `/expiry/[batchId]` | `ExpiryManagement/Details` | batch detail |
| `/expiry/[batchId]/reorder` | `ExpiryManagement/ReorderConfirm` | prefilled PO + editable multi-item rows → confirm |
| `/ground-balance?gbDate=&status=` | `GroundBalance/Index` | filters + BatchNo/Date/Status/ItemCount table |
| `/ground-balance/new` | `GroundBalance/Create` | BatchNo auto, date, remark, supplier→category→item cascade, ActualQty inputs + live inventory lookup |
| `/ground-balance/[batchNo]` | `GroundBalance/Details` | header + Actual/Inventory/Difference table |
| `/ground-balance/[batchNo]/edit` | `GroundBalance/Edit` | same as new + Status dropdown + existing lines |

### 9.6 POS (C,M)

| Route | Legacy | Contents |
|---|---|---|
| `/pos/select-counter` | `POSSession/SelectCounter` | Active-counter cards → select; auto-restore if already Active |
| `/pos/terminal` | `POSTerminal/Index` | category tabs, product search, batch selector (FEFO), cart panel (qty edit/remove), cash/card checkout modal, receipt print |
| `/pos/today-sales` | `POSTerminal/TodaySales` | today's sales table + totals (current cashier) |
| `/pos/end-session` | `POSSession/EndSession` | session summary (sales/items/amount) + Confirm close |

### 9.7 Reports & Audit (M)

| Route | Legacy | Contents |
|---|---|---|
| `/reports/sales?startDate=&endDate=&cashierId=&counterId=` | `SalesReport/Index` | filters + totals + sales table |
| `/reports/inventory-valuation` | `SalesReport/InventoryValuation` | by-category stock/value/revenue/profit + grand totals |
| `/reports/sessions/[sessionId]` | `SalesReport/SessionReport` | session report view |
| `/reports/stock-movement` | `SalesReport/StockMovement.cshtml` | ⚠️ orphan view — needs new spec |
| `/audit-logs?entityType=&action=&userId=&search=` | `AuditLog/Index` | filterable log table |
| `/audit-logs/[id]` | `AuditLog/Details` | full log incl. old/new values |

Total: **~35 routes + ~30 API groups.** Every table needs: search/filter, pagination, empty state, loading state, role-gated buttons.

---

## 10. Validation Rules

- `Category/UOM`: name required; shortName required + unique (edit excludes self).
- `Product`: name required; categoryId/uomId required; minStockLevel ≥ 0 (default 5).
- `Supplier`: name required; email format if given, unique case-insensitive; phones unique; at least zero products allowed; productIds must exist + not soft-deleted.
- `PO create`: supplierId required + exists; ≥1 item; each `productId` exists, `quantity ≥ 1`, `unitPrice ≥ 0`.
- `GRN create`: poId exists; arrays aligned; at least one `receivedQty > 0`; `sellingPrice ≥ 0`; expiry optional date.
- `Cart`: quantity ≥ 1; batch must belong to product; qty ≤ live `CurrentStockQuantity` (re-checked at checkout inside transaction).
- `Checkout`: Cash → `cashReceived >= Total`; Card → `cardNumber` required; all → Active session required.
- `GroundBalance`: ≥1 line; `itemCode` must exist; `actualQty ≥ 0`; server ignores client inventoryQty.
- `Session select`: counter must be `Active` + have no other Active session.
- `Staff`: userName unique; password policy (align with Identity default or define: min 6 + upper/lower/digit/special); role must be one of 3.

---

## 11. Audit Logging Specification

Helper: `audit(entityType, entityId, action, description?, oldValues?, newValues?)` — called server-side with current user + IP.

| Trigger | EntityType | Action |
|---|---|---|
| Category/Product/Supplier/UOM/Counter create | each | `Create` |
| Category/Product/Supplier/UOM/Counter edit | each | `Update`/`Edit` (include old/new JSON) |
| Category/Product/UOM/Counter delete | each | `Soft Delete` |
| Supplier delete | `Supplier` | `Delete` |
| PO create / reorder-create | `PurchaseOrder` | `Create` / `Reorder` |
| GRN confirm | `GRN` | `Create` |
| Sale checkout | `Sale` | `Create` |
| Session open / close (incl. auto-close on logout) | `POSSession` | `Create` / `Close` |
| GroundBalance create / edit / delete | `GroundBalance` | `Create` / `Edit` / `Delete` |
| Expiry write-off | `ExpiryManagement` | `WriteOff` |

Rebuild improvements: populate `IpAddress` from request (legacy always null); unify `Update` vs `Edit` to one verb; make audit writes part of the same DB transaction as the mutation where possible.

---

## 12. Session, Cart & Counter Rules

- Legacy `ISession` (8h, HttpOnly): keys `POSSessionId:int`, `CounterId:int`, `Cart: CartItem[]` (JSON). Cleared on session close/logout; cart cleared on `ClearCart`/`CompleteSale`.
- `CartItem = {productId, grnDetailId, productName, quantity, unitPrice, totalPrice}`.
- `GetCart` math: `subtotal=Σ total`, `tax=5%`, `total=sub+tax`, `itemCount=Σ qty`.
- Next.js: replace in-memory session with persisted store (DB `Cart` table per session/user or Redis) so multi-tab/multi-device and serverless deployments work. Keep the same merge-by-batch semantics and server-side stock re-validation.
- Counter exclusivity: `Active` counter with an `Active` session cannot be selected by another user. `AssignedUserId/Name` columns are dead — either wire them up or drop them.

---

## 13. Reports Specification

1. **Sales report** (`/reports/sales`): date range (default today; end inclusive `+1day−1tick`), optional cashier + counter; returns sales list + `{totalSales, totalAmount, totalItems}`.
2. **Session report** (end-session + `/reports/sessions/:id`): `{session, sales, totalSales, totalAmount, totalItems}`.
3. **Inventory valuation** (`/reports/inventory-valuation`): over batches with `CurrentStockQuantity > 0`, grouped by Category: `{totalStock, costValue (purchase), potentialRevenue (selling), profit}` + grand totals.
4. **Low stock** (dashboard + `/stock?lowStock=true`): batches where `CurrentStockQuantity <= Product.MinStockLevel`; `LowStockViewModel {productName, totalCurrentStock, minStockLevel}`.
5. **Expiry counts** (dashboard + `/expiry`): expired vs expiring-soon counts per threshold.
6. **Today sales** (cashier): `SaleDate >= today AND < tomorrow` for current cashier.

---

## 14. Seed Data

1. Roles: `Manager, Inventory, Cashier`.
2. Users: `manager1/Manager@123`, `inventory1/Inventory@123`, `cashier1/Cashier@123` (+ full names/emails per §2).
3. Counters (if empty): `Counter 1/Main Entrance`, `Counter 2/Back Entrance`, `Counter 3/Express Lane` — all `Active`, no assignee, `CreatedBy=System`.
4. Prisma `seed.ts` must be idempotent (upserts).

---

## 15. Migration Notes (MVC → Next.js)

1. **Routeless → routed:** legacy uses conventional MVC routes; Next.js needs explicit `app/` routes (§9) + `middleware.ts` role guards. Fix the two unauthenticated legacy actions with default-deny.
2. **Razor partials/modals → client components:** `_CreatePartial/_EditPartial` + inline modals become `react-hook-form` dialogs; cascading dropdowns (supplier→category→product) become chained `useQuery` calls to `/lookups/*`.
3. **Dynamic JS rows → typed cart/line editors:** PO multi-item form, GRN lines, ground-balance lines, supplier multi-phones all become controlled field arrays with zod validation.
4. **Session cart → persisted cart:** see §12.
5. **GET-mutations → proper verbs:** `Expiry/WriteOff` (legacy GET) must be `POST /api/expiry/:id/write-off` with CSRF protection.
6. **Local time → UTC:** migrate existing `datetime` data with a documented offset (server TZ) or freeze display TZ.
7. **Decimal precision:** all money columns `decimal(18,2)`; JS money math in integer minor units or `decimal.js`, round tax to 2dp.
8. **NoAction FKs:** Prisma `onDelete: NoAction` for the three Sale relations; handle orphans in app code.
9. **Orphan view:** `StockMovement.cshtml` has no controller action — do not port blindly; gather requirements first.
10. **Dead code:** `ProductRepository.GenerateBarcode()` (CM-... format) is uncalled; port only the live `ItemCode` rule (§6.1) unless barcode printing is re-scoped.

---

## 16. Implementation Phases

| Phase | Scope | Exit criteria |
|---|---|---|
| 0 | Repo + Prisma schema + seed + auth + middleware guards | login/logout + role redirects work; seeds idempotent |
| 1 | Master data (Category, UOM, Product, Supplier, Counter) + audit viewer | CRUD + soft-delete + audit entries verified |
| 2 | PO + GRN + Stock views | PO→GRN→stock-increase round-trip; status recompute tested |
| 3 | Expiry + Reorder + Ground Balance | write-off zeroes stock; reorder creates PO; stock-take differences correct |
| 4 | POS (counter→session→terminal→cart→checkout→close) | concurrent-counter exclusion, FEFO, transaction rollback on low stock, receipt print |
| 5 | Reports (sales, session, valuation, today-sales, dashboard widgets) | numbers reconcile with POS transactions |
| 6 | Hardening: UTC, money rounding, pagination, rate-limit login, IP audit, tests (Vitest + Playwright: checkout tx, stock race, RBAC matrix) | gate pass, UAT with seed accounts |

---

*End of PROJECT_PLAN.md — approved as the build contract for the Next.js rewrite.*
