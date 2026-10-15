# P-CRM — Enterprise Customer Relationship Management System

[![Build and Test](https://github.com/Premsai5577/p-crm/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/Premsai5577/p-crm/actions)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Identity](https://img.shields.io/badge/Security-ASP.NET%20Identity-success.svg)](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
[![EF Core](https://img.shields.io/badge/ORM-Entity%20Framework%20Core%208-purple.svg)](https://learn.microsoft.com/ef/core/)
[![Chart.js](https://img.shields.io/badge/Visuals-Chart.js-orange.svg)](https://www.chartjs.org/)

**P-CRM** is a role-based, production-ready enterprise CRM web application designed to manage the entire sales lifecycle—from lead capture and qualification to customer onboarding, opportunity pipeline forecasting, follow-up scheduling, and immutable audit compliance.

---

## 🌟 Key Functional Modules

### 1. Application Shell & Navigation (`P-CRM`)
- Responsive Bootstrap 5 modern UI with role-aware navigation bar, user status badges, action toasts, and confirmation dialogs.
- Protection against unauthorized and anonymous access to protected CRM routes.

### 2. Authentication & Authorization
- Built with **ASP.NET Core Identity** (no plaintext passwords, PBKDF2 adaptive password hashing).
- **Password Policy**: Minimum 8 characters requiring uppercase, lowercase, numbers, and special characters.
- **Account Lockout**: Automatic 15-minute account lockout triggered after 5 consecutive failed login attempts.
- **CSRF Protection**: All state-changing MVC POST requests are strictly protected with `[ValidateAntiForgeryToken]`.
- **Role-Based Authorization (RBAC)**:
  - **Admin**: Full access across all CRM records, User Administration, and Audit Trails.
  - **Manager**: Access to team-wide records, sales pipeline, and business intelligence reports.
  - **SalesExecutive**: Strict row-level ownership—accesses only assigned customers, leads, opportunities, follow-ups, and activities.

### 3. Executive Dashboard & Visual Analytics (Chart.js)
- **8 Mandatory Role-Scoped KPI Cards**:
  1. *Total Customers*
  2. *Total Leads*
  3. *Open Leads* (active, non-lost/converted)
  4. *Total Opportunities*
  5. *Open Opportunities*
  6. *Won Opportunities*
  7. *Lost Opportunities*
  8. *Total Pipeline Value & Weighted Pipeline Value*
- **3 Dynamic Chart.js Visualizations**:
  - **Lead Status Doughnut Chart**: Distribution across `New`, `Contacted`, `Qualified`, `Lost`, `Converted`.
  - **Opportunity Pipeline Bar Chart**: Value breakdown by stage (`Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`).
  - **Monthly Closed-Won Line Chart**: Historical 6-month revenue performance.
- Real-time widgets for **Upcoming Follow-Ups** and **Recent Activities**.

### 4. Customer Master Data Management
- Complete CRUD operations with search and multi-parameter filtering (Name, Email, Phone, Company, Status).
- Strict email and 10-digit phone format validation.
- Server-side unique constraints preventing duplicate customer records.
- Comprehensive customer profile showing linked deals, leads, follow-ups, and historical activity logs.

### 5. Lead Management & Conversion Workflow
- Capture prospective clients with sources (`Website`, `Referral`, `Cold Call`, `Social Media`, `Partner`).
- Status progression: `New` &rarr; `Contacted` &rarr; `Qualified` &rarr; `Converted` / `Lost`.
- **Lead-to-Customer Conversion**: One-click qualification workflow that automatically creates a master customer entity, creates an associated sales deal opportunity, and updates the lead status to *Converted*.

### 6. Opportunity & Pipeline Management
- Pipeline stages: `Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`.
- Automatic weighted pipeline calculation: `Amount × Probability / 100`.
- **Business Validations**:
  - Active opportunity amounts must be strictly greater than 0 (`Amount > 0`).
  - Probability must be between 0 and 100 inclusive (`0 ≤ Probability ≤ 100`).
  - Expected close date cannot be in the past for active opportunities.

### 7. Follow-Up Scheduling & Task Management
- Schedule touchpoints: `Call`, `Meeting`, `Email`, `Task`.
- Status tracking: `Planned`, `Completed`, `Missed`, `Cancelled`.
- **Business Rule**: Follow-up date cannot be in the past for new or planned activities.
- Quick action to mark activities as completed with resolution notes.

### 8. User & Role Administration (Admin Only)
- User listing with account status, lockout indicator, and failed login attempt counters.
- Ability to create users, reassign roles (`Admin`, `Manager`, `SalesExecutive`), toggle active/inactive status, and unlock locked accounts.
- Administrative password reset flow.

### 9. Compliance Audit Trail
- Append-only, tamper-proof activity logging for all security and business events:
  - `Login`, `Failed Login`, `Logout`, `Lockout`, `Create`, `Update`, `Delete`, `Convert`, `Role Change`, `Security`.
- Filterable by User, Entity/Module, Action type, and Date Range.

### 10. Secured REST API & Swagger UI
- REST conventions with data transfer objects (DTOs) and standardized HTTP status codes (`200 OK`, `201 Created`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found`, `409 Conflict`, `423 Locked`).
- **Interactive OpenAPI/Swagger UI** accessible directly at `/swagger`.
- Endpoints:
  - `POST /api/auth/login`
  - `POST /api/auth/logout`
  - `GET /api/customers` | `POST /api/customers`
  - `GET /api/customers/{id}` | `PUT /api/customers/{id}` | `DELETE /api/customers/{id}`
  - `GET /api/leads` | `POST /api/leads`
  - `GET /api/opportunities` | `POST /api/opportunities`
  - `GET /api/followups` | `POST /api/followups`
  - `GET /api/reports/pipeline`

### 11. Reporting & Analytics Suite
- Dedicated reporting engine with multi-criteria date and user filtering:
  - *Pipeline Report*
  - *Conversion Rate Report*
  - *Customer Directory Report*
  - *Lead Source Report*
  - *Follow-Up Activity Report*
  - *Opportunity Outcome Report*
  - *User Performance Report*
  - *Audit Compliance Report*
- 1-click printable and exportable format.

---

## 🔑 Default Test Accounts

| Role | Username / Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@123` | Full application administration, user management, audit logs, all records |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123` | Team-wide customer, lead, deal, follow-up access, pipeline reports |
| **Sales Executive** | `sales@acxiomcrm.com` | `Sales@123` | Assigned records, personal dashboard, personal follow-ups |
| **Sales Executive** | `john@acxiomcrm.com` | `Sales@123` | Assigned records, personal dashboard |

---

## 🚀 Running the Project Locally

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Steps
1. Clone the repository:
   ```bash
   git clone https://github.com/Premsai5577/p-crm.git
   cd p-crm
   ```
2. Build the solution:
   ```bash
   dotnet build
   ```
3. Run the automated business test suite:
   ```bash
   dotnet test
   ```
4. Run the application:
   ```bash
   dotnet run
   ```
5. Open your browser and navigate to:
   - Web Application: **`https://localhost:5001`** (or `http://localhost:5247`)
   - Swagger REST API Docs: **`https://localhost:5001/swagger`**

---

## 🐳 Docker Deployment

The application includes a production-ready, multi-stage `Dockerfile`.

1. **Build Docker image**:
   ```bash
   docker build -t p-crm:latest .
   ```
2. **Run container**:
   ```bash
   docker run -d -p 8080:8080 --name p-crm-app p-crm:latest
   ```
3. Access at **`http://localhost:8080`**.

---

## ☁️ Cloud Deployment (Render / Railway / Azure)

1. Connect this repository to your cloud provider (e.g. [Render](https://render.com), [Railway](https://railway.app), or [Azure App Service](https://azure.microsoft.com/services/app-service/)).
2. Select **Docker** or **.NET 8** environment.
3. Build command: `dotnet publish -c Release -o out`
4. Start command: `dotnet out/p-crm.dll`

---

## 🧪 Testing & Verification Checklist

| Area | Tested Behavior | Status |
| :--- | :--- | :---: |
| **Authentication** | Valid credentials work; invalid return 401; logout safely terminates session | ✅ Passed |
| **Password Policy** | Minimum 8 chars with upper, lower, digits, symbols required | ✅ Passed |
| **Lockout Protection** | Repeated failed logins lock account for 15 minutes; admin can unlock | ✅ Passed |
| **Client Validation** | jQuery Unobtrusive validation blocks invalid phone, email, and required fields | ✅ Passed |
| **Server Validation** | Crafted invalid requests are rejected on server with proper status codes | ✅ Passed |
| **Opportunity Rules** | Amount <= 0 and Probability not in 0–100 rejected; past date rejected | ✅ Passed |
| **Follow-Up Rules** | Date earlier than today rejected for planned follow-up | ✅ Passed |
| **RBAC Authorization** | SalesExecutive restricted to assigned data; Admin accesses all & audit logs | ✅ Passed |
| **REST APIs** | `/api/customers`, `/api/leads`, `/api/opportunities`, `/api/reports/pipeline` functional | ✅ Passed |
| **Unit Test Suite** | 9 xUnit business validation test cases automated and passing | ✅ Passed |
