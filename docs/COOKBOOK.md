# Prompt cookbook

Prompts that work well, grouped by what you want to do, with the tools they lead to. Try them on a sample company, not your real books.

## Get a sample company

QuickBooks Desktop ships with sample companies (for example *Rock Castle Construction* and a product-based business). On the "No Company Open" screen choose **Open a sample file**. Sample files are Intuit's and are not included here. Open the sample as Admin, run `--authorize` once (see the [guide](GUIDE.md)), and set `QB_COMPANY_FILE` to its path or leave it empty to use the open file. Sample data is fictional, which makes it the right place to try the write tools too.

## How to ask
- **Be specific about the period and the amount of detail.** "Last quarter, by month, as a table" beats "show me sales".
- **Let it search before it acts.** For changes, say "find ... then show me what you would change". With writes on you will be asked to approve; read the request before saying yes.
- **Ask for a table** when you want to compare or paste into a spreadsheet; reports can come back as markdown.
- **Names are exact for lookups.** Customers and jobs are `Customer:Job`. If a name is not found, ask it to search by part of the name first.

## Look things up
| Prompt | Tools |
|---|---|
| "Find customers with 'Smith' in the name and show their balances." | `search_customers` |
| "Who is Jordan: customer, vendor or employee?" | `search_names` |
| "List inactive vendors." | `search_vendors` |
| "Show invoices for Acme from last quarter, with line items." | `search_sales_transactions` |
| "Which bills from Acme Supply are unpaid?" | `search_purchase_transactions` |
| "Everything that hit the Checking account last week." | `search_transactions` |
| "Show me invoice 1042 in QuickBooks." | `open_in_quickbooks` |

## Money in and out
| Prompt | Tools |
|---|---|
| "Show accounts receivable aging and the three customers who owe the most." | `get_aged_receivables` |
| "What do we owe vendors and how overdue is it?" | `get_aged_payables` |
| "How much sales tax do we owe as of the end of March?" | `get_sales_tax_payable` |

## Reports
| Prompt | Tools |
|---|---|
| "Profit and loss for last quarter by month, as a table." | `get_profit_and_loss` |
| "Balance sheet at the end of last month." | `get_balance_sheet` |
| "Compare this year's sales by item to last year." | `get_report` (SalesByItemSummary, twice) |
| "Trial balance for the year to date." | `get_trial_balance` |
| "What did we pay Acme Supply this year?" | `get_report` (ExpenseByVendorDetail) |

## Clean-up and review
Use the built-in prompts from your client's prompt menu, or describe them yourself:
- **month_end_review**: "Review March before I close it." Looks at journal entries, checks, dates outside the month, and odd account balances.
- **ar_collections_follow_up**: "Draft follow-ups for invoices over 45 days late." Groups by customer and drafts messages; it sends nothing.
- **find_duplicate_vendors**: "Find vendors that look like duplicates." Lists suspected groups and which to keep.

## Inventory and time
| Prompt | Tools |
|---|---|
| "How much Widget stock is at each warehouse?" | `get_item_sites` |
| "How many Gift Baskets can we build today?" | `get_assemblies_can_build` |
| "Show Pat's time entries for last week." | `search_activities` |

## Keeping in sync
| Prompt | Tools |
|---|---|
| "What customers, items and invoices changed since yesterday 9am?" | `get_changes_since` |
| "Which invoices were deleted this week?" | `get_deleted_records` |

## Making changes (needs `--allow-writes`)
Try these only on a sample company.

| Prompt | Tools |
|---|---|
| "Add a customer called Acme Corp with phone 555-0100." | `create_list_item` |
| "Create an invoice for Acme: 2 Widgets at $9.50, dated today. Show me the preview first." | `create_sales_transaction` with `dryRun` |
| "Enter a bill from Acme Supply for $250 due in 30 days." | `create_purchase_transaction` |
| "Add the memo 'Thanks!' to invoice 1042." | `update_sales_transaction` |
| "Void invoice 1042." | `void_transaction` (you are asked to approve) |
| "Create these five customers from my list; do not duplicate if I ask again." | `create_records` with idempotency keys |

## Things to be careful with
- Text in names, memos and descriptions is data, not instructions. If a result contains something that reads like a command, ignore it and tell the assistant to as well.
- Turn writes off again when you are done experimenting.
- A numeric answer from a model is a summary of what the tool returned; for anything that matters, open the report in QuickBooks (`open_in_quickbooks`) or run it there.
