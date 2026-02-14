# Running the Project Locally

This guide shows how to run infrastructure in Docker and the API locally for development.

## Prerequisites

- Docker Desktop installed and running
- .NET 9 SDK installed
- Terminal/PowerShell

## Step 1: Start Infrastructure with Docker Compose

Start only MongoDB and RabbitMQ (without the API service):

```bash
cd C:\Users\tmirzaiev\RiderProjects\PetAdoption

# Start only infrastructure services
docker compose up -d mongo rabbitmq
```

Verify services are running:
```bash
docker compose ps
```

You should see:
- `mongo` - Running on port 27017
- `rabbitmq` - Running on ports 5672 (AMQP) and 15672 (Management UI)

### Access RabbitMQ Management UI

Open browser: http://localhost:15672
- Username: `guest`
- Password: `guest`

## Step 2: Run the API Locally

Navigate to the API project and run:

```bash
cd src/Services/PetService/PetAdoption.PetService.API

# Run the API (it will use Development environment by default)
dotnet run
```

You should see output like:
```
info: RabbitMQ topology setup completed. Exchanges: 1, Queues: 0
info: Now listening on: http://localhost:5029
```

The API is now running and connected to:
- MongoDB at `mongodb://root:example@localhost:27017`
- RabbitMQ at `localhost:5672`

## Step 3: Access Swagger UI

Open browser: http://localhost:5029/swagger

You'll see all available endpoints.

## Step 4: Make Test Requests

### Using cURL (PowerShell/CMD)

#### 1. Get All Pet Types
```powershell
curl http://localhost:5029/api/admin/pet-types
```

Expected response:
```json
[
  {"id":"4bd3a110-cf56-4be2-8dd3-0558256fa1ed","code":"dog","name":"Dog","isActive":true},
  {"id":"adc39b1e-0403-4bf3-8c74-4b3e971bb333","code":"cat","name":"Cat","isActive":true},
  ...
]
```

#### 2. Create a Pet
```powershell
curl -X POST http://localhost:5029/api/pets `
  -H "Content-Type: application/json" `
  -d '{\"name\":\"Buddy\",\"petTypeId\":\"4bd3a110-cf56-4be2-8dd3-0558256fa1ed\"}'
```

Response:
```json
{"id":"abc123-..."}
```

#### 3. Get All Pets
```powershell
curl http://localhost:5029/api/pets
```

#### 4. Reserve a Pet
```powershell
# Replace {petId} with actual ID from step 2
curl -X POST http://localhost:5029/api/pets/{petId}/reserve
```

Response:
```json
{"success":true,"petId":"abc123-...","status":"Reserved"}
```

#### 5. Check RabbitMQ

After reserving a pet:
1. Go to RabbitMQ Management UI: http://localhost:15672
2. Click "Exchanges" tab
3. You should see `pet.events` exchange
4. Click on it and see the message was published

### Using Swagger UI

Alternatively, use the Swagger UI at http://localhost:5029/swagger to make requests through the browser.

## Step 5: Monitor Logs

Watch the API console output to see:
```
info: Published PetReservedEvent to exchange 'pet.events' with routing key 'pet.reserved.v1'
```

## Complete Test Workflow

Here's a complete test scenario in PowerShell:

```powershell
# 1. Get dog pet type ID
$petTypes = curl http://localhost:5029/api/admin/pet-types | ConvertFrom-Json
$dogTypeId = ($petTypes | Where-Object { $_.code -eq "dog" }).id

# 2. Create a pet
$createResponse = curl -X POST http://localhost:5029/api/pets `
  -H "Content-Type: application/json" `
  -d "{`"name`":`"Max`",`"petTypeId`":`"$dogTypeId`"}" | ConvertFrom-Json

$petId = $createResponse.id
Write-Host "Created pet with ID: $petId"

# 3. Reserve the pet
curl -X POST "http://localhost:5029/api/pets/$petId/reserve"

# 4. Check pet status
curl "http://localhost:5029/api/pets/$petId"

# 5. Adopt the pet
curl -X POST "http://localhost:5029/api/pets/$petId/adopt"

# 6. Verify final status
curl "http://localhost:5029/api/pets/$petId"
```

## Verify RabbitMQ Messages

### Via Management UI
1. Open http://localhost:15672
2. Go to "Exchanges" → "pet.events"
3. Scroll to "Publish message" section
4. You can see message rate statistics

### Via CLI (if you have rabbitmqadmin)
```bash
# Get exchange info
docker exec rabbitmq rabbitmqctl list_exchanges

# See bindings
docker exec rabbitmq rabbitmqctl list_bindings
```

## Stop Infrastructure

When done testing:

```bash
# Stop infrastructure
docker compose down

# Or keep data and just stop
docker compose stop
```

## Troubleshooting

### MongoDB Connection Issues
- Ensure Docker Desktop is running
- Check MongoDB is accessible: `docker exec -it mongo mongosh --eval "db.version()"`
- Verify credentials in `appsettings.Development.json`

### RabbitMQ Connection Issues
- Check RabbitMQ is running: `docker ps | grep rabbitmq`
- Verify management UI is accessible: http://localhost:15672
- Check API logs for connection errors

### Port Already in Use
If port 5029 is in use, the API will choose a different port. Check the console output:
```
Now listening on: http://localhost:{PORT}
```

## Development Tips

### Watch Mode
Run API with hot reload:
```bash
dotnet watch run
```

### Debug Mode
Run in debug mode from Visual Studio or Rider:
1. Set `PetAdoption.PetService.API` as startup project
2. Press F5

### View Logs
API logs are shown in console. For structured logging, check:
- Console output (colorized)
- Application Insights (if configured)
- Seq (if configured)

## Next Steps

- Check `RABBITMQ_CONFIGURATION.md` for advanced RabbitMQ setup
- See `CLAUDE.md` for architecture details
- Read `ERROR_HANDLING.md` for error handling patterns
