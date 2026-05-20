Here's what I found across the codebase:

Current State: PayPal-Only, Single Gateway
The system currently supports one payment gateway at a time (PayPal only). Here's the breakdown:

Architecture Overview
Layer	Key Files
Domain	GatewayProvider.cs enum (PayPal only), PlatformPaymentGateway.cs aggregate
Application	ConfigurePlatformPaymentGatewayHandler.cs, InitiatePaymentHandler.cs
Infrastructure	PayPalWebhookValidator.cs
WebApi	WebhooksController.cs, SuperAdminController.cs
SuperAdmin Controls Gateway Config — But It's PayPal-Specific
The SuperAdmin entity itself has no payment properties. However, SuperAdmin exclusively controls PlatformPaymentGateway configuration via POST /api/superadmin/payment-gateway. The PlatformPaymentGateway aggregate has a unique constraint on is_active — meaning the schema enforces only one active gateway at a time by design.

Tight Coupling Problems for GCash/PayMaya
GatewayProvider.cs — enum only has PayPal. No GCash, PayMaya, or PayMongo.
ConfigurePlatformPaymentGatewayHandler.cs — hardcodes PayPal OAuth token validation. No abstraction.
InitiatePaymentHandler.cs — hardcodes PayPal REST API calls (v1/oauth2/token, v2/checkout/orders). No strategy pattern.
IPayPalWebhookValidator.cs — interface is PayPal-specific, not generic.
WebhooksController.cs — only has /payment/paypal endpoint, only handles CHECKOUT.ORDER.APPROVED and PAYMENT.CAPTURE.COMPLETED events.
Important Context on GCash/PayMaya
Both GCash and PayMaya (Maya) in the Philippines are accessed through PayMongo — a local payment aggregator that acts as the integration layer for both. You don't integrate GCash or Maya directly; PayMongo provides a single unified API that covers both. This means:

GatewayProvider would likely need a PayMongo enum value (not separate GCash/PayMaya values)
PayMongo uses a different auth model (API key, not OAuth client credentials like PayPal)
PayMongo webhooks have a different signature scheme (x-paymongo-signature)
What Would Need to Change (High Level)
Extend GatewayProvider enum with PayMongo
Create a generic IPaymentGatewayService abstraction in Domain/Application with implementations per provider
Refactor ConfigurePlatformPaymentGatewayHandler to dispatch credential validation to the correct provider service
Refactor InitiatePaymentHandler to use a gateway service factory/strategy
Create IWebhookValidator abstraction; add PayMongoWebhookValidator alongside existing PayPalWebhookValidator
Add a /payment/paymongo webhook endpoint in WebhooksController
Add HandlePayMongoWebhookCommand + handler (mirroring the PayPal equivalent)
Migration to add PayMongo to the enum column (if stored as string) or nothing (if int-based)
Want me to draw up a detailed implementation plan before we proceed?