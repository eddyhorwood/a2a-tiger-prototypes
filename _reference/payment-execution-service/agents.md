# Project Instructions


## Architecture
- Three layers in the solution: Application Layer(for example webapi, worker and lambda), Domain Layer(with meditR), and client library layer(to access db, and other depneding services)
- Ensure clear separation of concerns
- Application Layer (aka top layer) acts as the entry point for external interactions.
- Domain layers (aka middle layer) encapsulates business/domain logic (leveraging MediatR for loose coupling via commands/queries)
- Client library layer (aka bottom layer) handles infrastructure concerns like external integrations and data access.
- Dependency Flow: Top → Middle → Bottom (one-way dependencies; use interfaces in middle/bottom to abstract bottom-layer implementations).
- Shared Projects: Business Domain (middle) and Client Libraries (bottom) are .NET class libraries referenced by both top-layer projects (Web API and Worker). Avoid direct coupling from bottom to middle/top.
- DI Container: Use a shared DI setup (e.g., in each top-layer project) to register middle/bottom services uniformly.
- MediatR Integration: Place it in the middle layer for request/response handling, keeping handlers focused on orchestration without direct infrastructure calls.
- Use auto mapper for mapping between objects within and across different layers.
- Mapping between Top Layer and middle layer should exist in the top layer.
- Mapping between middle and bottom layer should exist in the bottom layer.