# Writing to QuickBooks

Writes are off by default. Enable them with `--allow-writes` (everything), or per kind with `QB_ENABLE_WRITE` (create),
`QB_ENABLE_UPDATE` and `QB_ENABLE_DELETE` (delete and void). `QB_WRITE_ALLOW=Invoice,Customer` limits which entities
can be written. Every write is recorded in a local audit log (`QB_AUDIT_LOG`, no record data).

## Tools
| Tool | Entities |
|---|---|
| `create_list_item`, `update_list_item` | Account, Class, Customer (and jobs), Vendor, Employee, OtherName, Lead, all item types, terms, PaymentMethod, SalesTaxCode, SalesRep, Vehicle, BillingRate, PriceLevel, WorkersCompCode, CustomerType, VendorType, JobType, ShipMethod, CustomerMsg |
| `create_sales_transaction`, `update_sales_transaction` | Invoice, SalesReceipt, CreditMemo, Estimate, SalesOrder, ReceivePayment, ARRefundCreditCard, Charge, Deposit |
| `create_purchase_transaction`, `update_purchase_transaction` | Bill, BillPaymentCheck, BillPaymentCreditCard, PurchaseOrder, ItemReceipt, VendorCredit, Check, CreditCardCharge, CreditCardCredit, SalesTaxPaymentCheck, Transfer, JournalEntry |
| `describe_write_fields` | Lists the field names of any create or update |

`data` is a JSON object whose keys are qbXML element names, in any order (the server orders them as QuickBooks requires,
and refuses unknown names, listing the valid ones). Groups such as `BillAddress` are nested objects. Repeated elements such
as lines are arrays. References are `{"FullName": "..."}` or `{"ListID": "..."}`.

```json
{ "CustomerRef": { "FullName": "Acme" },
  "InvoiceLineAdd": [ { "ItemRef": { "FullName": "Widget" }, "Quantity": 2, "Rate": 9.5 } ] }
```

Update fetches the record's current EditSequence for you and changes only the fields given. To add a line to an existing
transaction, send an `...LineMod` with `TxnLineID` set to `-1`. To link a new invoice to an estimate or sales order, use
`LinkToTxnID` or the line's `LinkToTxn`.

`dryRun: true` shows the exact request and sends nothing. Journal entries are checked for balance before anything is sent.

## Deleting and voiding
`delete_record` and `void_transaction` need `QB_ENABLE_DELETE` (or `--allow-writes`). The first call never changes anything: it returns the
request and a single-use `confirmation_token` valid for five minutes and tied to that exact record. Call again with the token to perform it.
Voiding keeps the transaction in the books at zero; deleting removes it. Prefer voiding.

- **Can be voided:** ARRefundCreditCard, Bill, BillPaymentCheck, BillPaymentCreditCard, Charge, Check, CreditCardCharge, CreditCardCredit, CreditMemo, Deposit, InventoryAdjustment, Invoice, ItemReceipt, JournalEntry, SalesReceipt, VendorCredit
- **Transactions that can be deleted:** ARRefundCreditCard, Bill, BillPaymentCheck, BillPaymentCreditCard, BuildAssembly, Charge, Check, CreditCardCharge, CreditCardCredit, CreditMemo, Deposit, Estimate, InventoryAdjustment, Invoice, ItemReceipt, JournalEntry, PayrollLiabilityAdjustment, PayrollPriorPayment, PayrollYearToDateAdjustment, PurchaseOrder, ReceivePayment, SalesOrder, SalesReceipt, SalesTaxPaymentCheck, TimeTracking, TransferInventory, VehicleMileage, VendorCredit
- **List items that can be deleted:** Account, BillingRate, Class, Currency, Customer, CustomerMsg, CustomerType, DateDrivenTerms, Employee, InventorySite, ItemDiscount, ItemFixedAsset, ItemGroup, ItemInventory, ItemInventoryAssembly, ItemNonInventory, ItemOtherCharge, ItemPayment, ItemSalesTax, ItemSalesTaxGroup, ItemService, ItemSubtotal, JobType, OtherName, PaymentMethod, PayrollItemNonWage, PayrollItemWage, PriceLevel, SalesRep, SalesTaxCode, ShipMethod, StandardTerms, ToDo, UnitOfMeasureSet, Vehicle, Vendor, VendorType, WorkersCompCode

Estimates, sales orders and purchase orders can be deleted but not voided.

## Batches and retries
`create_records` creates up to 50 records in one call (`[{ "entity", "data", "idempotencyKey" }]`). Items run in order, each in its own
short QuickBooks session; a failed item is reported and the rest continue unless `stopOnError` is true.

Every create tool and every batch item accepts an `idempotencyKey`. The first success is remembered (next to the audit log,
`idempotency.jsonl`); sending the same key with the same request again returns the first result instead of creating a second
record, and reusing a key for a different request is refused. Dry runs never use up a key.
