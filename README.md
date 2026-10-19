
# METS - Stock Replenishment Request System

A manufacturing execution and tracking system (METS) feature for managing stock replenishment requests on the production floor.

## Overview

Production line workers can request material replenishment when stock runs low at their station. Requests follow an approval workflow:

```
Draft → Submitted → Approved → Fulfilled
                  ↘ Rejected
```

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core (.NET 10), Controller-based |
| Data Access | Entity Framework Core |
| Database | SQLite In-Memory |
| Frontend | Blazor Server + MudBlazor |
| Testing | NUnit + NSubstitute |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run

```bash
# Build the entire solution
dotnet build

# Run the web application
dotnet run --project src/METS.StockReplenishment.Web

# Run tests
dotnet test
```

The application will start at `https://localhost:7210` (or `http://localhost:5210`).

### Seed Data

The application comes pre-loaded with sample data across all workflow states:
- **2 Draft** requests (ready to submit)
- **3 Submitted** requests (ready to approve/reject)
- **1 Approved** request (ready to fulfill)
- **1 Rejected** request
- **1 Fulfilled** request

## Architecture

```
METS.StockReplenishment.sln
├── src/
│   ├── METS.StockReplenishment.Core/          # Domain models, DTOs, interfaces
│   ├── METS.StockReplenishment.Infrastructure/ # EF Core, repositories, services
│   └── METS.StockReplenishment.Web/           # API controllers + Blazor UI
└── tests/
    └── METS.StockReplenishment.Tests/         # Unit tests
```

### Core Features

1. **CRUD Operations** — Create, read, update replenishment requests with line items
2. **Workflow Transitions** — Submit, approve, reject, and fulfill requests with business rule validation
3. **Async Stock Validation** — Simulated external stock check runs in the background (2-5 seconds) without blocking the UI. The client polls for results.
4. **Filtering & Pagination** — Browse requests filtered by status, priority, location, and search term
5. **MudBlazor UI** — Clean, functional interface with tables, forms, dialogs, and status chips

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/replenishmentrequests` | List requests (with filters & pagination) |
| GET | `/api/replenishmentrequests/{id}` | Get single request |
| POST | `/api/replenishmentrequests` | Create new draft request |
| PUT | `/api/replenishmentrequests/{id}` | Update a draft request |
| POST | `/api/replenishmentrequests/{id}/submit` | Submit for approval |
| POST | `/api/replenishmentrequests/{id}/approve` | Approve a submitted request |
| POST | `/api/replenishmentrequests/{id}/reject` | Reject a submitted request |
| POST | `/api/replenishmentrequests/{id}/fulfill` | Mark approved request as fulfilled |
| GET | `/api/replenishmentrequests/{id}/stock-validation` | Check stock validation status |
| GET | `/api/replenishmentrequests/locations` | Get distinct stock locations |

### External Stock Validation

When a request is submitted, an asynchronous stock validation is queued via a background service using `System.Threading.Channels`. The validation:
- Simulates a slow external service (2-5 second delay)
- Runs without blocking the API or UI
- Results are polled by the Blazor UI every 2 seconds
- Returns one of: Available, PartiallyAvailable, or Unavailable
=======
# METS-StockReplenishment
