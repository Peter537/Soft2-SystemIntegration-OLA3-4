# Tactical Design — Mobile Services Integration

This is a detailed tactical design for mobile services that integrate with existing MyTrailer systems. It focuses on mobile-specific aggregates, integration adapters, and event handling patterns.

## Mobile-Specific Aggregates

### Mobile Booking Session Aggregate (Mobile Booking Context)
- **Aggregate Root:** MobileBookingSession
  - Id: SessionId (GUID)
  - CustomerId
  - DeviceId
  - GPSLocation (value object)
  - SearchCriteria (trailer preferences)
  - SessionState: Browsing | Selecting | Booking | Completed
  - SelectedTrailerRef (nullable)
  - BookingRef (nullable, from existing system)
  
- **Domain Rules:**
  - Session expires after 30 minutes of inactivity
  - GPS location required for trailer search
  - Only one active session per customer/device

### Mobile Notification Aggregate (Mobile Notification Context)
- **Aggregate Root:** MobileNotification
  - Id: NotificationId (GUID)
  - CustomerId
  - DeviceTokens[]
  - Type: BookingConfirmation | PaymentAlert | LocationReminder
  - Status: Pending | Sent | Failed
  - Content (localized)

## Integration Adapters (Not Aggregates)

### Existing System Integration Points
- **IInventoryApiClient** - calls existing inventory API for trailer availability
- **IBookingApiClient** - calls existing booking API for booking creation/management  
- **IPaymentApiClient** - calls existing payment API for insurance/fee processing
- **ICustomerApiClient** - calls existing customer API for profile data

## Value Objects (Mobile-Specific)
- **GPSLocation** { Latitude, Longitude, Accuracy, Timestamp }
- **MobileDevice** { DeviceId, Platform, PushToken }
- **SearchRadius** { CenterLocation, RadiusKm }
- **BookingPreferences** { MaxDistance, PriceRange, Features[] }

## Mobile Repositories (for Mobile Data Only)
- **IMobileSessionRepository**
  - GetActiveSession(CustomerId, DeviceId)
  - SaveSession(MobileBookingSession)
  - ExpireSessions(TimeSpan)

- **IMobileNotificationRepository**
  - GetPendingNotifications()
  - SaveNotification(MobileNotification)
  - MarkAsSent(NotificationId)

## Domain Services (Mobile-Specific)
- **LocationService** - GPS-based trailer discovery using existing inventory data
- **MobileSessionManager** - manages mobile booking session lifecycle
- **PushNotificationService** *(?) Inferred* - handles mobile push notifications
- **OfflineDataService** - manages mobile app offline capabilities

## Mobile-Specific Domain Events (aligned with OLA3)
- **MobileBookingCreated** { SessionId, BookingRef, GPSLocation, DeviceId }
- **LocationQueried** { CustomerId, GPSLocation, SearchRadius, ResultCount }
- **MobileInsurancePurchased** { SessionId, BookingRef, Amount }
- **MobileBookingCancelled** { SessionId, BookingRef, Reason }
- **MobileSessionStarted** { SessionId, CustomerId, DeviceId, GPSLocation }
- **MobileSessionExpired** { SessionId, CustomerId }
- **MobileNotificationSent** *(?) Inferred* { NotificationId, CustomerId, Type, Status }

## External Events Subscribed From Existing Systems
- **BookingConfirmed** { BookingId, CustomerId } → Trigger mobile confirmation notification
- **PaymentProcessed** { PaymentId, BookingId, Status } → Update mobile payment status
- **TrailerStatusChanged** { TrailerRef, NewStatus } → Update mobile availability display

## Process Managers / Sagas (Mobile-Specific)

### Mobile Booking Flow (Mobile Booking Context)
- **Starts on:** CreateMobileBooking command
- **Steps:** 
  1. Validate mobile session and GPS location
  2. Call existing Inventory API to check availability
  3. Call existing Booking API to create booking
  4. If insurance requested → call existing Payment API
  5. Publish MobileBookingCreated event for mobile notifications
- **Compensation:** If any step fails → expire mobile session and notify user

### Mobile Notification Flow (Mobile Notification Context) *(?) Inferred*
- **Starts on:** External system events (BookingConfirmed, PaymentProcessed)
- **Steps:**
  1. Determine mobile notification requirements
  2. Format mobile-specific message content
  3. Send push notification via mobile platform APIs
  4. Track delivery status
- **Compensation:** If delivery fails → retry with backoff, fallback to email

## Integration and Event Architecture

### Event Bus Integration
- Mobile services connect to existing event bus (RabbitMQ/Azure Service Bus)
- **Mobile Services Publish:** MobileBookingCreated, LocationQueried, MobileSessionStarted
- **Mobile Services Subscribe:** BookingConfirmed, PaymentProcessed, TrailerStatusChanged
- **Existing Systems Publish:** BookingConfirmed, PaymentProcessed, TrailerStatusChanged (no changes to existing systems)

### API Integration Patterns
- **Mobile → Existing APIs:** Customer/Supplier pattern with anti-corruption layer
- **Inventory Integration:** Read-only GPS-based queries to existing inventory API
- **Booking Integration:** Create bookings via existing booking API, mobile services don't own booking state
- **Payment Integration:** Route insurance/fee processing through existing payment API
- **Error Handling:** Circuit breaker pattern for existing API calls, graceful degradation

## Notes for C# Implementation (OLA4)

### Integration-Focused Architecture
- **Mobile Aggregates:** Focus on mobile-specific concerns (sessions, notifications, GPS)
- **Integration Adapters:** HTTP clients for existing APIs with retry policies
- **Event Handlers:** React to existing system events for mobile-specific processing
- **Anti-Corruption Layer:** Protect mobile domain from existing system changes

### Key Integration Patterns
- **API Gateway Pattern:** Single entry point for mobile app requests
- **Backend for Frontend (BFF):** Mobile-optimized API responses
- **Event-Driven Integration:** Asynchronous communication with existing systems
- **Circuit Breaker:** Resilience for existing API dependencies

### Technology Stack for Integration
- **Integration Layer:** HttpClient with Polly for resilience, Refit for type-safe API clients
- **Event Handling:** MassTransit for event bus integration with existing systems
- **Mobile Data:** EF Core for mobile-specific data (sessions, preferences, cache)
- **API Layer:** ASP.NET Core with mobile-optimized endpoints
