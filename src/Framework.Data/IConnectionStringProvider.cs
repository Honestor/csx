using System.Threading.Tasks;

namespace Framework.Core.Data
{
    public interface IConnectionStringProvider
    {
        string GetConnectionName();

        string GetConnectionString();
    }
}
