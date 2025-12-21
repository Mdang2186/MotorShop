# MotorShop — Motorcycle E-Commerce Web App

MotorShop is a full-stack **online motorcycle store** built with **ASP.NET Core MVC**. It provides a customer storefront (browse → cart → checkout) and an **Admin dashboard** for monitoring key metrics.

**Repository:** `https://github.com/Mdang2186/MotorShop`

---

## Highlights
- Customer storefront: product listing, product detail, cart, checkout, account, orders
- Admin: **Dashboard only** (KPIs + charts)
- Database: SQL Server + EF Core migrations
- Clean project structure: MVC + Identity-based authentication

---

## Built With (popular tech)
![.NET](https://img.shields.io/badge/.NET-9-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF%20Core-ORM-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-Database-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)

---

## Features

### Customer (Storefront)
- Browse products by **category**, **search**, **sort**, and **pagination**
- Product detail page with **images**, description, specifications, and stock
- Shopping cart: add items, update quantity, remove items
- Checkout with shipping information (default: **Cash on Delivery / COD**)
- Account: register, login, profile
- Orders: order history and order details

### Admin (Dashboard Only)
- Secure admin login
- Dashboard KPIs and charts (e.g., orders/revenue trend, top products — depending on data)

### Optional Pages (if included in your build)
Some teams extend the storefront with extra pages. If your current build includes them, you may also see:
- **AI Recommendations** — suggest relevant products for customers
- **Customer Care Chat** — support chat page for guidance/FAQs
- **Store Locator Map** — show store locations and directions

---

## Roles (Access Control)
MotorShop uses **two roles**:
- **Customer**
- **Admin**

---

## Quick Start (Newcomer Guide)

### 1) Requirements
- **.NET SDK** (match the project target framework)
- **SQL Server** (LocalDB / Express / Developer)
- **Visual Studio 2022** (recommended) or Rider

### 2) Clone the repository
```bash
git clone https://github.com/Mdang2186/MotorShop.git
cd MotorShop
```

### 3) Open the solution (recommended)
- Open `MotorShop.sln` in Visual Studio
- Set the `MotorShop` project as **Startup Project**

### 4) Configure database connection
Edit: `MotorShop/appsettings.json`

LocalDB example:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MotorShopDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

SQL Server (username/password) example:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MotorShopDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 5) Restore dependencies
```bash
dotnet restore
```

### 6) Create the database (EF Core migrations)

Install EF tools (one-time):
```bash
dotnet tool install --global dotnet-ef
```

**Option A — If migrations already exist**
```bash
dotnet ef database update --project MotorShop/MotorShop.csproj --startup-project MotorShop/MotorShop.csproj
```

**Option B — If you need to create the first migration**
```bash
dotnet ef migrations add InitialCreate --project MotorShop/MotorShop.csproj --startup-project MotorShop/MotorShop.csproj
dotnet ef database update --project MotorShop/MotorShop.csproj --startup-project MotorShop/MotorShop.csproj
```

**Option C — SQL script**
- Open `MotorShopDb.sql` in SSMS and execute it (schema/data depends on script contents)

### 7) Run the application
**CLI**
```bash
dotnet run --project MotorShop/MotorShop.csproj
```

**Visual Studio**
- Press **F5**

Open:
- `https://localhost:<port>/`

---

## Admin Dashboard Access
1. Register a normal account (Customer).
2. Assign the **Admin** role to that account (via your seed logic or directly in DB).
3. Open the admin dashboard route (commonly `/Admin`).

---

## Project Structure 
```txt
MotorShop/
├─ MotorShop.sln
├─ MotorShopDb.sql
├─ docs/screenshots/
└─ MotorShop/                    # ASP.NET Core MVC project
   ├─ Areas/Admin/               # Admin dashboard area (if used)
   ├─ Controllers/
   ├─ Data/                      # DbContext, Migrations
   ├─ Models/                    # Entities, Identity, ViewModels
   ├─ Views/                     # Razor views
   ├─ wwwroot/                   # CSS/JS/images
   ├─ appsettings.json
   └─ Program.cs
```

---

## Screenshots  

Place screenshots in: `docs/screenshots/`   

<p align="center">
  <img src="docs/screenshots/01-home.png" width="780" alt="Customer — Home / Product Listing">
</p>
<p align="center">
  <img src="docs/screenshots/02-product-detail.png" width="780" alt="Customer — Product Detail">
</p>
<p align="center">
  <img src="docs/screenshots/03-cart.png" width="780" alt="Customer — Cart">
</p>
<p align="center">
  <img src="docs/screenshots/04-checkout.png" width="780" alt="Customer — Checkout">
</p>
<p align="center">
  <img src="docs/screenshots/05-admin-dashboard.png" width="780" alt="Admin — Dashboard">
</p>

<p align="center">
  <img src="docs/screenshots/08-ai-recommendations.png" width="780" alt="Customer — AI Recommendations (optional)">
</p>
<p align="center">
  <img src="docs/screenshots/09-customer-chat.png" width="780" alt="Customer — Customer Care Chat (optional)">
</p>
<p align="center">
  <img src="docs/screenshots/10-map.png" width="780" alt="Customer — Store Locator Map (optional)">
</p>

---

## Team
- **Do Cong Minh** — Team Lead, UI/UX  
- **Dang Dinh The Hieu** — Backend, Database
