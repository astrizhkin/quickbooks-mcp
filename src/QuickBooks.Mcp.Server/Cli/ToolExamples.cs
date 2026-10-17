namespace QuickBooks.Mcp.Server.Cli;

/// <summary>One example prompt per tool, shown in the generated reference. A test requires an entry for every tool.</summary>
public static class ToolExamples
{
    private static readonly Dictionary<string, string> Prompts = new()
    {
        ["search_activities"] = "Show Pat's time entries for last week.",
        ["create_activity"] = "Log 2.5 hours for Pat on the Acme job today.",
        ["update_activity"] = "Mark the to-do 'Call Acme' as done.",
        ["create_records"] = "Create these five customers from my list, and don't create duplicates if I ask again.",
        ["get_changes_since"] = "What customers, items and invoices changed since yesterday 9am?",
        ["get_deleted_records"] = "Which invoices were deleted this week?",
        ["get_company"] = "What company file am I connected to?",
        ["get_custom_field_definitions"] = "What custom fields do we have on customers?",
        ["get_sales_tax_payable"] = "How much sales tax do we owe each agency as of the end of March?",
        ["create_custom_field_definition"] = "Add a custom text field 'Region' to customers.",
        ["delete_custom_field_definition"] = "Remove the 'Region' custom field.",
        ["delete_custom_field_value"] = "Clear the Region on Acme.",
        ["update_custom_field_value"] = "Set Acme's Region to West.",
        ["delete_record"] = "Delete the duplicate vendor 'Acme Supply (old)'.",
        ["void_transaction"] = "Void invoice 1042.",
        ["get_assemblies_can_build"] = "How many Gift Baskets can we build today?",
        ["get_item_sites"] = "How much Widget stock is at each warehouse?",
        ["search_inventory_sites"] = "List our inventory sites.",
        ["search_inventory_transactions"] = "Show inventory adjustments from last month.",
        ["create_inventory_record"] = "Adjust Widget stock to 40 at the main warehouse.",
        ["update_inventory_record"] = "Change the memo on that inventory adjustment.",
        ["search_items"] = "Find service items with 'install' in the name.",
        ["create_list_item"] = "Add a customer called Acme Corp with phone 555-0100.",
        ["update_list_item"] = "Change Acme's phone number to 555-0199.",
        ["search_customers"] = "Find customers whose name contains 'Smith', with balances.",
        ["search_employees"] = "List active employees.",
        ["search_names"] = "Is 'Jordan' a customer, a vendor or an employee?",
        ["search_other_names"] = "Show my leads.",
        ["search_vendors"] = "Which vendors are inactive?",
        ["get_payroll_report"] = "Payroll summary for last quarter by month.",
        ["search_payroll_items"] = "List the payroll wage items.",
        ["search_purchase_transactions"] = "Show unpaid bills from Acme Supply this year.",
        ["search_transactions"] = "Everything that touched the Checking account last week.",
        ["create_purchase_transaction"] = "Enter a bill from Acme Supply for $250 dated today.",
        ["update_purchase_transaction"] = "Change the due date on bill 7781.",
        ["query_records"] = "List the first 20 classes, only their names.",
        ["get_aged_payables"] = "What do we owe vendors and how overdue is it?",
        ["get_aged_receivables"] = "Show accounts receivable aging, detail by invoice.",
        ["get_balance_sheet"] = "Balance sheet as of the end of last month.",
        ["get_general_ledger"] = "General ledger for the Checking account in March.",
        ["get_profit_and_loss"] = "Profit and loss for last quarter by month, as a table.",
        ["get_report"] = "Sales by item summary for this year.",
        ["get_trial_balance"] = "Trial balance for the year to date.",
        ["search_sales_transactions"] = "Open invoices for Acme from last quarter, with line items.",
        ["create_sales_transaction"] = "Create an invoice for Acme: 2 Widgets at $9.50.",
        ["update_sales_transaction"] = "Add a memo 'Thanks!' to invoice 1042.",
        ["get_preferences"] = "Is multi-currency turned on?",
        ["search_accounts"] = "Show bank accounts and their balances.",
        ["search_setup_lists"] = "List the payment terms.",
        ["describe_write_fields"] = "What fields can I set when creating an invoice?",
        ["open_in_quickbooks"] = "Show me invoice 1042 in QuickBooks.",
    };

    /// <summary>The example prompt for a tool.</summary>
    public static string For(string toolName) => Prompts.GetValueOrDefault(toolName, "(no example yet)");

    /// <summary>True if the tool has an example.</summary>
    public static bool Has(string toolName) => Prompts.ContainsKey(toolName);
}
