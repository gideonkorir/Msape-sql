# Msape

My system simplified version of [M-Pesa](https://en.wikipedia.org/wiki/M-Pesa) built on Azure. Mpesa is a mobile money system that is very popular in Kenya.

The name Msape is a sheng synonym to Mpesa in some parts of Kenya.

## Project Objectives

### Main Goals

1. Learn the financial flows needed to implement the system. I have tried learning double entry accounting for a while now and it would be nice to see how to implement that in software.
2. Build a system that is both scalable and fit for the supported transaction flows. 
3. Attempt to build the following azure services:
   a. [Azure SQL Database](https://azure.microsoft.com/en-us/products/azure-sql/database/#overview) as the main transactional store
   b. [Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/) as the transaction read store and workflow (saga) storage
   c. [Service Bus](https://azure.microsoft.com/en-us/services/service-bus/#overview) as the queueing technology
4. Observerbility

### Nice to haves

1. Side by side deployments
2. [AKS](https://azure.microsoft.com/en-us/services/kubernetes-service/) for hosting
3. [Azure B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/overview) as the user security platform

## More docs

1. The simplified [domain](docs/Domain.md)
2. How transactions are [posted](docs/Posting.md)
