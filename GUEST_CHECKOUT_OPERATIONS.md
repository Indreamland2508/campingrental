# Guest checkout and payment operations

## Local development safety

The application runs EF Core migrations during startup. Before running it locally, point `ConnectionStrings__DefaultConnection` at an isolated development database. Do not run this branch with production connection settings.
In the `Development` environment, startup now rejects PostgreSQL hosts other than `localhost`, `127.0.0.1`, or `::1`. Pass the local connection string through `ConnectionStrings__DefaultConnection`; ASP.NET configuration environment-variable overrides take precedence over JSON settings.

The schema changes are in the migrations under `BAOCAOWEBNANGCAO/Migrations/` (including `20261107133000_HardenGuestCheckout.cs`, `20261107193100_AddPublicOrderAndRentalFields.cs`, and `20261107195200_AddRefundBankDetails.cs`). The application applies pending migrations at startup; only run it against the isolated local database when testing.

## SePay webhook

Configure the SePay webhook to use **HMAC-SHA256** and JSON. Set `SePay__WebhookSecret` in the local/deployment environment to the same secret used in the SePay webhook configuration. Never put the secret in source control. The endpoint rejects requests without a configured secret, verifies the signature against the raw request body and a timestamp within five minutes, and stores each SePay transaction ID once.

Endpoint: `POST /api/SePay/webhook`.

## Email delivery

Order notifications and order-lookup codes use the configured `IEmailSender`. Set `Email__GatewayUrl` to an HTTPS email gateway endpoint if it differs from the existing default. Delivery failures are logged and do not roll back an order or payment update.

## Identity and administrator provisioning

Email confirmation is required to sign in, and new passwords must be at least 12 characters with uppercase, lowercase, numeric, and non-alphanumeric characters (at least four unique characters). Configure email delivery before relying on account registration or password recovery.

The application no longer creates an administrator with a source-coded default password. To bootstrap an administrator on a new database, set `BootstrapAdmin__Email` and `BootstrapAdmin__Password` through User Secrets for local development or the deployment secret manager. Both settings must be provided together; the password must meet the configured Identity policy. The bootstrap user is created only when that email does not already exist. Existing administrator accounts are not modified by the bootstrap process. No bootstrap account is created if the settings are omitted.

Example local setup from the project directory:

```powershell
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com"
dotnet user-secrets set "BootstrapAdmin:Password" "<use-a-unique-strong-password>"
```

Do not use the example email or put real passwords in source control or shared configuration files. Rotate any credentials that were previously used with the old seeded administrator.

## Bank transfer QR configuration

The checkout reads `Payment__BankId`, `Payment__AccountNumber`, and `Payment__AccountName` from configuration. No bank account is committed to source. If these values are missing or invalid, checkout is disabled, no new order is created, and an existing payment page hides the QR and tells the customer not to transfer money. Confirm that the account is authorized for business receipts before setting these values. The QR image is requested from VietQR, so its URL includes the recipient bank/account, amount, and order transfer reference; disclose/assess this provider before production use.

Set the values with User Secrets locally or a deployment secret manager. Do not add a real account number to `appsettings.json` or source control. Configure all three values only after independently verifying the bank account.

```powershell
dotnet user-secrets set "Payment:BankId" "<confirmed-bank-id>"
dotnet user-secrets set "Payment:AccountNumber" "<confirmed-account-number>"
dotnet user-secrets set "Payment:AccountName" "<confirmed-account-holder>"
```

## Reservation and manual cancellation handling

New orders reserve requested quantities for 30 minutes while the required deposit is incomplete. A partial payment restarts that 30-minute window. If it expires before the deposit threshold is met, the order expires and any received amount is recorded for a staff-processed manual refund. Successful deposit payment removes the expiry; canceled or expired orders no longer count against stock. Date ranges use the selected checkout-exclusive rule: the return date is not billed, while same-day rentals count as one day.

Customers can request cancellation after verifying their order by email OTP (10-minute expiry, up to five code attempts). Staff must approve cancellation. Requests made at least three calendar days before pickup are eligible for a refund of the full amount received; requests made less than three days before pickup are not refundable. Staff perform any approved refund outside the application and record its status and transfer reference in the order details.

For an approved refund, the customer returns to Order Tracking, verifies the order by email OTP, and submits the bank name, account number, and account holder through the protected form. Staff transfer the approved amount manually, then record the bank transaction reference in the admin order details. The account details are cleared when the refund is marked complete and are intentionally excluded from JSON backups. SePay is currently configured for incoming payment webhooks only; automatic outgoing refunds require a separate bank/provider payout API integration and credentials.

When an approved rental-date change increases the deposit due, Order Tracking displays the additional deposit amount and transfer reference and links to the QR payment page. The QR amount is the difference between the new deposit requirement and the amount already received. The rental reservation has a 30-minute completion window while that additional amount remains unpaid.

Checkout separately requires consent to the rental policy and the privacy notice. Consent timestamps and the privacy notice version are recorded with new orders. The privacy notice remains a draft: the provided controller/contact details need confirmation, the postal address and hosting/database provider are incomplete, and retention periods and legal wording need professional review before public deployment.

The public rental policy is available at `/Home/RentalPolicy`. Customer support: `0982412005`. Late-return and damage/loss fees have not been configured because no fee schedule has been approved.

## Suggested local verification sequence

1. Use a disposable PostgreSQL database and local-only secrets.
2. Apply the migration by starting the application only after checking its effective connection string.
3. Configure SePay Test Mode with HMAC-SHA256 and JSON, then use its test-send or simulated transaction feature.
4. Verify overlapping rental quantities, unpaid and partial-payment reservation expiry, duplicate webhook delivery, email OTP, and manual cancellation/refund from the admin UI.

Official SePay webhook references:

- https://docs.sepay.vn/tich-hop-webhooks.html
- https://developer.sepay.vn/vi/sepay-webhooks/xac-thuc
