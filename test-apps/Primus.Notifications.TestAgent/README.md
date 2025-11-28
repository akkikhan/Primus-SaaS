# Primus Notification Module - Test Agent

## Overview
This comprehensive test agent validates the Primus Notification Module across 4 difficulty tiers with real-world scenarios.

## Test Scenarios

### 🟢 Easy: Welcome Email
**Real-World Use Case**: New user onboarding
- **Validates**: Basic single-channel dispatch
- **Template Features**: Simple variable substitution (`{{ Name }}`)
- **Expected Behavior**: Email sent successfully with personalized greeting

### 🟡 Moderate: Invoice Generated
**Real-World Use Case**: E-commerce billing system
- **Validates**: Complex Liquid templates with loops and conditionals
- **Template Features**: 
  - `{% for %}` loops for line items
  - `{% if %}` conditionals for payment status
  - Dynamic totals and formatting
- **Expected Behavior**: Professional invoice email with itemized breakdown

### 🟠 Hard: Multi-Channel Partial Failure
**Real-World Use Case**: Security alert system with SMS backup
- **Validates**: Resilience and graceful degradation
- **Channels**: Email (Success) + SMS (Simulated Failure)
- **Expected Behavior**: 
  - Email delivers successfully
  - SMS failure is logged but doesn't crash the system
  - Demonstrates fault tolerance

### 🔴 Complex: High Concurrency Load Test
**Real-World Use Case**: Black Friday sale notifications
- **Validates**: Thread safety and performance under load
- **Test Parameters**: 100 parallel notifications
- **Metrics Tracked**:
  - Total execution time
  - Average time per notification
  - Template caching effectiveness
- **Expected Behavior**: 
  - All 100 notifications dispatch successfully
  - No race conditions or deadlocks
  - Performance benefits from template caching (should be <5ms per notification after cache warm-up)

## Running the Tests

### Prerequisites
- .NET 8.0 Runtime
- Templates directory must be in the same folder as the executable

### Execution
```bash
cd test-apps/Primus.Notifications.TestAgent
dotnet run
```

### Interactive Menu
The test agent provides an interactive CLI:
1. **Run All**: Executes all 4 scenarios sequentially with progress bar
2. **Run Single**: Select and run a specific scenario
3. **Exit**: Quit the test agent

## Expected Output

### Success Indicators
- ✓ Green checkmarks for completed scenarios
- 📢 Logger channel outputs showing notification data
- Performance metrics for Complex scenario

### Failure Indicators
- ✗ Red X marks for failed scenarios
- Detailed error messages in console
- Stack traces for debugging

## Real-World Value Demonstration

### For Developers
- **Easy**: "I can send a notification in 1 line of code"
- **Moderate**: "Complex templates are handled automatically"
- **Hard**: "The system is resilient to partial failures"
- **Complex**: "It scales to production-level concurrency"

### For Management
- **Time Savings**: Reduces notification implementation from days to hours
- **Reliability**: Built-in fault tolerance prevents customer-facing failures
- **Scalability**: Proven to handle high-volume scenarios
- **Maintainability**: Templates can be updated by non-developers

## Architecture Validation

This test agent proves:
1. **Separation of Concerns**: Business logic (notifications) is decoupled from delivery (channels)
2. **Template Caching**: Performance optimization works correctly
3. **Multi-Channel Support**: Architecture supports multiple delivery mechanisms
4. **Error Handling**: Graceful degradation when channels fail
5. **Concurrency Safety**: Thread-safe for production use
