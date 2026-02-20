# Quick Test Guide - UserService

Quick reference for testing UserService functionality. See [E2E-VERIFICATION-PLAN.md](./E2E-VERIFICATION-PLAN.md) for complete verification checklist.

## Quick Start

### 1. Start the Service
```bash
cd src/Services/UserService
docker-compose up -d

# Check logs
docker-compose logs -f userservice
```

### 2. Verify Services Running
```bash
docker-compose ps
# All should be "Up" and healthy
```

### 3. Access Points
- **UserService API**: http://localhost:5001
- **MongoDB**: mongodb://localhost:27017 (root/example)
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)

## Common Test Scenarios

### Register a User
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "fullName": "Test User",
    "password": "SecurePass123!",
    "phoneNumber": "+1234567890"
  }'
```

**Expected**: HTTP 200, returns `userId` and success message

### Login and Get Token
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123!"
  }'
```

**Expected**: HTTP 200, returns JWT token in `token` field

**Save the token**:
```bash
export JWT_TOKEN="<paste-token-here>"
```

### Get User Profile
```bash
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"
```

**Expected**: HTTP 200, returns user profile

### Update Profile
```bash
curl -X PUT http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "Updated Name",
    "phoneNumber": "+9876543210",
    "preferences": null
  }'
```

**Expected**: HTTP 200, returns success message

### Change Password
```bash
curl -X POST http://localhost:5001/api/users/me/change-password \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "SecurePass123!",
    "newPassword": "NewSecurePass456!"
  }'
```

**Expected**: HTTP 200, password changed

## Admin Operations

### 1. Manually Promote User to Admin
```bash
# Access MongoDB
docker exec -it petadoption-mongodb mongosh -u root -p example

# In MongoDB shell:
use UserDb
db.Users.updateOne(
  { "Email.Value": "test@example.com" },
  { $set: { "Role": 1 } }
)
exit
```

### 2. Login as Admin
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "NewSecurePass456!"
  }'

# Save admin token
export ADMIN_TOKEN="<admin-token-here>"
```

**Verify**: Response should show `"role": "Admin"`

### 3. List All Users (Admin Only)
```bash
curl -X GET "http://localhost:5001/api/users?skip=0&take=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Expected**: HTTP 200, returns array of users

### 4. Get User by ID (Admin Only)
```bash
curl -X GET http://localhost:5001/api/users/<userId> \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Expected**: HTTP 200, returns user details

### 5. Suspend User (Admin Only)
```bash
curl -X POST http://localhost:5001/api/users/<userId>/suspend \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Violation of terms"
  }'
```

**Expected**: HTTP 200, user suspended

### 6. Promote User to Admin (Admin Only)
```bash
curl -X POST http://localhost:5001/api/users/<userId>/promote-to-admin \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

**Expected**: HTTP 200, user promoted

## Database Inspection

### View Users in MongoDB
```bash
docker exec -it petadoption-mongodb mongosh -u root -p example

# In MongoDB shell:
use UserDb

# Count users
db.Users.countDocuments()

# View all users
db.Users.find().pretty()

# Find specific user
db.Users.findOne({ "Email.Value": "test@example.com" })

# View outbox events
db.OutboxEvents.find().sort({CreatedAt: -1}).limit(5).pretty()
```

### View RabbitMQ Events

1. Open browser: http://localhost:15672
2. Login: guest/guest
3. Navigate to: Exchanges → user.events
4. Create test queue:
   - Queues → Add queue → Name: `test-events` → Add
5. Bind queue to exchange:
   - Queues → test-events → Bindings
   - From exchange: `user.events`
   - Routing key: `user.#`
   - Bind
6. Perform user actions (register, update, etc.)
7. Check queue: Queues → test-events → Get messages

## Verify Token Structure

### Decode JWT Token
```bash
# Using jwt.io (paste token in browser)
# Or using command line:
echo "$JWT_TOKEN" | cut -d. -f2 | base64 -d 2>/dev/null | jq
```

**Verify contains**:
- `sub`: userId
- `email`: user email
- `role`: User or Admin
- `userId`: custom claim
- `iss`: PetAdoption.UserService
- `aud`: PetAdoption.Services
- `exp`: expiration timestamp

## Common Issues & Solutions

### Issue: Cannot connect to API
```bash
# Check if service is running
docker-compose ps

# Check logs for errors
docker-compose logs userservice

# Restart service
docker-compose restart userservice
```

### Issue: MongoDB connection failed
```bash
# Check MongoDB is running
docker-compose ps mongodb

# Check MongoDB logs
docker-compose logs mongodb

# Restart MongoDB
docker-compose restart mongodb
```

### Issue: RabbitMQ not publishing events
```bash
# Check RabbitMQ is running
docker-compose ps rabbitmq

# Check RabbitMQ logs
docker-compose logs rabbitmq

# Verify exchange exists
# Go to http://localhost:15672 and check Exchanges tab
```

### Issue: 401 Unauthorized
- Check token is valid
- Check token hasn't expired (1 hour lifetime)
- Check Authorization header format: `Bearer <token>`
- Re-login to get fresh token

### Issue: 403 Forbidden (Admin endpoints)
- Check user has Admin role
- Verify token is for admin user
- Check role claim in token: `"role": "Admin"`

## Reset Everything

### Restart Services (Keep Data)
```bash
docker-compose restart
```

### Full Reset (Delete All Data)
```bash
docker-compose down -v
docker-compose up -d
```

## Testing Flow Example

Complete flow testing all major features:

```bash
# 1. Start services
docker-compose up -d && sleep 5

# 2. Register user
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","fullName":"Demo User","password":"Demo123!"}'

# 3. Login
JWT_TOKEN=$(curl -s -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"Demo123!"}' | jq -r '.token')

# 4. Get profile
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"

# 5. Update profile
curl -X PUT http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"fullName":"Updated Demo","phoneNumber":"+1234567890","preferences":null}'

# 6. Change password
curl -X POST http://localhost:5001/api/users/me/change-password \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"currentPassword":"Demo123!","newPassword":"NewDemo456!"}'

# 7. Login with new password
JWT_TOKEN=$(curl -s -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"NewDemo456!"}' | jq -r '.token')

# 8. Promote to admin (via MongoDB)
docker exec -it petadoption-mongodb mongosh -u root -p example --eval \
  'use UserDb; db.Users.updateOne({"Email.Value":"demo@example.com"},{$set:{"Role":1}})'

# 9. Login as admin
ADMIN_TOKEN=$(curl -s -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"demo@example.com","password":"NewDemo456!"}' | jq -r '.token')

# 10. List users as admin
curl -X GET "http://localhost:5001/api/users?skip=0&take=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN"

echo "✅ All tests completed!"
```

## Next Steps

For complete end-to-end verification, see [E2E-VERIFICATION-PLAN.md](./E2E-VERIFICATION-PLAN.md)

## Useful Commands

```bash
# View all containers
docker-compose ps

# View logs (all services)
docker-compose logs -f

# View logs (specific service)
docker-compose logs -f userservice

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Restart specific service
docker-compose restart userservice

# Execute command in container
docker exec -it petadoption-userservice sh

# View MongoDB data
docker exec -it petadoption-mongodb mongosh -u root -p example

# Pretty print JSON responses
curl ... | jq '.'
```
