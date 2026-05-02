# In-App Chat (Adopter ↔ Shelter) — Design Spec

**Date:** 2026-05-02  
**Status:** Approved  
**Scope:** PetService backend (new aggregate + REST endpoints + SignalR hub) + Blazor WASM frontend (new component + SignalR client + nav badges)

---

## Overview

A 1-to-1 (logically) chat thread per `AdoptionRequest`, between the adopter (the request's `UserId`) and any org member with `Admin`/`Moderator` role at the request's `OrganizationId`. Messages are persisted in PetService and pushed in real time via SignalR. The thread is embedded in the existing adoption request detail views on both sides. The thread auto-closes (read-only) when the request reaches a terminal state (`Approved` + pet adopted, `Rejected`, `Cancelled`).

---

## Architecture

Clean Architecture, full CQRS path. New aggregate in `PetService`, plus a SignalR hub colocated with the REST API:

```
ChatController (REST: history, send, mark-read)
  └── IChatRepository (write) + IChatQueryStore (read)
        └── ChatMessage aggregate

ChatHub @ /hubs/chat (real-time push only)
  └── delegates writes through the same Application-layer SendMessageCommand
```

PetService does **not** currently use SignalR — `Microsoft.AspNetCore.SignalR` is added to the API project, mapped in `Program.cs` after `MapControllers()`, and reuses the existing JWT bearer auth (with the `OnMessageReceived` event wired to read the access token from the `?access_token=` query string for hub URLs only).

---

## Domain changes (PetService)

### `ChatMessage` aggregate (new)

```csharp
public class ChatMessage : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid AdoptionRequestId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public ChatSenderRole SenderRole { get; private set; }   // Adopter | Shelter
    public string Body { get; private set; }                 // 1–2000 chars, trimmed
    public DateTime SentAt { get; private set; }
    public DateTime? ReadByRecipientAt { get; private set; }

    public static ChatMessage Send(AdoptionRequest request, Guid senderUserId,
                                   ChatSenderRole role, string body);
    public void MarkRead(DateTime when);
}
```

- `Send` validates: request is **not** in a terminal state (`Approved` with pet `Adopted`, `Rejected`, `Cancelled`) → throws `DomainException(ChatThreadClosed)`.
- `Body` value rules enforced in factory: trim, non-empty, max 2000 chars → `DomainException(InvalidChatMessageBody)`.
- No domain events on the aggregate itself; the cross-cutting `ChatMessageSentEvent` is appended to the outbox by the command handler (see Cross-cutting).

### Authorization rule (Application layer)

A small helper `IChatAuthorizationService.AuthorizeAsync(adoptionRequestId, currentUserId, currentRole, currentOrgId)` returns `(allowed, senderRole)`:

- **Adopter path**: `currentUserId == request.UserId` → `senderRole = Adopter`.
- **Shelter path**: `currentRole ∈ {Admin, Moderator}` AND `currentOrgId == request.OrganizationId` → `senderRole = Shelter`.
- Anything else → 403.

This same check gates REST send/history/mark-read AND `ChatHub.JoinThread`.

### New error codes

| Code | HTTP |
|------|------|
| `chat_thread_closed` | 409 |
| `invalid_chat_message_body` | 400 |
| `chat_access_denied` | 403 |

---

## Backend — REST endpoints

`ChatController` at `/api/adoption-requests/{requestId:guid}/messages`:

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/adoption-requests/{id}/messages?afterId=&take=` | Authenticated + thread participant | Paginated history, ascending `SentAt`. `take` clamped to 1–100 (default 50). `afterId` is a keyset cursor (the message id; handler resolves to its `SentAt` and returns rows strictly after). |
| POST | `/api/adoption-requests/{id}/messages` | Authenticated + thread participant | Body `{ Body }`. Returns the created `ChatMessageDto`. Server fans out via SignalR to thread group. |
| POST | `/api/adoption-requests/{id}/messages/mark-read` | Authenticated + thread participant | Marks all incoming (other-party) messages as read for caller. Returns count marked. |

All three resolve sender role via `IChatAuthorizationService` and reject closed threads with `409 chat_thread_closed`. Read history is allowed even on closed threads (read-only mode).

### Handlers / queries

- `SendChatMessageCommand` → mediator handler → loads `AdoptionRequest`, calls `ChatMessage.Send`, persists via `IChatRepository`, appends `ChatMessageSentEvent` to outbox, returns DTO.
- `GetChatHistoryQuery` → `IChatQueryStore.GetHistoryAsync(requestId, afterId?, take)`.
- `GetUnreadCountsQuery` → `IChatQueryStore.GetUnreadCountsAsync(userId, requestIds[])` returning `IReadOnlyDictionary<Guid, int>` for badge rendering on list pages.
- `MarkChatThreadReadCommand` → bulk update `ReadByRecipientAt = UtcNow WHERE AdoptionRequestId == id AND SenderUserId != currentUserId AND ReadByRecipientAt IS NULL`.

### Unread counts

Two new endpoints to feed the navigation badges (single round-trip each):

- `GET /api/me/chat/unread-total` → `{ TotalUnread: int }` for adopter — sums unread across the user's requests.
- `GET /api/organizations/{orgId}/chat/unread-total` → `{ TotalUnread: int }` for org members (uses `OrgAuthorizationFilter`).

The `IChatQueryStore.GetUnreadCountsAsync(...)` method is also called by the existing list endpoints (`GET /api/users/me/adoption-requests`, `GET /api/organizations/{orgId}/adoption-requests`) so per-row unread counts are returned alongside each request — avoids N+1 calls.

---

## Backend — SignalR hub

```csharp
[Authorize]
public class ChatHub : Hub
{
    public Task JoinThread(Guid requestId);     // auth-checks then adds to group $"req:{requestId}"
    public Task LeaveThread(Guid requestId);    // removes from group
    public Task SendMessage(Guid requestId, string body); // delegates to SendChatMessageCommand
}
```

- Mapped at `app.MapHub<ChatHub>("/hubs/chat")` in `Program.cs` (after `MapControllers()`).
- JWT auth: `JwtBearerOptions.Events.OnMessageReceived` reads `context.Request.Query["access_token"]` when the request path starts with `/hubs/`. Browser WebSocket clients can't set an `Authorization` header, so the Blazor client appends the token as a query-string param.
- After a successful send (REST or hub), the server calls `_hubContext.Clients.Group($"req:{requestId}").SendAsync("MessageReceived", dto)` — this is the **only** event the client listens for. REST sends and hub sends produce the same broadcast (single source of truth).
- `JoinThread` performs the same `IChatAuthorizationService.AuthorizeAsync` check before adding to the group; throws `HubException` with `chat_access_denied` on failure.
- No server-pushed presence/typing events in v1.

---

## Frontend (Blazor WASM)

### `ChatPanel.razor` (new component)

`Components/Chat/ChatPanel.razor` — embedded in adoption request detail on both sides. Inputs: `[Parameter] Guid AdoptionRequestId`, `[Parameter] bool ReadOnly`.

Behaviour:

1. On init: `GET /api/adoption-requests/{id}/messages?take=50` → render bottom-anchored list.
2. Build `HubConnection` against `{petApiBaseUrl}/hubs/chat?access_token={jwt}` with `WithAutomaticReconnect()`. Start, then `InvokeAsync("JoinThread", AdoptionRequestId)`.
3. `connection.On<ChatMessageDto>("MessageReceived", msg => …)` — append to list, auto-scroll if user is at bottom.
4. Send: `await PetApi.SendChatMessageAsync(id, body)` (REST POST). The hub broadcast echoes it back via `MessageReceived`; we don't optimistically render to keep one render path.
5. On panel visible + scrolled-to-bottom: fire `POST /messages/mark-read` (debounced 500 ms).
6. On dispose / `LeaveThread` + `StopAsync`.
7. When `ReadOnly` (terminal status): hide composer, show muted footer "This conversation is closed because the adoption request is {Status}."

### Where it's embedded

- **Adopter view**: a new collapsible "Messages" section in `MyAdoptionRequests.razor`'s row-expand panel (or a new `MyAdoptionRequestDetail.razor` page if expansion gets cluttered — implementer's call).
- **Shelter view**: same pattern in `OrgAdoptionRequests.razor`.

Each row in the existing tables renders an unread-count `MudBadge` next to the pet name when `UnreadCount > 0`. The list endpoint already returns this per-row (see backend section).

### Nav badge

`MainLayout.razor` polls `GET /api/me/chat/unread-total` (and the org variant when the user is in an org context) every 60 s as a fallback for users not currently viewing any thread, and decorates the "My Requests" / "Adoption Requests" nav links with a `MudBadge`. Real-time decrement happens implicitly when the user opens a panel and reads.

### API client additions (`PetApiClient`)

```csharp
Task<IReadOnlyList<ChatMessageDto>> GetChatHistoryAsync(Guid requestId, Guid? afterId, int take);
Task<ChatMessageDto> SendChatMessageAsync(Guid requestId, string body);
Task<int> MarkChatThreadReadAsync(Guid requestId);
Task<int> GetMyChatUnreadTotalAsync();
Task<int> GetOrgChatUnreadTotalAsync(Guid orgId);
```

Add `Microsoft.AspNetCore.SignalR.Client` to `PetAdoption.Web.BlazorApp.csproj`.

---

## Cross-cutting

### Outbox event

`ChatMessageSentEvent(MessageId, AdoptionRequestId, SenderUserId, SenderRole, RecipientUserHint, SentAt)` is appended to `OutboxEvents` inside the same transaction as the message insert. Published to RabbitMQ exchange `pet.events` with routing key `chat.message.sent`. The future email-notification feature subscribes downstream — this spec does **not** add a consumer.

`RecipientUserHint` is `null` when the recipient is "any org member" — the consumer resolves the org-member fan-out itself.

---

## Out of scope

- Attachments / images
- Emoji picker
- Read receipts beyond the simple `ReadByRecipientAt` boolean (no per-recipient receipts, no "delivered" state)
- Typing indicators
- Message editing or deletion
- Group chats / multi-adopter threads
- Push notifications (web push, mobile)
- Moderation tools (mute, report, block)
- Search inside threads

---

## Testing

**Unit tests** (`PetService.UnitTests`):
- `ChatMessageTests` — `Send` rejects body too long / empty / whitespace; `Send` rejects on terminal-state requests; `MarkRead` is idempotent.
- `ChatAuthorizationServiceTests` — adopter allowed for own request; non-org user denied; wrong-org admin denied; `User`-role org member denied; right-org `Moderator` allowed.

**Integration tests** (`PetService.IntegrationTests`):
- `ChatControllerTests` — round-trip: shelter sends, adopter reads history; cross-user 403; pagination via `afterId`; `mark-read` flips `ReadByRecipientAt`; sending on a `Rejected` request returns 409 `chat_thread_closed`; history on a closed thread still returns 200.
- Reuses `PetServiceWebAppFactory` and existing `GenerateTestToken` helper.

**SignalR**: no automated e2e in v1 — manual smoke test (open two browsers, verify push). A `ChatHub` unit test against `Mock<IHubCallerClients>` is acceptable but not required.
