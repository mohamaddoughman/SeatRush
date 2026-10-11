# ADR 0003: API documentation with the built-in OpenAPI generator and Scalar

- **Status:** Accepted
- **Date:** 2026-10-11
- **Decision owner:** Mohamad

## Context

Step 1.4c adds the first HTTP endpoints. Trying them by hand, and later describing them to other tools, needs a machine-readable description of the API and a way to browse and call it.

Since .NET 9, ASP.NET Core generates OpenAPI documents itself (`Microsoft.AspNetCore.OpenApi`), and the project templates no longer include Swashbuckle. The document will be used beyond manual testing: k6 scripts in Phase 5 and possibly client generation.

## Decision

- **`Microsoft.AspNetCore.OpenApi`** generates the OpenAPI document, served at `/openapi/v1.json`.
- **Scalar** (`Scalar.AspNetCore`) shows it at `/scalar`, with a built-in request runner.
- Both endpoints are mapped **in Development only**. The generator is registered in every environment, but nothing is exposed outside Development.
- Controllers describe their responses with `[ProducesResponseType]`, which feeds the document.

## Alternatives considered

**Swashbuckle (generator + Swagger UI):** Rejected. Dropped from the .NET templates since .NET 9 and less actively maintained; the built-in generator replaces its main job.

**Built-in generator + Swagger UI:** Rejected. Works, but the UI is dated and offers nothing Scalar lacks.

**`.http` files only:** Rejected as the only option. No dependencies and version-controlled, but there is no browsable contract and nothing for other tools to consume. They can still be added next to the docs.

## Consequences

### Positive

- An always-current API description generated from the code, usable by people (Scalar) and tools (k6, client generators).
- First-party generator; the only third-party package is the UI, which is used only in Development.

### Negative

- The document is only as accurate as the response metadata. Problem-details extensions (the `code` field) aren't described yet, and a 400 is documented as `ValidationProblemDetails` even when it carries a domain error.
- Custom JSON converters can hide a property's type from the generator; an API test checks that date-times are still documented as `date-time`.

### When to revisit

- The API becomes public: decide whether to expose the docs outside Development, and how to secure them.
- Clients are generated from the document: describe the problem-details shape (including `code`) as a proper schema first.
