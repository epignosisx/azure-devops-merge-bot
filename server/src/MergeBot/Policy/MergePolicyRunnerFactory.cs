using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MergeBot
{
    public class MergePolicyRunnerFactoryContext : IEquatable<MergePolicyRunnerFactoryContext>
    {
        public MergePolicyRunnerFactoryContext(IAzureDevOpsClient azDoClient, string repositoryId, string organization)
        {
            AzDoClient = azDoClient ?? throw new ArgumentNullException(nameof(azDoClient));
            RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
            Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        }

        public MergePolicyRunnerFactoryContext(string repositoryId, string organization)
        {
            RepositoryId = repositoryId ?? throw new ArgumentNullException(nameof(repositoryId));
            Organization = organization ?? throw new ArgumentNullException(nameof(organization));
        }

        public IAzureDevOpsClient? AzDoClient { get; }
        public string RepositoryId { get; }
        public string Organization { get; }

        public bool Equals([AllowNull] MergePolicyRunnerFactoryContext other)
        {
            if (other is null)
                return false;

            return string.Equals(Organization, other.Organization, StringComparison.Ordinal) &&
                string.Equals(RepositoryId, other.RepositoryId, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return (obj is MergePolicyRunnerFactoryContext other) && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Organization, RepositoryId);
        }

        public override string? ToString()
        {
            return $"{Organization} - {RepositoryId}";
        }
    }

    public sealed class MergePolicyRunnerFactory : IMergePolicyRunnerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MergePolicyRunnerFactory> _logger;
        private readonly ConcurrentDictionary<MergePolicyRunnerFactoryContext, Task<IMergePolicyRunner>> _cache = new ConcurrentDictionary<MergePolicyRunnerFactoryContext, Task<IMergePolicyRunner>>();
        private readonly Func<MergePolicyRunnerFactoryContext, Task<IMergePolicyRunner>> _valueFactory;

        public MergePolicyRunnerFactory(
            IServiceProvider serviceProvider,
            ILogger<MergePolicyRunnerFactory> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _valueFactory = CreateNew;
        }

        public void Clear(MergePolicyRunnerFactoryContext context)
        {
            _cache.Remove(context, out _);
        }

        public Task<IMergePolicyRunner> CreateAsync(MergePolicyRunnerFactoryContext context)
        {
            return _cache.GetOrAdd(context, _valueFactory);
        }

        private async Task<IMergePolicyRunner> CreateNew(MergePolicyRunnerFactoryContext context)
        {
            var policyConfiguration = await context.AzDoClient!.GetMergePoliciesAsync(context.Organization, context.RepositoryId);
            if (policyConfiguration.Value is null || policyConfiguration.Value.Count == 0)
                return NoopMergePolicyRunner.Instance;

            var policies = new List<MergePolicy>();
            foreach (var policyConfig in policyConfiguration.Value.OrderBy(n => n.CreateDate))
            {
                var policyType = policyConfig.Strategy switch
                {
                    ReleaseBranchCascadingPolicy.PolicyName => typeof(ReleaseBranchCascadingPolicy),
                    SpecificSourceAndTargetPolicy.PolicyName => typeof(SpecificSourceAndTargetPolicy),
                    _ => null
                };

                if (policyType != null)
                {
                    var policy = (MergePolicy)_serviceProvider.GetService(policyType);
                    try
                    {
                        policy.Configure(policyConfig);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(new EventId(1, "ConfigurationFailed"), ex, "Failed to configure {Policy} with {@Config}", policyType.Name, policyConfig);
                        return NoopMergePolicyRunner.Instance;
                    }
                    policies.Add(policy);
                }
            }

            return new MergePolicyRunner(
                policies.ToArray(),
                _serviceProvider.GetService<ILogger<MergePolicyRunner>>()
            );
        }
    }
}
