# UserService End-to-End Verification Plan

Complete checklist for verifying UserService functionality from infrastructure to API endpoints.

## Prerequisites Verification

### 1. Infrastructure Setup
- [ ] Docker and Docker Compose installed and running
- [ ] Ports available: 5001 (API), 27017 (MongoDB), 5672/15672 (RabbitMQ)
- [ ] Network connectivity working

### 2. Service Startup
- [ ] Run `docker-compose up -d` successfully
- [ ] All containers running: `docker-compose ps`
  - [ ] petadoption-userservice (healthy)
  - [ ] petadoption-mongodb (healthy)
  - [ ] petadoption-rabbitmq (healthy)
- [ ] Check UserService logs: `docker-compose logs userservice`
  - [ ] No startup errors
  - [ ] "UserService API starting..." message present
  - [ ] MongoDB connection successful
  - [ ] RabbitMQ connection successful
  - [ ] Exchange "user.events" created

### 3. Database Connectivity
- [ ] Connect to MongoDB: `docker exec -it petadoption-mongodb mongosh -u root -p example`
- [ ] Database "UserDb" created automatically
- [ ] Collections exist:
  - [ ] `Users` collection
  - [ ] `OutboxEvents` collection

### 4. Message Broker Connectivity
- [ ] Access RabbitMQ Management UI: http://localhost:15672
- [ ] Login with guest/guest
- [ ] Exchange "user.events" exists
  - [ ] Type: topic
  - [ ] Durable: true

## API Endpoint Verification

### 5. Health & Documentation
- [ ] API is accessible: http://localhost:5001
- [ ] OpenAPI endpoint available: http://localhost:5001/openapi/v1.json
- [ ] No unauthorized access to protected endpoints

### 6. User Registration (Public Endpoint)

**Test Case 6.1: Successful Registration**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "fullName": "John Doe",
    "password": "SecurePass123!",
    "phoneNumber": "+1234567890"
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Response contains:
  - [ ] `success: true`
  - [ ] `userId` (non-empty string)
  - [ ] `message: "User registered successfully"`

**Test Case 6.2: Duplicate Email**
```bash
# Register same email again
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "fullName": "Jane Doe",
    "password": "AnotherPass123!"
  }'
```

Expected Response:
- [ ] HTTP 400 or 500
- [ ] Error message about existing email

**Test Case 6.3: Invalid Email Format**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "not-an-email",
    "fullName": "Test User",
    "password": "Password123!"
  }'
```

Expected Response:
- [ ] HTTP 400
- [ ] Validation error for email format

**Test Case 6.4: Weak Password**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "fullName": "Test User",
    "password": "123"
  }'
```

Expected Response:
- [ ] HTTP 400
- [ ] Validation error for password requirements

### 7. User Login (Public Endpoint)

**Test Case 7.1: Successful Login**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePass123!"
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Response contains:
  - [ ] `success: true`
  - [ ] `token` (JWT token string)
  - [ ] `userId`
  - [ ] `email: "john.doe@example.com"`
  - [ ] `fullName: "John Doe"`
  - [ ] `role: "User"`
  - [ ] `expiresIn: 3600` (seconds)

**Save the JWT token for subsequent tests:**
```bash
export JWT_TOKEN="<token-from-response>"
```

**Test Case 7.2: Invalid Credentials**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "WrongPassword"
  }'
```

Expected Response:
- [ ] HTTP 401 Unauthorized
- [ ] Error message about invalid credentials

**Test Case 7.3: Non-existent User**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "nonexistent@example.com",
    "password": "Password123!"
  }'
```

Expected Response:
- [ ] HTTP 401 or 404
- [ ] Error message about user not found

### 8. Get Current User Profile (Authenticated)

**Test Case 8.1: With Valid Token**
```bash
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Response contains user details:
  - [ ] `userId`
  - [ ] `email: "john.doe@example.com"`
  - [ ] `fullName: "John Doe"`
  - [ ] `role: "User"`
  - [ ] `phoneNumber: "+1234567890"`
  - [ ] `registeredAt` (timestamp)

**Test Case 8.2: Without Token**
```bash
curl -X GET http://localhost:5001/api/users/me
```

Expected Response:
- [ ] HTTP 401 Unauthorized

**Test Case 8.3: With Invalid Token**
```bash
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer invalid-token"
```

Expected Response:
- [ ] HTTP 401 Unauthorized

### 9. Update User Profile (Authenticated)

**Test Case 9.1: Update Full Name and Phone**
```bash
curl -X PUT http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Updated Doe",
    "phoneNumber": "+9876543210",
    "preferences": null
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Success message
- [ ] Updated data reflected

**Test Case 9.2: Verify Update Persisted**
```bash
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"
```

Expected Response:
- [ ] `fullName: "John Updated Doe"`
- [ ] `phoneNumber: "+9876543210"`

### 10. Change Password (Authenticated)

**Test Case 10.1: Successful Password Change**
```bash
curl -X POST http://localhost:5001/api/users/me/change-password \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "SecurePass123!",
    "newPassword": "NewSecurePass456!"
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Success message

**Test Case 10.2: Login with New Password**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "NewSecurePass456!"
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] New JWT token returned

**Test Case 10.3: Old Password No Longer Works**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "SecurePass123!"
  }'
```

Expected Response:
- [ ] HTTP 401 Unauthorized

**Test Case 10.4: Wrong Current Password**
```bash
curl -X POST http://localhost:5001/api/users/me/change-password \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "currentPassword": "WrongPassword",
    "newPassword": "AnotherNewPass789!"
  }'
```

Expected Response:
- [ ] HTTP 400 or 401
- [ ] Error about incorrect current password

### 11. Admin Operations Setup

**Test Case 11.1: Register Admin User**

First, create an admin user directly in MongoDB:
```bash
# Access MongoDB
docker exec -it petadoption-mongodb mongosh -u root -p example

# Switch to UserDb
use UserDb

# Manually promote john.doe to admin
db.Users.updateOne(
  { "Email.Value": "john.doe@example.com" },
  { $set: { "Role": 1 } }
)

# Verify
db.Users.findOne({ "Email.Value": "john.doe@example.com" })
```

Verify:
- [ ] Role field is 1 (Admin)

**Test Case 11.2: Login as Admin and Get Token**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "NewSecurePass456!"
  }'
```

Expected Response:
- [ ] `role: "Admin"`

**Save admin token:**
```bash
export ADMIN_TOKEN="<admin-token-from-response>"
```

### 12. List Users (Admin Only)

**Test Case 12.1: Admin Can List Users**
```bash
curl -X GET "http://localhost:5001/api/users?skip=0&take=10" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Array of users
- [ ] Pagination info (total, skip, take)

**Test Case 12.2: Regular User Cannot List Users**

Create another regular user first:
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "regular@example.com",
    "fullName": "Regular User",
    "password": "RegularPass123!"
  }'

# Login as regular user
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "regular@example.com",
    "password": "RegularPass123!"
  }'

# Save token
export REGULAR_TOKEN="<token>"

# Try to list users
curl -X GET "http://localhost:5001/api/users" \
  -H "Authorization: Bearer $REGULAR_TOKEN"
```

Expected Response:
- [ ] HTTP 403 Forbidden

### 13. Get User By ID (Admin Only)

**Test Case 13.1: Admin Can Get User by ID**

Get the userId from the list users response, then:
```bash
curl -X GET http://localhost:5001/api/users/<userId> \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] User details returned

**Test Case 13.2: Regular User Cannot Get User by ID**
```bash
curl -X GET http://localhost:5001/api/users/<userId> \
  -H "Authorization: Bearer $REGULAR_TOKEN"
```

Expected Response:
- [ ] HTTP 403 Forbidden

### 14. Suspend User (Admin Only)

**Test Case 14.1: Admin Can Suspend User**
```bash
curl -X POST http://localhost:5001/api/users/<regular-user-id>/suspend \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Violation of terms of service"
  }'
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Success message

**Test Case 14.2: Suspended User Cannot Login**
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "regular@example.com",
    "password": "RegularPass123!"
  }'
```

Expected Response:
- [ ] HTTP 403 Forbidden
- [ ] Error message about suspended account

**Test Case 14.3: Regular User Cannot Suspend**
```bash
# Create another user to try to suspend
curl -X POST http://localhost:5001/api/users/<some-user-id>/suspend \
  -H "Authorization: Bearer $REGULAR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Test"
  }'
```

Expected Response:
- [ ] HTTP 403 Forbidden

### 15. Promote User to Admin (Admin Only)

**Test Case 15.1: Register New User for Promotion**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tobepromoted@example.com",
    "fullName": "To Be Promoted",
    "password": "PromotePass123!"
  }'
```

**Test Case 15.2: Admin Promotes User**
```bash
curl -X POST http://localhost:5001/api/users/<user-id>/promote-to-admin \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

Expected Response:
- [ ] HTTP 200 OK
- [ ] Success message

**Test Case 15.3: Verify Promotion**
```bash
# Login as promoted user
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "tobepromoted@example.com",
    "password": "PromotePass123!"
  }'
```

Expected Response:
- [ ] `role: "Admin"`

**Test Case 15.4: Regular User Cannot Promote**
```bash
curl -X POST http://localhost:5001/api/users/<some-user-id>/promote-to-admin \
  -H "Authorization: Bearer $REGULAR_TOKEN"
```

Expected Response:
- [ ] HTTP 403 Forbidden

## Data Persistence Verification

### 16. Database Persistence

**Test Case 16.1: Verify Data in MongoDB**
```bash
docker exec -it petadoption-mongodb mongosh -u root -p example

use UserDb

# Check users exist
db.Users.countDocuments()
```

Expected Results:
- [ ] Multiple users exist
- [ ] Password hashes start with "$2" (BCrypt)
- [ ] Email addresses are lowercase
- [ ] RegisteredAt and UpdatedAt timestamps present
- [ ] Role values: 0 (User) or 1 (Admin)

**Test Case 16.2: Password Hashing**
```bash
# In MongoDB shell
db.Users.findOne({ "Email.Value": "john.doe@example.com" })
```

Verify:
- [ ] Password.HashedValue starts with "$2" (BCrypt format)
- [ ] Password is NOT stored in plain text
- [ ] Different users have different password hashes

### 17. Event Publishing

**Test Case 17.1: Check Outbox Events**
```bash
# In MongoDB shell
use UserDb
db.OutboxEvents.find().sort({CreatedAt: -1}).limit(10)
```

Expected Results:
- [ ] Events exist for user registration
- [ ] Events have routing keys like:
  - [ ] `user.registered.v1`
  - [ ] `user.profile.updated.v1`
  - [ ] `user.password.changed.v1`
  - [ ] `user.role.changed.v1`
  - [ ] `user.suspended.v1`
- [ ] Events have `IsProcessed: true` or `false`
- [ ] Events have payload data

**Test Case 17.2: RabbitMQ Message Publishing**

Access RabbitMQ Management UI: http://localhost:15672

Navigate to Exchanges → user.events → Publish rates

Verify:
- [ ] Messages published to exchange
- [ ] Message rate corresponds to user actions

**Test Case 17.3: Create Test Queue to Receive Events**

In RabbitMQ Management UI:
1. Go to Queues → Add queue
   - [ ] Create queue: `test-user-events`
2. Go to Queues → test-user-events → Bindings
   - [ ] Add binding to `user.events` exchange
   - [ ] Routing key: `user.#` (receives all user events)
3. Perform some user actions (register, update profile)
4. Check queue for messages

Expected Results:
- [ ] Messages appear in queue
- [ ] Messages contain event data (userId, eventType, timestamp, payload)

### 18. JWT Token Verification

**Test Case 18.1: Token Structure**

Decode the JWT token using jwt.io or:
```bash
echo "$JWT_TOKEN" | cut -d. -f2 | base64 -d 2>/dev/null | jq
```

Verify token contains:
- [ ] `sub` claim (userId)
- [ ] `email` claim
- [ ] `role` claim
- [ ] `userId` custom claim
- [ ] `jti` claim (unique token ID)
- [ ] `iss` claim: "PetAdoption.UserService"
- [ ] `aud` claim: "PetAdoption.Services"
- [ ] `exp` claim (expiration ~1 hour from issued)

**Test Case 18.2: Token Expiration**

Set system time forward or wait 1 hour, then:
```bash
curl -X GET http://localhost:5001/api/users/me \
  -H "Authorization: Bearer $JWT_TOKEN"
```

Expected Response:
- [ ] HTTP 401 Unauthorized
- [ ] Error about expired token

### 19. Security Verification

**Test Case 19.1: CORS (if applicable)**
```bash
curl -X OPTIONS http://localhost:5001/api/users/me \
  -H "Origin: http://example.com" \
  -H "Access-Control-Request-Method: GET"
```

**Test Case 19.2: SQL Injection Attempts** (Should be protected by MongoDB)
```bash
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@example.com'\'' OR 1=1--",
    "password": "anything"
  }'
```

Expected Response:
- [ ] HTTP 401 or 400
- [ ] No unauthorized access

**Test Case 19.3: Password Security**
- [ ] Passwords are hashed with BCrypt
- [ ] Work factor is 12 or higher
- [ ] Same password produces different hashes (due to salt)

### 20. Error Handling

**Test Case 20.1: Invalid JSON**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d 'invalid-json'
```

Expected Response:
- [ ] HTTP 400 Bad Request
- [ ] Clear error message

**Test Case 20.2: Missing Required Fields**
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com"
  }'
```

Expected Response:
- [ ] HTTP 400 Bad Request
- [ ] Validation errors for missing fields

**Test Case 20.3: Service Unavailable Scenarios**

Stop MongoDB:
```bash
docker-compose stop mongodb
```

Try to register:
```bash
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "fullName": "Test",
    "password": "Pass123!"
  }'
```

Expected Response:
- [ ] HTTP 500 or 503
- [ ] Error message about service unavailability

Restart MongoDB:
```bash
docker-compose start mongodb
```

## Performance & Load Testing (Optional)

### 21. Basic Load Testing

**Test Case 21.1: Concurrent Registrations**
```bash
# Using Apache Bench or similar tool
ab -n 100 -c 10 -T 'application/json' \
  -p register.json \
  http://localhost:5001/api/users/register
```

Monitor:
- [ ] Response times
- [ ] Success rate
- [ ] No duplicate users created
- [ ] Database consistency

**Test Case 21.2: Concurrent Logins**
```bash
ab -n 100 -c 10 -T 'application/json' \
  -p login.json \
  http://localhost:5001/api/users/login
```

Monitor:
- [ ] Response times
- [ ] All tokens valid
- [ ] No authentication errors

## Cleanup & Reset

### 22. Service Restart Verification

**Test Case 22.1: Restart Service**
```bash
docker-compose restart userservice
```

Wait for service to start, then:
```bash
# Try logging in with existing credentials
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "NewSecurePass456!"
  }'
```

Expected Results:
- [ ] Service starts successfully
- [ ] Data persisted (can login with existing user)
- [ ] No data loss

**Test Case 22.2: Full Stack Restart**
```bash
docker-compose down
docker-compose up -d
```

Verify:
- [ ] All services start
- [ ] Data persisted in volumes
- [ ] Service fully functional

### 23. Cleanup

**Test Case 23.1: Stop Services**
```bash
docker-compose down
```

**Test Case 23.2: Remove Volumes (if needed)**
```bash
docker-compose down -v
```

## Summary Checklist

### Infrastructure
- [ ] Docker environment working
- [ ] MongoDB connection successful
- [ ] RabbitMQ connection successful
- [ ] All containers healthy

### Public Endpoints
- [ ] User registration working
- [ ] Email uniqueness enforced
- [ ] User login working
- [ ] JWT token generated correctly
- [ ] Invalid credentials rejected

### Authenticated Endpoints
- [ ] Get user profile working
- [ ] Update user profile working
- [ ] Change password working
- [ ] Token validation working
- [ ] Unauthorized access blocked

### Admin Endpoints
- [ ] List users (admin only)
- [ ] Get user by ID (admin only)
- [ ] Suspend user (admin only)
- [ ] Promote to admin (admin only)
- [ ] RBAC enforced correctly

### Security
- [ ] Passwords hashed with BCrypt
- [ ] JWT tokens properly signed
- [ ] Role-based access control working
- [ ] Suspended users blocked
- [ ] Token expiration working

### Data & Events
- [ ] Data persisted in MongoDB
- [ ] Events stored in Outbox
- [ ] Events published to RabbitMQ
- [ ] Data survives restart
- [ ] No data corruption

### Error Handling
- [ ] Invalid input rejected
- [ ] Clear error messages
- [ ] Graceful degradation
- [ ] No stack traces exposed

---

## Testing Tools

Recommended tools for verification:
- **curl** - Command-line HTTP client
- **Postman** - API testing GUI
- **MongoDB Compass** - MongoDB GUI client
- **mongosh** - MongoDB shell
- **jq** - JSON processor
- **jwt.io** - JWT decoder
- **Apache Bench (ab)** - Load testing
- **Artillery** - Advanced load testing

## Success Criteria

The UserService is considered fully functional when:
- ✅ All infrastructure components running
- ✅ All public endpoints working correctly
- ✅ All authenticated endpoints working correctly
- ✅ All admin endpoints working correctly
- ✅ RBAC properly enforced
- ✅ Data persisted and retrieved correctly
- ✅ Events published to RabbitMQ
- ✅ Security measures in place
- ✅ Error handling appropriate
- ✅ Service survives restart

**Total Checks: 100+**
