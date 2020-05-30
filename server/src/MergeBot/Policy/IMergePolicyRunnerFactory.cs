using System.Threading.Tasks;

namespace MergeBot
{
    public interface IMergePolicyRunnerFactory
    {
        Task<IMergePolicyRunner> CreateAsync(MergePolicyRunnerFactoryContext context);
        void Clear(MergePolicyRunnerFactoryContext context);
    }
}
