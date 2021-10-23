# Msape

My system simplified version of [M-Pesa](https://en.wikipedia.org/wiki/M-Pesa) built on Azure. Mpesa is a mobile money system that is very popular in Kenya.

The name Msape is a sheng synonym to Mpesa in some parts of Kenya.

## Project Objectives

### Main Goals

1. Learn the financial flows needed to implement the system. I have tried learning double entry accounting for a while now and it would be nice to see how to implement that in software.
2. Attempt to build the system using [Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/) rather than using an RDBMS; this will throw some interesting challenges
3. Observerbility

### Nice to haves

1. Side by side deployments
2. [AKS](https://azure.microsoft.com/en-us/services/kubernetes-service/)

## The simplified Msape Domain

In our simplified domain we will have the following personas:

1. **Enterprise**: The organization that owns and hosts/runs the Msape platform. There is only 1 enterprise organization.
2. **Customer**: Individuals end users of the platform they are the one's who mostly make payments on the system. They are also the only personas that can *SendMoney*.
3. **Agents**: Agents are organizations where Customers access *Deposit & Withdrawal services*. They earn commission for such transactions.
4. **Cashier Service Providers**: Organizations that offer services that isn't tied to a specific party/account in the provider organization. They accept payment without the need of a reference (e.g. account #, order #, etc.). Customers make payment to these providers using the *PayCashier* transaction.
5. **Cash Collection Service Provider**: Organizations that offer services tied ot a specified party/account in the provider organization. Payments to them must include a reference number (e.g. account #, order #, etc.). Customers make payment to these providers using the *PayBill* transaction.

### Supported Transaction Types

1. **Agent Float Deposit** (`AgentFloatDeposit`): Agents need to deposit `float` with the enterprise, this float is what is used to support the other transactions on the system.
2. **Customer Cash Deposit** (`CustomerCashDeposit`): Customer will deposit cash into his account at a given agent.
3. **Customer Send Money** (`CustomerSendMoney`): Customer sends money from their account into another customer's account
4. **Customer Cash Withdrawal** (`CustomerCashWithdrawal`): Customer can get cash from their account at a given agent.
5. **Pay Cashier** (`PayCashier`): Customer pays to a specific cashier/register/till
6. **Pay Bill** (`PayBill`): Customer pays to a specific cash collection service provider
7. **Transaction Charge** (`TransactionCharge`): Charges made to accounts for transaction services provided on the platform.
8. **Agent Commission Payment** (`AgentCommissionPayment`): Payment made to agents for services they provide to our end-users.
10. **Organization To 3rd Party Payment** (`OrgTo3rdPartyPayment`): Payment made from an organization to a customer. I have no good name for it :), on M-Pesa this is known as salary payment.

## The party model

I will assume there is another system that manages the parties (organization & people) and will assume that another system is responsible for managing the party information. To be autonomous the system will however track:

1. The party id
2. The party name - for confirmation and alerts
3. The party identifier document (passport #, id card) - for authorization/validation
4. The phone number - used to identify the account

The system will provide a HTTP api to update the data (One could build an integration layer with events that calls into the API if necessary).

[Martin Fowler](https://martinfowler.com/) has good documentation on how to implement party roles [here](https://martinfowler.com/apsupp/accountability.pdf)

## The accounting model

### What accounts are there?

<table>
  <thead>
    <tr><td>Persona</td><td>Account Name</td><td>Account Type</td><td>Has Phone #</td><td>Description</td></tr>
  </thead>
  <tbody>
    <tr><td>Enterprise</td><td>Agent Float Held</td><td>Asset</td><td>false</td><td>Account that tracks the amount of cash in terms of agent deposits we hold</td></tr>
    <tr><td>Enterprise</td><td>Transaction Fees Revenue</td><td>Equity</td><td>false</td><td>The account that tracks fees charged</td></tr>
    <tr><td>Agent</td><td>Agent Float Account</td><td>Liability</td><td>true</td><td>Per Agent account that tracks the amount of money currently held on the platform</td></tr>
    <tr><td>Agent</td><td>Agent Commission Account</td><td>Liability</td><td>false</td><td>Per Agent account the tracks the amount of commission earned so far</td></tr>
    <tr><td>Cashier</td><td>Cashier Account</td><td>Liability</td><td>false</td><td>Per Organization cashier account, An org can have multiple such accounts</td></tr>
    <tr><td>Cash Collection</td><td>Cash Collection Account</td><td>Liability</td><td>false</td><td>Per Organization cash collection account, an org can have multiple</td></tr>
    <tr><td>Any Organization (!Agents)</td><td>Org Payment Account</td><td>Liability</td><td>false</td><td>Per organization account that orgs can use to make payments to other parties</td></tr>
    <tr><td>Customer</td><td>Customer Account</td><td>Liability</td><td>true</td><td>Per customer account that tracks the cash held by the customer on the platform</td></tr>
  </tbody>
</table>

*Phone numbers is used to send SMS notifications in almost all cases but the table above indicates accounts that MUST be attached to a given phone number. For accounts that require a phone number, the phone number is also the fixed notification number and it can't be changed; you will need to create a new account for that*

### What flows are allowed?

<table>
  <thead>
    <tr><td>Transaction Type</td><td>Parent Transaction Type</td><td>Debit Account</td><td>Credit Account</td></tr>
  </thead>
  <tbody>
    <tr><td>AgentFloatDeposit</td><td>&nbsp;</td><td>Enterprise Cash Account</td><td>Agent Float Account</td></tr>
    <tr><td>CustomerCashDeposit</td><td>&nbsp;</td><td>Agent Float Account</td><td>Customer Account</td></tr>
    <tr><td>CustomerSendMoney</td><td>&nbsp;</td><td>Customer Account</td><td>Customer Account</td></tr>
    <tr><td>CustomerCashWithdrawal</td><td>&nbsp;</td><td>Customer Account</td><td>Agent Account</td></tr>
    <tr><td>PayCashier</td><td>&nbsp;</td><td>Customer Account</td><td>Cashier Account</td></tr>
    <tr><td>PayBill</td><td>&nbsp;</td><td>Customer Account</td><td>Pay Bill Account</td></tr>
    <tr><td>OrgTo3rdPartyPayment</td><td>&nbsp;</td><td>Org Payment Account</td><td>Customer Account</td></tr>
    <tr><td>Transaction Fees</td><td>CustomerSendMoney|CustomerCashWithdrawal|PayCashier|PayBill</td><td>Customer Account</td><td>Tx Fees Account</td></tr>
    <tr><td>Transaction Fees</td><td>OrgTo3rdPartyPayment</td><td>Org Payment Account</td><td>Tx Fees Account</td></tr>
    <tr><td>Agent Commission</td><td>CustomerCashWithdrawal</td><td>Customer Account</td><td>Agent Commission Account</td></tr>
  </tbody>
</table>
