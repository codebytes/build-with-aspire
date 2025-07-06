# BuildWithAspire Playwright Tests

This project contains end-to-end (E2E) tests for the BuildWithAspire Blazor application using Playwright for .NET.

## Overview

The tests cover:
- **Home Page**: Navigation, responsive design, basic functionality
- **Chat Page**: Message sending, conversation management, real-time features
- **Weather Page**: Data display, loading states, error handling
- **Conversation Management**: Creating, selecting, deleting conversations
- **Accessibility**: Semantic structure, keyboard navigation, screen reader support

## Test Structure

### Base Classes
- `BasePlaywrightTest`: Base class that integrates Playwright with Aspire distributed application testing
- `GlobalSetup`: Handles Playwright browser installation and global setup

### Test Categories
1. **HomePageTests**: Basic navigation and layout tests
2. **ChatPageTests**: Chat functionality including message sending and AI responses
3. **ConversationManagementTests**: Conversation CRUD operations and persistence
4. **WeatherPageTests**: Weather data display and responsive design
5. **AccessibilityTests**: WCAG compliance and keyboard navigation

## Running the Tests

### Prerequisites
- .NET 9.0 SDK
- Playwright browsers (installed automatically on first run)

### Commands

```bash
# Run all Playwright tests
dotnet test tests/BuildWithAspire.PlaywrightTests/

# Run specific test class
dotnet test tests/BuildWithAspire.PlaywrightTests/ --filter "ClassName=ChatPageTests"

# Run with specific browser
dotnet test tests/BuildWithAspire.PlaywrightTests/ -- --browser=chromium

# Run in headed mode (visible browser)
dotnet test tests/BuildWithAspire.PlaywrightTests/ -- --headed
```

### Test Configuration

The tests use `playwright.config.json` for configuration:
- **Timeout**: 30 seconds per test
- **Retries**: 1 retry on failure
- **Browsers**: Chromium, Firefox, WebKit
- **Screenshots**: On failure only
- **Videos**: Retained on failure
- **Traces**: On first retry

## Test Data and State

### Aspire Integration
Tests automatically:
- Start the Aspire distributed application
- Wait for all services to be ready
- Get the correct web frontend URL
- Handle Blazor SignalR connections

### Data Isolation
- Tests may create and clean up their own conversations
- Shared database state between tests is expected
- Tests are designed to be resilient to existing data

### AI Integration
- Tests work with real AI responses (may be slow)
- Fallback handling for AI service unavailability
- Mock-friendly design for faster test execution

## Test Helpers

### Navigation
```csharp
await NavigateToPageAsync("/chat"); // Navigate and wait for Blazor
await WaitForBlazorConnectionAsync(); // Wait for SignalR
```

### Element Selection
Tests use multiple selectors for robustness:
```csharp
var messageInput = Page.GetByRole(AriaRole.Textbox, new() { Name = new Regex("message|chat|input") })
    .Or(Page.Locator("input[type='text']"))
    .Or(Page.Locator("[data-testid='message-input']"));
```

### Conversation Management
```csharp
await EnsureConversationExists(); // Helper to create conversation if needed
await CreateConversationWithName("Test"); // Create named conversation
```

## Best Practices

### Selectors
- Use semantic selectors (roles, labels) first
- Fall back to data-testid attributes
- Avoid brittle CSS selectors
- Use regex for flexible text matching

### Waits
- Use `WaitForLoadStateAsync(LoadState.NetworkIdle)` for page loads
- Use `Expect().ToBeVisibleAsync()` with timeouts for elements
- Add explicit waits for AI responses (can be slow)

### Error Handling
- Tests handle missing elements gracefully
- Fallback strategies for different UI implementations
- Comprehensive error messages for debugging

### Accessibility
- Tests verify semantic HTML structure
- Check for proper ARIA labels and roles
- Validate keyboard navigation
- Ensure proper color contrast

## Debugging

### Visual Debugging
```bash
# Run with headed browser
dotnet test -- --headed

# Take screenshots on all actions
dotnet test -- --screenshot=on
```

### Trace Analysis
```bash
# Generate trace files
dotnet test -- --trace=on

# View traces in Playwright Trace Viewer
npx playwright show-trace test-results/trace.zip
```

### Console Logs
Tests capture browser console errors:
```csharp
Page.Console += (_, e) => {
    if (e.Type == "error") errors.Add(e.Text);
};
```

## CI/CD Integration

### GitHub Actions
```yaml
- name: Run Playwright Tests
  run: dotnet test tests/BuildWithAspire.PlaywrightTests/
  
- name: Upload Test Results
  uses: actions/upload-artifact@v3
  with:
    name: playwright-results
    path: tests/BuildWithAspire.PlaywrightTests/test-results/
```

### Docker Support
Tests can run in containerized environments with:
- Headless browser execution
- Xvfb for display simulation
- Proper font installation for rendering

## Troubleshooting

### Common Issues
1. **Blazor Connection Timeout**: Increase wait times for SignalR connection
2. **Element Not Found**: Check for UI changes, update selectors
3. **AI Response Timeout**: Increase timeout for message processing
4. **Browser Installation**: Run `playwright install` manually if needed

### Performance
- Tests run sequentially to avoid resource conflicts
- Single worker configuration for Aspire app stability
- Shared browser contexts where possible

### Network Issues
- Tests handle network delays gracefully
- Retry logic for transient failures
- Proper cleanup on test failures