# Test Project Guidelines

These instructions apply to work under `Tests/*`.

## Frameworks & Libraries

- Use the following libraries when the test needs their capability. Do not introduce mocks, generated data, or helpers when direct setup is clearer.
- **Mocking:** Use `NSubstitute` for test doubles and interaction verification. Prefer real collaborators when they are simple, deterministic, and isolated.
- **Test Data:** Use `AutoFixture` for generic input data. Use explicit values when they affect the behavior or make the scenario easier to understand, and constrain generated data so the test remains deterministic.
- **Runner:** Use `xUnit.v3`.
- **Assertions:** Use `AwesomeAssertions` for fluent, descriptive assertions.
- **Blazor Testing:** Use `bUnit` for Blazor component unit tests.

## Test Style & Quality

- Follow the Arrange, Act, Assert pattern. Add phase comments when they make nontrivial tests easier to scan; omit them when the phases are already obvious.
- Each test should have one reason to fail. Multiple assertions are acceptable when they jointly prove the same observable outcome.
- Keep behavior-specific arrangement in each test. Use the test class constructor only for small, immutable shared setup; use `IAsyncLifetime` for asynchronous lifecycle work and `IDisposable` or `IAsyncDisposable` for teardown when needed.
- Name test classes after the production method or behavior being tested and suffix them with `_Tests`.
- Name test methods with the pattern `[State]_[ExpectedBehavior]`, for example `WithOvertime_ReturnsCorrectAmount`.
- Prefer `AutoFixture` for generic data. Override specific values in Arrange only when they matter to the behavior.
- Keep mock verifications explicit and minimal.
- Verify business intent instead of implementation details.

## File Organization

Use a granular structure so tests stay navigable and focused.

1. By default, create a folder for the production class or component under test.
2. Create a separate test file for each method, behavior, or component surface when that improves navigation. Keep small, cohesive test surfaces together when splitting them would add ceremony without improving clarity.
3. Use `[MethodName].Tests.cs` for unit tests and `[MethodName].IntegrationTests.cs` for integration tests when practical.
4. Keep all states and expected behaviors for that method or component surface in the corresponding file.

For Blazor components, place component tests in the appropriate Blazor test project and use bUnit render assertions, event dispatch, and semantic queries where possible.

## Blazor Component Test Expectations

- Every new or meaningfully changed observable Blazor component behavior should have corresponding bUnit coverage.
- Use `xUnit` and bUnit with the approved test libraries above when their capabilities are needed.
- Prefer assertions against rendered behavior, accessible text, roles, labels, and callback effects over brittle markup details.
- Register injected dependencies explicitly and keep component setup readable. Substitute only the dependencies that must be isolated.
- When component structure or NTComponents usage is unclear, inspect existing tests and match the local pattern before adding new helpers.

## Coverage & Bug Reporting

- Target high coverage for critical paths, edge cases, and error handling.
- Add or update tests when a change affects observable behavior or a contract, including business logic, serialized DTO shapes, repository contracts, pages, moderation flows, billing flows, and component behavior.
- If a bug is discovered while writing tests, report the reproduction, impact, and likely fix clearly. Add a regression test when it is within scope, and do not silently expand production-code changes beyond the requested work.

## Integration & E2E Testing

- Keep all integration and end-to-end tests in a dedicated project named `[ProjectName].IntegrationTests` so unit-test runs remain isolated from infrastructure and browser dependencies.

### Testcontainers

- Use `Testcontainers for .NET` for tests requiring real infrastructure such as databases or queues.
- A test that starts or connects to real infrastructure is an integration test and must reside in the dedicated integration test project.
- Dispose containers cleanly to prevent resource leaks.

### Playwright

- Use Playwright for browser-based integration and end-to-end tests.
- Playwright tests must follow the dedicated integration-project rule above.
- Name Playwright test files `[Behavior].IntegrationTests.cs`.
- Use Page Objects when interactions or selectors recur or a workflow is complex. Keep assertions in the test so the behavior being proved remains visible.
- Prefer user-facing locators such as `GetByRole`, `GetByText`, and `GetByLabel` over brittle CSS or XPath selectors.
- Rely on Playwright auto-waiting. Avoid hardcoded `Thread.Sleep`.
- Use storage state to avoid repeated logins when authentication is not the behavior under test. Keep state separate by identity or role, and authenticate explicitly in login, authorization-transition, session-expiry, and account-isolation tests.

## Validation

- Unit tests must be fast and isolated with no external dependencies.
- Run focused test filters while iterating.
- Before finishing, run the affected test project with `dotnet test` when practical.
- For Blazor component work, ensure the relevant bUnit tests pass locally before finalizing.
- If a validation command cannot be run, state that clearly in the final response.

## General Rules

- Never commit secrets or environment-specific configuration. Use environment variables or secure storage.
- Update tests whenever business logic changes. Tests are part of the feature, not a follow-up task.
