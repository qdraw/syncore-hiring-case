# Cubing API

An ASP.NET Core (.NET 10) API for **Super Nice Cubes**, who sell only cubic articles. Given a quantity of
articles, an article size and a box size, it works out how many boxes are needed, assigns articles to each
box, gives every box a unique license plate number (LPN), and stores the result in SQLite.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

No other tooling is needed. SQLite is embedded and the database file is created automatically.

## Running

```bash
dotnet run --project src/Cubing.Api
```

The API listens on `http://localhost:5257` (see `src/Cubing.Api/Properties/launchSettings.json`).

| URL | What |
| --- | --- |
| `http://localhost:5257/` | Redirects to the Scalar UI |
| `http://localhost:5257/scalar` | Interactive API documentation (Scalar) |
| `http://localhost:5257/openapi/v1.json` | OpenAPI document |

### Database

- The connection string is `ConnectionStrings:Cubing` in `appsettings.json`. If it is missing, the app uses
  `Data Source=cubing.db`, a file relative to the working directory.
- The schema is created at startup with `EnsureCreated()`. There are no migrations.
- **If the schema changes, delete `cubing.db`.** `EnsureCreated()` does nothing when the file already
  exists, so an old file with outdated tables will cause errors.

### Tests

```bash
dotnet test Cubing.slnx
```

## API

### `POST /api/cubing`

Calculates the boxes, stores the calculation, and returns **201 Created** with a `Location` header.

```json
{
  "customerId": "SuperNiceCubes",
  "quantity": 28,
  "articleName": "Article1",
  "articleDimensions": { "length": 10, "width": 10, "height": 10 },
  "boxTypeName": "Box",
  "boxDimensions": { "length": 35, "width": 35, "height": 35 }
}
```

Response:

```json
{
  "calculationId": "e8d7260e-759d-40b4-a224-1102f3412a83",
  "customerId": "SuperNiceCubes",
  "articleName": "Article1",
  "boxTypeName": "Box",
  "totalQuantity": 28,
  "totalBoxes": 2,
  "boxes": [
    { "licensePlateNumber": "9f971ba3-0ca4-43b7-97eb-85c460128085", "numberOfItems": 27 },
    { "licensePlateNumber": "f29777a9-3d14-43fd-8b14-20f404be6992", "numberOfItems": 1 }
  ]
}
```

`customerId` selects the customer's packing rules and LPN format. It is optional and defaults to
`Cubing:DefaultCustomerId` (`SuperNiceCubes`), so the example payload from the brief works unchanged. Ids are
case-insensitive.

Invalid requests return **400** with a standard validation problem (`errors` keyed by field). Nothing is
stored and no LPNs are generated. A request is rejected when:

- quantity is not greater than zero,
- the article or box name is missing,
- any dimension is not greater than zero,
- the customer id is unknown,
- the article is rejected by the customer's packing strategy (for Super Nice Cubes: not cubic, so length, width
  and height must be equal),
- the article does not fit inside the box,
- the request needs more than 10,000 boxes.

### `GET /api/cubing/{id}`

Returns a stored calculation in the same shape as above, or **404** if it does not exist.

## Solution layout

```
Cubing.slnx
Directory.Build.props              # shared TargetFramework, nullable, implicit usings, warnings as errors
src/
  Cubing.Core/                     # business logic, no ASP.NET or EF Core dependencies
    Entities/                      #   persisted entities (CubingCalculation, CalculatedBox)
    Packing/
      Interfaces/                  #   IPackingStrategy, ILpnGenerator, ICubingService
      Models/                      #   Dimensions, CubingRequest, CubingResult, PackedBox
      Strategies/                  #   CubePackingStrategy, GuidLpnGenerator
      Services/                    #   CubingService
      Exceptions/                  #   CubingValidationException
    Repositories/                  #   ICubingCalculationRepository
  Cubing.Infrastructure/           # EF Core + SQLite
    Data/                          #   CubingDbContext
    Repositories/                  #   CubingCalculationRepository
    DependencyInjection.cs         #   AddInfrastructure()
  Cubing.Api/                      # web host
    Controllers/                   #   CubingController, HomeController
    Contracts/                     #   request/response models
    Extensions/                    #   AddCubing()
    Program.cs
tests/
  Cubing.Core.Tests/               # unit tests (xUnit + NSubstitute), mirrors Core's folders
  Cubing.Api.Tests/                # integration tests via WebApplicationFactory + in-memory SQLite
```

Dependencies point inwards: `Api` -> `Infrastructure` -> `Core`, and `Api` -> `Core`. `Core` references
nothing.

## Architecture decisions

**Layered / clean architecture.** Follows Microsoft's recommended layout for ASP.NET Core applications:
domain and business logic in `Core`, persistence in `Infrastructure`, HTTP in `Api`. The packing logic can be
used and tested without a web host or database.

**Strategy pattern for the customer-specific rules.** Two extension points, each behind an interface:

- `IPackingStrategy` decides how many articles fit in one box. `CubePackingStrategy` computes
  `floor(box/edge)` per axis and multiplies the three. Because articles are cubes, rotation does not matter.
  It is deliberately not a general 3D bin-packing solver.
- `ILpnGenerator` produces LPNs as strings, so a future customer can use prefixed or sequential formats.
  `GuidLpnGenerator` uses `Guid.NewGuid()`.

**Customer profiles.** A `CustomerProfile` bundles one `IPackingStrategy` and one `ILpnGenerator`.
`ICustomerProfileResolver` (implemented by `CustomerProfileRegistry`, a case-insensitive dictionary in `Core`)
maps the request's `customerId` to its profile, and `CubingService` uses that profile for the whole request. An
unknown customer is a validation error. Profiles are registered in `AddCubing()` (`Api/Extensions`), which
accepts a callback to add more. The registry has no DI dependency, so it is trivial to test.

**Customer rules live in the strategy.** `CubingService` only checks things every customer needs (quantity,
names, positive dimensions). Whether an article is acceptable at all is `IPackingStrategy.ValidateArticle`;
the "article must be cubic" rule belongs to `CubePackingStrategy`, not the shared service.

**`CubingService` orchestrates.** It validates, asks the strategy for the capacity, computes the box count,
generates one LPN per box and distributes the articles. Dependencies arrive through the constructor, so the
service is tested with mocked strategies and generators.

**Validation is business logic, not HTTP.** The service throws `CubingValidationException` with errors keyed
by field. The controller converts it to a 400 validation problem. All input is validated before any LPN is
generated, so an invalid request never creates boxes.

**LPN uniqueness is enforced twice.** The service keeps a set of LPNs used in the current response and
retries a duplicate up to 10 times per box before failing with `InvalidOperationException`. The database
also has a unique index on the LPN column.

**Overflow safety.** Per-axis counts are `long`, and their product saturates at `long.MaxValue` instead of
wrapping (a `b <= long.MaxValue / a` check, no exceptions). The box count uses ceiling division on `long` values, so no intermediate result overflows.

**Box limit.** A request needing more than 10,000 boxes (`MaxBoxes` in `CubingService`) is rejected with a
validation error. Without it, a request like 2 billion articles with a capacity of 1 would try to allocate
2 billion boxes.

**Repository for persistence.** `ICubingCalculationRepository` lives in `Core` and is implemented with EF Core
in `Infrastructure`, so the controller does not use `DbContext` directly. `AddAsync` saves immediately.
There is no separate unit-of-work abstraction because each request writes one aggregate.

**Controllers, classes and conventions.**

- The API uses MVC controllers with `[ApiController]`, which gives automatic 400 responses for malformed
  bodies.
- Domain models and API contracts are immutable classes in `Core` and plain classes in `Api`, not records.
- API contracts are separate from domain models and entities, so the HTTP shape can change independently.
- A missing dimensions object in the request becomes 0x0x0 and is rejected by the service's validation.

**SQLite with `EnsureCreated()`.** Chosen for zero-setup. If the project grows, switch to EF Core migrations.

**Testing approach.**

- Unit tests (`Cubing.Core.Tests`) cover the packing strategy and the service, including the scenarios from
  the requirements, LPN generation (one call per box, duplicates retried, uniqueness) and every validation
  rule. NSubstitute mocks the strategy and LPN generator.
- Integration tests (`Cubing.Api.Tests`) run the real API through `WebApplicationFactory` with in-memory
  SQLite. They cover the 201 and 404 paths, round-tripping a stored result, 400 validation responses and
  that invalid requests store nothing.
- The test folders and namespaces mirror the source.

## Extending for a new customer

1. Implement `IPackingStrategy` (and `ILpnGenerator` if the identifier format differs).
2. Register the customer: `services.AddCubing(r => r.Add("NewCustomer", new MyStrategy(), new MyLpnGenerator()))`.
   The customer then sends `"customerId": "NewCustomer"`.
3. Add unit tests next to the existing ones in `tests/Cubing.Core.Tests/Packing/`.
