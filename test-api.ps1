# Quick Test Script for Pet Adoption API
# Run this after starting infrastructure and API

Write-Host "=== Pet Adoption API Test Script ===" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5029"

# 1. Get all pet types
Write-Host "1. Fetching pet types..." -ForegroundColor Yellow
try {
    $petTypes = Invoke-RestMethod -Uri "$baseUrl/api/admin/pet-types" -Method Get
    $dogType = $petTypes | Where-Object { $_.code -eq "dog" }
    $catType = $petTypes | Where-Object { $_.code -eq "cat" }

    Write-Host "   ✓ Found $($petTypes.Count) pet types" -ForegroundColor Green
    Write-Host "     - Dog ID: $($dogType.id)" -ForegroundColor Gray
    Write-Host "     - Cat ID: $($catType.id)" -ForegroundColor Gray
} catch {
    Write-Host "   ✗ Failed to fetch pet types" -ForegroundColor Red
    Write-Host "     Make sure API is running on $baseUrl" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 2. Create a dog
Write-Host "2. Creating a dog named 'Buddy'..." -ForegroundColor Yellow
try {
    $createDog = @{
        name = "Buddy"
        petTypeId = $dogType.id
    } | ConvertTo-Json

    $dog = Invoke-RestMethod -Uri "$baseUrl/api/pets" -Method Post -Body $createDog -ContentType "application/json"
    Write-Host "   ✓ Created dog with ID: $($dog.id)" -ForegroundColor Green
    $dogId = $dog.id
} catch {
    Write-Host "   ✗ Failed to create dog" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 3. Create a cat
Write-Host "3. Creating a cat named 'Whiskers'..." -ForegroundColor Yellow
try {
    $createCat = @{
        name = "Whiskers"
        petTypeId = $catType.id
    } | ConvertTo-Json

    $cat = Invoke-RestMethod -Uri "$baseUrl/api/pets" -Method Post -Body $createCat -ContentType "application/json"
    Write-Host "   ✓ Created cat with ID: $($cat.id)" -ForegroundColor Green
    $catId = $cat.id
} catch {
    Write-Host "   ✗ Failed to create cat" -ForegroundColor Red
    exit 1
}

Write-Host ""

# 4. List all pets
Write-Host "4. Listing all pets..." -ForegroundColor Yellow
try {
    $allPets = Invoke-RestMethod -Uri "$baseUrl/api/pets" -Method Get
    Write-Host "   ✓ Found $($allPets.Count) pets:" -ForegroundColor Green
    foreach ($pet in $allPets) {
        Write-Host "     - $($pet.name) ($($pet.type)) - Status: $($pet.status)" -ForegroundColor Gray
    }
} catch {
    Write-Host "   ✗ Failed to list pets" -ForegroundColor Red
}

Write-Host ""

# 5. Reserve the dog
Write-Host "5. Reserving 'Buddy' the dog..." -ForegroundColor Yellow
try {
    $reserveResult = Invoke-RestMethod -Uri "$baseUrl/api/pets/$dogId/reserve" -Method Post
    Write-Host "   ✓ Reserved! Status: $($reserveResult.status)" -ForegroundColor Green
    Write-Host "     Check RabbitMQ: http://localhost:15672 (guest/guest)" -ForegroundColor Cyan
} catch {
    Write-Host "   ✗ Failed to reserve dog" -ForegroundColor Red
}

Write-Host ""

# 6. Try to reserve again (should fail)
Write-Host "6. Trying to reserve 'Buddy' again (should fail)..." -ForegroundColor Yellow
try {
    $reserveAgain = Invoke-RestMethod -Uri "$baseUrl/api/pets/$dogId/reserve" -Method Post -ErrorAction Stop
    Write-Host "   ✗ Unexpected success - pet was reserved twice!" -ForegroundColor Red
} catch {
    $errorResponse = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "   ✓ Correctly rejected: $($errorResponse.errorCode)" -ForegroundColor Green
    Write-Host "     Message: $($errorResponse.message)" -ForegroundColor Gray
}

Write-Host ""

# 7. Adopt the dog
Write-Host "7. Adopting 'Buddy'..." -ForegroundColor Yellow
try {
    $adoptResult = Invoke-RestMethod -Uri "$baseUrl/api/pets/$dogId/adopt" -Method Post
    Write-Host "   ✓ Adopted! Status: $($adoptResult.status)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Failed to adopt dog" -ForegroundColor Red
}

Write-Host ""

# 8. Reserve the cat
Write-Host "8. Reserving 'Whiskers' the cat..." -ForegroundColor Yellow
try {
    $reserveCat = Invoke-RestMethod -Uri "$baseUrl/api/pets/$catId/reserve" -Method Post
    Write-Host "   ✓ Reserved! Status: $($reserveCat.status)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Failed to reserve cat" -ForegroundColor Red
}

Write-Host ""

# 9. Cancel cat reservation
Write-Host "9. Cancelling 'Whiskers' reservation..." -ForegroundColor Yellow
try {
    $cancelResult = Invoke-RestMethod -Uri "$baseUrl/api/pets/$catId/cancel-reservation" -Method Post
    Write-Host "   ✓ Cancelled! Status: $($cancelResult.status)" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Failed to cancel reservation" -ForegroundColor Red
}

Write-Host ""

# 10. Final status check
Write-Host "10. Final pet status:" -ForegroundColor Yellow
try {
    $finalDog = Invoke-RestMethod -Uri "$baseUrl/api/pets/$dogId" -Method Get
    $finalCat = Invoke-RestMethod -Uri "$baseUrl/api/pets/$catId" -Method Get

    Write-Host "   - $($finalDog.name): $($finalDog.status)" -ForegroundColor $(if ($finalDog.status -eq "Adopted") { "Green" } else { "Gray" })
    Write-Host "   - $($finalCat.name): $($finalCat.status)" -ForegroundColor $(if ($finalCat.status -eq "Available") { "Green" } else { "Gray" })
} catch {
    Write-Host "   ✗ Failed to fetch final status" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Check RabbitMQ Management UI: http://localhost:15672" -ForegroundColor Gray
Write-Host "  2. View Swagger UI: http://localhost:5029/swagger" -ForegroundColor Gray
Write-Host "  3. Check API logs for published events" -ForegroundColor Gray
