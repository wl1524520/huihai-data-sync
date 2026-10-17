using HH.GeneralLibrary.Core.DAL;

namespace HH.TiYu.Model
{
    public class SyncConfigEntry : IEntity<string>
    {
        public string ID { get; set; }
        public string Value { get; set; }
    }
}
