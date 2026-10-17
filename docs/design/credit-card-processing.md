# Decision: credit card processing fields

Status: decided. **The server does not read, accept or write card data.**

## Context
qbXML can carry card-processing data: `CreditCardTxnInfo` on payments and sales receipts (input: card number, expiry, name, address; result: authorization code, merchant account, transaction IDs), and saved card details on customers (`CreditCardInfo`). Exposing these to an LLM tool would put the server (and the model's context, logs and transcripts) in scope for card-data rules such as PCI DSS.

## Decision
- **Writes:** `CreditCardTxn*`, `CreditCardInfo` and `CreditCardNumber` fields are not accepted by any create or update tool. They are also left out of `describe_write_fields`. A request that includes one is refused with a message saying why. Card payments can still be recorded as ordinary payments and deposits, without card details.
- **Reads:** the same blocks, plus bank and card numbers, are masked by default redaction (on unless `QB_REDACT=false`). Even with redaction off, nothing in the server asks QuickBooks to process a card.
- **Processing:** the server never triggers card authorizations or charges.
- **Not an accident to fix later:** adding any of this needs a new decision, a threat model, and a separate opt-in. Accepting the risk is the user's call, not a default.

## Why not make it optional now
Optional still means the code path exists, is tested, and is reachable by a prompt-injected model. The value (typing card numbers into a chat) is low and the downside (card data in logs and transcripts) is high. Users who need card processing already have QuickBooks Payments and merchant tools built for it.

## Where it is enforced
`WriteSchema` (writes) and `RedactingQbClient` (reads), with tests for both.
