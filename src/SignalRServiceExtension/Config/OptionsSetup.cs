﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class OptionsSetup : IConfigureOptions<ServiceManagerOptions>, IOptionsChangeTokenSource<ServiceManagerOptions>
    {
        private readonly IConfiguration configuration;
        private readonly string connectionStringKey;
        private readonly ILogger logger;

        public OptionsSetup(IConfiguration configuration, ILoggerFactory loggerFactory, string connectionStringKey)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (string.IsNullOrWhiteSpace(connectionStringKey))
            {
                throw new ArgumentException($"'{nameof(connectionStringKey)}' cannot be null or whitespace", nameof(connectionStringKey));
            }

            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.connectionStringKey = connectionStringKey;
            logger = loggerFactory.CreateLogger<OptionsSetup>();
        }

        public string Name => Options.DefaultName;

        public void Configure(ServiceManagerOptions options)
        {
            options.ConnectionString = configuration.GetConnectionString(connectionStringKey) ?? configuration[connectionStringKey];
            options.ServiceEndpoints = configuration.GetEndpoints(Constants.AzureSignalREndpoints).ToArray();
            var serviceTransportTypeStr = configuration[Constants.ServiceTransportTypeName];
            if (Enum.TryParse<ServiceTransportType>(serviceTransportTypeStr, out var transport))
            {
                options.ServiceTransportType = transport;
            }
            else if (string.IsNullOrWhiteSpace(serviceTransportTypeStr))
            {
                options.ServiceTransportType = ServiceTransportType.Transient;
                logger.LogInformation($"{Constants.ServiceTransportTypeName} not set, using default {ServiceTransportType.Transient} instead.");
            }
            else
            {
                options.ServiceTransportType = ServiceTransportType.Transient;
                logger.LogWarning($"Unsupported service transport type: {serviceTransportTypeStr}. Use default {ServiceTransportType.Transient} instead.");
            }
            //make connection more stable
            options.ConnectionCount = 3;
            options.ProductInfo = GetProductInfo();
        }

        public IChangeToken GetChangeToken()
        {
            return configuration.GetReloadToken();
        }

        private string GetProductInfo()
        {
            var workerRuntime = configuration[Constants.FunctionsWorkerRuntime];
            var sdkProductInfo = ProductInfo.GetProductInfo();
            return $"{sdkProductInfo} [{Constants.FunctionsWorkerProductInfoKey}={workerRuntime}]";
        }
    }
}