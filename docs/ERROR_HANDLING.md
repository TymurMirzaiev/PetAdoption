# Error Handling with Subcodes

This document describes the subcode-based error handling system implemented in the PetAdoption service.

## Overview

Instead of using multiple exception types, the system uses a **single `DomainException` class with error codes (subcodes)**. This approach provides:

- **Programmatic error handling** - API clients can switch on error codes
- **Consistent error responses** - All errors follow the same structure
- **Easy internationalization** - Error codes can be mapped to localized messages
- **Simplified exception handling** - Middleware catches all domain exceptions uniformly

## Architecture

```
Domain Layer
├── DomainException (single exception class)
├── PetDomainErrorCode (enum with all error codes)
└── Domain logic throws DomainException with appropriate code

Application Layer
└── ErrorResponse DTO (standardized API response)

Infrastructure Layer
└── ExceptionHandlingMiddleware (catches and transforms exceptions)

API Response
└── JSON with errorCode, message, details, timestamp
```

## Error Code Ranges

| Range | Category | Examples |
|-------|----------|----------|
| 1000-1999 | Pet aggregate errors | PetNotAvailable, PetNotReserved, PetNotFound |
| 2000-2999 | Value object validation | InvalidPetName, InvalidPetType |
| 9000-9999 | General domain errors | InvalidOperation, UnknownDomainError |

## Error Codes

### Pet Aggregate Errors (1000-1999)

#### `PetNotAvailable = 1001`
Pet cannot be reserved because it is not available.

**Example:**
```csharp
throw new DomainException(
    PetDomainErrorCode.PetNotAvailable,
    $"Pet {Id} cannot be reserved because it is {Status}.",
    new Dictionary<string, object>
    {
        { "PetId", Id },
        { "CurrentStatus", Status.ToString() },
        { "RequiredStatus", PetStatus.Available.ToString() }
    });
```

#### `PetNotReserved = 1002`
Pet cannot be adopted or have reservation cancelled because it is not reserved.

#### `PetNotFound = 1003`
Pet was not found in the system.

### Value Object Validation (2000-2999)

#### `InvalidPetName = 2001`
Pet name is invalid (empty, too long, etc.).

**Metadata Reasons:**
- `EmptyOrWhitespace` - Name is null, empty, or whitespace
- `TooShort` - Name is shorter than minimum length
- `TooLong` - Name exceeds maximum length

#### `InvalidPetType = 2002`
Pet type is invalid (not in allowed list).

**Metadata Reasons:**
- `EmptyOrWhitespace` - Type is null, empty, or whitespace
- `InvalidType` - Type is not in the valid types list

## HTTP Status Code Mapping

The `ExceptionHandlingMiddleware` maps error codes to appropriate HTTP status codes:

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `PetNotFound` | 404 Not Found | Resource doesn't exist |
| `InvalidPetName` | 400 Bad Request | Invalid input |
| `InvalidPetType` | 400 Bad Request | Invalid input |
| `PetNotAvailable` | 409 Conflict | Business rule violation |
| `PetNotReserved` | 409 Conflict | Business rule violation |
| Others | 500 Internal Server Error | Unexpected errors |

## API Error Response Format

All domain exceptions are transformed into a standardized JSON response:

```json
{
  "errorCode": "PetNotAvailable",
  "errorCodeValue": 1001,
  "message": "Pet abc123... cannot be reserved because it is Reserved.",
  "details": {
    "petId": "abc123...",
    "currentStatus": "Reserved",
    "requiredStatus": "Available"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

## Usage Examples

### Example 1: Attempting to Reserve an Already Reserved Pet

**Request:**
```http
POST /api/pets/abc123.../reserve
```

**Response:** `409 Conflict`
```json
{
  "errorCode": "PetNotAvailable",
  "errorCodeValue": 1001,
  "message": "Pet abc123... cannot be reserved because it is Reserved. Only Available pets can be reserved.",
  "details": {
    "petId": "abc123...",
    "currentStatus": "Reserved",
    "requiredStatus": "Available"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

**Client Handling:**
```typescript
if (response.errorCode === "PetNotAvailable") {
  if (response.details.currentStatus === "Reserved") {
    showMessage("This pet is already reserved by someone else.");
  } else if (response.details.currentStatus === "Adopted") {
    showMessage("This pet has already been adopted.");
  }
}
```

### Example 2: Invalid Pet Name

**Request:**
```http
POST /api/pets
{
  "name": "",
  "type": "Dog"
}
```

**Response:** `400 Bad Request`
```json
{
  "errorCode": "InvalidPetName",
  "errorCodeValue": 2001,
  "message": "Pet name cannot be empty or whitespace.",
  "details": {
    "attemptedValue": "",
    "reason": "EmptyOrWhitespace"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

**Client Handling:**
```typescript
if (response.errorCode === "InvalidPetName") {
  switch (response.details.reason) {
    case "EmptyOrWhitespace":
      showFieldError("name", "Please enter a pet name");
      break;
    case "TooLong":
      showFieldError("name", `Name must be at most ${response.details.maxLength} characters`);
      break;
  }
}
```

### Example 3: Invalid Pet Type

**Request:**
```http
POST /api/pets
{
  "name": "Fluffy",
  "type": "Dragon"
}
```

**Response:** `400 Bad Request`
```json
{
  "errorCode": "InvalidPetType",
  "errorCodeValue": 2002,
  "message": "Pet type must be one of: Dog, Cat, Rabbit, Bird, Fish, Hamster.",
  "details": {
    "attemptedValue": "Dragon",
    "validTypes": ["Dog", "Cat", "Rabbit", "Bird", "Fish", "Hamster"],
    "reason": "InvalidType"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

**Client Handling:**
```typescript
if (response.errorCode === "InvalidPetType") {
  showFieldError("type", `Please select a valid pet type: ${response.details.validTypes.join(", ")}`);
}
```

### Example 4: Pet Not Found

**Request:**
```http
POST /api/pets/nonexistent-id/reserve
```

**Response:** `404 Not Found`
```json
{
  "errorCode": "PetNotFound",
  "errorCodeValue": 1003,
  "message": "Pet with ID nonexistent-id was not found.",
  "details": {
    "petId": "nonexistent-id"
  },
  "timestamp": "2026-02-12T10:30:00Z"
}
```

**Client Handling:**
```typescript
if (response.errorCode === "PetNotFound") {
  showMessage("This pet is no longer available.");
  redirectToHomePage();
}
```

## Adding New Error Codes

To add a new error code:

1. **Add to enum** in `Domain/Exceptions/PetDomainErrorCode.cs`:
```csharp
/// <summary>
/// Pet cannot be transferred because it is not in transferable status.
/// </summary>
PetNotTransferable = 1004,
```

2. **Add HTTP mapping** in `Infrastructure/Middleware/ExceptionHandlingMiddleware.cs`:
```csharp
PetDomainErrorCode.PetNotTransferable => HttpStatusCode.Conflict,
```

3. **Throw in domain code**:
```csharp
public void Transfer()
{
    if (Status != PetStatus.Adopted)
    {
        throw new DomainException(
            PetDomainErrorCode.PetNotTransferable,
            $"Pet {Id} cannot be transferred because it is {Status}.",
            new Dictionary<string, object>
            {
                { "PetId", Id },
                { "CurrentStatus", Status.ToString() }
            });
    }
    // ...
}
```

## Benefits

### For Backend Developers
- ✅ Single exception type to handle
- ✅ Consistent error structure
- ✅ Middleware handles all formatting
- ✅ Rich metadata for debugging

### For Frontend Developers
- ✅ Programmatic error handling via error codes
- ✅ Consistent JSON structure
- ✅ Metadata for context-specific error messages
- ✅ Easy to implement retry logic or fallback behavior

### For Operations
- ✅ Structured logging with error codes
- ✅ Easy to monitor specific error types
- ✅ Clear categorization (4xx vs 5xx)
- ✅ Metadata helps with debugging

## Testing

### Unit Test Example
```csharp
[Fact]
public void Reserve_ThrowsDomainException_WhenPetIsAlreadyReserved()
{
    // Arrange
    var pet = Pet.Create("Bella", "Dog");
    pet.Reserve(); // First reservation

    // Act & Assert
    var exception = Assert.Throws<DomainException>(() => pet.Reserve());
    Assert.Equal(PetDomainErrorCode.PetNotAvailable, exception.ErrorCode);
    Assert.Contains("Reserved", exception.Message);
    Assert.Equal("Reserved", exception.Metadata["CurrentStatus"]);
}
```

### Integration Test Example
```csharp
[Fact]
public async Task ReservePet_Returns409_WhenPetIsAlreadyReserved()
{
    // Arrange
    var petId = await CreateReservedPet();

    // Act
    var response = await _client.PostAsync($"/api/pets/{petId}/reserve", null);

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

    var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
    Assert.Equal("PetNotAvailable", error.ErrorCode);
    Assert.Equal(1001, error.ErrorCodeValue);
}
```
