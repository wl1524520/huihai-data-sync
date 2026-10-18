using System;
using System.Data.Linq.Mapping;
using System.IO;
using System.Linq;
using HH.TiYu.DAL.LinqProvider;

namespace HH.TiYu.DAL
{
    public class ProviderFactory
    {
        private static readonly Lazy<MappingSource> Mapping = new Lazy<MappingSource>(LoadMapping);
        private static readonly Type[] Providers = {
            typeof(FacilityProvider), typeof(StudentProvider),
            typeof(PhysicalItemProvider), typeof(PhysicalProjectProvider),
            typeof(ProjectPhysicalItemProvider), typeof(SyncConfigProvider), typeof(StudentScoreProvider)
        };
        private static MappingSource LoadMapping()
        {
            using (Stream stream = typeof(ProviderFactory).Assembly.GetManifestResourceStream("HH.TiYu.DAL.LinqProvider.DataMapping.xml"))
            {
                if (stream == null) throw new InvalidOperationException("缺少采集器 DAL 映射资源");
                return XmlMappingSource.FromStream(stream);
            }
        }
        public static T Create<T>(string connStr) where T : class
        {
            Type provider = Providers.SingleOrDefault(type => typeof(T).IsAssignableFrom(type));
            if (provider == null)
                throw new NotSupportedException("采集器未启用数据访问接口: " + typeof(T).FullName);
            return (T)Activator.CreateInstance(provider, connStr, Mapping.Value);
        }
    }
}
