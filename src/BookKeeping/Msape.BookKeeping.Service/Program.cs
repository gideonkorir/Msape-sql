using GreenPipes;
using MassTransit;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Msape.BookKeeping.Components.Consumers;
using Msape.BookKeeping.Components.Consumers.Posting;
using Msape.BookKeeping.Components.Consumers.Posting.Saga;
using Msape.BookKeeping.Components.Infra;
using Msape.BookKeeping.Data;
using System;
using System.Threading.Tasks;

namespace Msape.BookKeeping.Service
{
    class Program
    {
        static async Task Main(string[] args)
        {
            
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((host, services) =>
                {
                    var client = new CosmosClient(host.Configuration.GetConnectionString("Cosmos"), new CosmosClientOptions()
                    {
                        ApplicationName = "msape",
                        ConnectionMode = ConnectionMode.Direct,
                        ConsistencyLevel = ConsistencyLevel.Session,
                        SerializerOptions = new CosmosSerializationOptions()
                        {
                            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                        }
                    });

                    var opts = new PostTransactionStateMachineOptions()
                    {
                        TransactionProcessingSendEndpoint = new Uri("queue:transaction-processing"),
                        AccountTypeSendEndpoint = new Func<AccountType, Uri>(accountType =>
                        {
                            var name = AccountTypeQueueHelper.GetQueueName(accountType);
                            return new Uri($"queue:{name}");
                        })
                    };
                    services.AddSingleton(client);
                    services.AddSingleton<ICosmosAccount>(new CosmosAccount(client, ("msape2", "accounts"), ("msape2", "account_numbers"), ("msape2", "transactions")));
                    services.AddSingleton(opts);

                    services.AddMassTransit(configurator =>
                    {
                        configurator.SetKebabCaseEndpointNameFormatter();
                        configurator.AddConsumersFromNamespaceContaining<Components.StringUtil>();
                        configurator.AddSagaStateMachine<PostTransactionStateMachine, PostTransactionSaga>()
                            .CosmosRepository(config =>
                            {
                                var connBuilder = new System.Data.Common.DbConnectionStringBuilder
                                {
                                    ConnectionString = host.Configuration.GetConnectionString("Cosmos")
                                };
                                config.DatabaseId = "msape2";
                                config.CollectionId = "sagas";
                                config.EndpointUri = connBuilder["AccountEndpoint"].ToString();
                                config.Key = connBuilder["AccountKey"].ToString();
                            });

                        configurator.UsingAzureServiceBus((context, configurator) =>
                        {
                            configurator.Host(host.Configuration.GetConnectionString("ServiceBus"));
                            configurator.UseServiceBusMessageScheduler();
                            configurator.SetNamespaceSeparatorToUnderscore();
                            //configure endpoints but exclude credit transaction consumer
                            //since it has to be registered in all endpoints and will never
                            //run on it's own endpoint
                            configurator.ConfigureEndpoints(context, filter =>
                            {
                                filter.Exclude<PostTransactionToSourceConsumer>();
                                filter.Exclude<PostTransactionToDestConsumer>();
                                filter.Exclude<PostTransactionChargeConsumer>();
                                filter.Exclude<ReversePostTransactionToSource>();

                                filter.Exclude<CompleteTransactionConsumer>();
                                filter.Exclude<CompleteTransactionChargeConsumer>();
                            });

                            configurator.ReceiveEndpoint("transaction-processing", endpoint =>
                            {
                                endpoint.UseDelayedRedelivery(retry =>
                                {
                                    retry.Ignore<CosmosException>(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
                                    retry.Incremental(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));
                                });
                                endpoint.UseMessageRetry(r =>
                                {
                                    r.Ignore<CosmosException>(ex => ex.StatusCode == System.Net.HttpStatusCode.NotFound);
                                    r.Interval(3, TimeSpan.FromSeconds(5));
                                });
                                endpoint.ConfigureConsumer<CompleteTransactionConsumer>(context);
                                endpoint.ConfigureConsumer<CompleteTransactionChargeConsumer>(context);
                            });

                            foreach(var value in Enum.GetValues<AccountType>())
                            {
                                var name = AccountTypeQueueHelper.GetQueueName(value);
                                configurator.ReceiveEndpoint(name, endpoint =>
                                {
                                    endpoint.RequiresSession = true;
                                    configurator.UseServiceBusMessageScheduler();
                                    configurator.UseDelayedRedelivery(retry =>
                                    {
                                        retry.Ignore<AccountNumberNotFound>();
                                        retry.Ignore<AccountTypeMisMatchException>();
                                        retry.Incremental(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30));
                                    });
                                    configurator.UseMessageRetry(r =>
                                    {
                                        r.Ignore<AccountNumberNotFound>();
                                        r.Ignore<AccountTypeMisMatchException>();
                                        r.Interval(3, TimeSpan.FromSeconds(5));
                                    });

                                    endpoint.ConfigureConsumer<PostTransactionToSourceConsumer>(context);
                                    endpoint.ConfigureConsumer<PostTransactionToDestConsumer>(context);
                                    endpoint.ConfigureConsumer<PostTransactionChargeConsumer>(context);
                                    endpoint.ConfigureConsumer<ReversePostToSourceConsumer>(context);
                                });
                            }
                        });
                    });
                    services.AddHostedService<MassTransitHostedService>();
                });
            await host.RunConsoleAsync();            
        }
    }
}
