# Tester guide

Thank you for helping. The server has been run against one QuickBooks Desktop (Enterprise Accountant 24.0). Every other edition and year is untested, and a single report from you fills a row in the [compatibility table](COMPATIBILITY.md). It takes about 20 minutes and changes nothing in your books.

## Before you start
- Use **Intuit's sample company file**, never your real books. QuickBooks installs it: File > Open or Restore Company > Open a sample file (any sample works; "product-based business" is the one used so far).
- A Windows PC with QuickBooks Desktop (Pro, Premier or Enterprise, any year) and you can sign in as a QuickBooks **Admin**.
- Leave writes off. This test only reads.

## 1. Install
Download `quickbooks-mcp-win-x64.exe` from the [latest release](https://github.com/devwithmj/quickbooks-mcp/releases). If the next step says only a 32-bit component is registered, download `quickbooks-mcp-win-x86.exe` instead. Put the file somewhere simple such as `C:\Tools`.

## 2. Allow access once
1. Open QuickBooks and the sample file as Admin, in **single-user mode** (File > Switch to Single-user Mode).
2. In a terminal run `C:\Tools\quickbooks-mcp-win-x64.exe --authorize`.
3. In the QuickBooks dialog for "QuickBooks MCP", choose **Yes, always; allow access even if QuickBooks is not running**, pick an Admin user and confirm.

## 3. Run the check
Run `C:\Tools\quickbooks-mcp-win-x64.exe --doctor` and keep the output. It lists the QuickBooks product and the qbXML versions it supports. This is the most useful part of your report.

## 4. Try it with an MCP client
Add the server to an MCP client you already use (the [guide](GUIDE.md) has Claude Desktop, Claude Code, VS Code, Cursor, ChatGPT desktop and Gemini CLI). Set `QB_COMPANY_FILE` to the sample file path. Then ask:

1. "What is in my chart of accounts?"
2. "List my customers."
3. "Show accounts receivable aging."
4. "Profit and loss for last year, by month."
5. "Find the five largest unpaid invoices."

Note which answers matched what QuickBooks shows (Lists > Customer Center, Reports > A/R Aging Summary) and which gave an error.

## 5. Repeat in the other modes (optional, very helpful)
- **Multi-user mode:** switch back (File > Switch to Multi-user Mode) and repeat step 4.
- **QuickBooks closed:** close QuickBooks, then run `--doctor` again. If you chose "allow access even if QuickBooks is not running", it should still connect.

## 6. Send your result
Open an issue with the **Compatibility report** template, or paste this into any issue or message:

```
Edition and year:
Product line from --doctor:
qbXML versions from --doctor (highest):
Bitness (64 or 32) and Windows version:
Single-user, QuickBooks open: pass / partial / fail
Multi-user, QuickBooks open: pass / partial / fail
QuickBooks closed: pass / partial / fail / not tried
Prompts that matched QuickBooks (1-5):
Errors, with the exact text:
```

If something fails, also run `C:\Tools\quickbooks-mcp-win-x64.exe --diagnostics` and paste the output. It has versions and settings and no company data.

## Multi-currency test (optional)
Multi-currency is the least tested part. If you have a company file that already uses it, or a throwaway file you can turn it on in, this adds a useful row to the report.

**Safety first**
- Use a throwaway or sample file, never your real books. Turning multi-currency on in QuickBooks (Edit > Preferences > Multiple Currencies) cannot be undone for that file.
- Leave writes off. Everything below only reads.
- The edition must support it (Premier or Enterprise). On other editions QuickBooks answers with code 3250, which is expected.

**Set up the file** (skip if it already has foreign-currency records)
1. In single-user mode turn multi-currency on and pick a home currency.
2. Add one foreign currency (Lists > Currency List) and note its exchange rate.
3. Create one customer and one vendor in that currency, then one invoice for the customer and one bill from the vendor, each with an amount you will recognise (for example 1,000.00).

**Ask**
1. "Is multi-currency turned on, and what is the home currency?"
2. "List my currencies."
3. "Show the invoice for [the foreign customer]. What currency is it in, and what is the amount in the home currency?"
4. "List my customers" and check the foreign customer shows its currency.
5. "What are my unpaid bills? Add up the totals, and say which currency each is in."
6. "Show accounts receivable aging." Reports are in the home currency, so the foreign invoice should appear at its home-currency value.

**What to check**
- Each foreign record's amount is in its own currency, and any field ending in `InHomeCurrency` matches what QuickBooks shows in the home currency.
- The answer states the home currency and does not add foreign and home amounts together as if they were the same.
- Results that hold foreign records carry a `currency` note with the home currency. Records in the home currency only do not.
- The A/R aging total matches QuickBooks (Reports > Customers & Receivables > A/R Aging Summary).

**Add to your report**
```
Multi-currency on: yes / no
Home currency and foreign currency used:
Invoice amount shown by QuickBooks (foreign / home):
Invoice amount shown by the assistant (foreign / home):
Was the currency named correctly: yes / no
Did the A/R aging total match: yes / no
Errors, with the exact text:
```

## Known limits (not a failure)
Some requests are refused by QuickBooks itself on some editions, for example inventory sites and multi-currency on editions without them. The error text says so (codes 3231 or 3250). Mention them, but they are not bugs.
