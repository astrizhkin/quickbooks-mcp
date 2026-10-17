# Reports

Every report is available through `get_report` with `reportType` set to a name below (case, spaces and `&` are ignored).
Named shortcuts exist for the common ones: `get_profit_and_loss`, `get_balance_sheet`, `get_trial_balance`,
`get_general_ledger`, `get_aged_receivables`, `get_aged_payables`.

Results are tables: `columns` (id, title, type) and `rows` (`kind` is data, subtotal, total or text; `cells` line up with the columns).

## Options
| Option | Meaning |
|---|---|
| `dateMacro` | All, Today, ThisWeek, ThisWeekToDate, ThisMonth, ThisMonthToDate, ThisQuarter, ThisQuarterToDate, ThisYear, ThisYearToDate, Yesterday, LastWeek, LastWeekToDate, LastMonth, LastMonthToDate, LastQuarter, LastQuarterToDate, LastYear, LastYearToDate, NextWeek, NextFourWeeks, NextMonth, NextQuarter, NextYear |
| `from`, `to` | Explicit period, yyyy-MM-dd (instead of a macro) |
| `basis` | Accrual or Cash |
| `summarizeColumnsBy` | Column grouping, e.g. Month, Quarter, Year, Customer, Class, Vendor, TotalOnly |
| `summarizeRowsBy` | Custom reports: row grouping |
| `includeColumns` | Detail and custom reports: extra columns |
| `accounts`, `names`, `items`, `classes` | Filters by full name |
| `postingStatus` | Posting, NonPosting or Either |

## Aging (`AgingReportQueryRq`)
ARAgingSummary, ARAgingDetail, APAgingSummary, APAgingDetail, CollectionsReport

## General summary
BalanceSheetByClass, BalanceSheetPrevYearComp, BalanceSheetStandard, BalanceSheetSummary, CustomerBalanceSummary, ExpenseByVendorSummary, IncomeByCustomerSummary, InventoryStockStatusByItem, InventoryStockStatusByVendor, IncomeTaxSummary, InventoryValuationSummary, InventoryValuationSummaryBySite, LotNumberInStockBySite, PhysicalInventoryWorksheet, ProfitAndLossByClass, ProfitAndLossByJob, ProfitAndLossPrevYearComp, ProfitAndLossStandard, ProfitAndLossYTDComp, PurchaseByItemSummary, PurchaseByVendorSummary, SalesByCustomerSummary, SalesByItemSummary, SalesByRepSummary, SalesTaxLiability, SalesTaxRevenueSummary, SerialNumberInStockBySite, TrialBalance, VendorBalanceSummary

## General detail
1099Detail, AuditTrail, BalanceSheetDetail, CheckDetail, CustomerBalanceDetail, DepositDetail, EstimatesByJob, ExpenseByVendorDetail, GeneralLedger, IncomeByCustomerDetail, IncomeTaxDetail, InventoryValuationDetail, JobProgressInvoicesVsEstimates, Journal, MissingChecks, OpenInvoices, OpenPOs, OpenPOsByJob, OpenSalesOrderByCustomer, OpenSalesOrderByItem, PendingSales, ProfitAndLossDetail, PurchaseByItemDetail, PurchaseByVendorDetail, SalesByCustomerDetail, SalesByItemDetail, SalesByRepDetail, TxnDetailByAccount, TxnListByCustomer, TxnListByDate, TxnListByVendor, UnpaidBillsDetail, UnbilledCostsByJob, VendorBalanceDetail

## Job
ItemEstimatesVsActuals, ItemProfitability, JobEstimatesVsActualsDetail, JobEstimatesVsActualsSummary, JobProfitabilityDetail, JobProfitabilitySummary

## Time
TimeByItem, TimeByJobDetail, TimeByJobSummary, TimeByName

## Budget
BalanceSheetBudgetOverview, BalanceSheetBudgetVsActual, ProfitAndLossBudgetOverview, ProfitAndLossBudgetPerformance, ProfitAndLossBudgetVsActual

## Custom
CustomSummary (use `summarizeColumnsBy` and `summarizeRowsBy`), CustomTxnDetail (use `includeColumns`)

Not available: a cash-flow statement (QuickBooks' SDK has no such report type) and payroll reports (see the payroll tools).
