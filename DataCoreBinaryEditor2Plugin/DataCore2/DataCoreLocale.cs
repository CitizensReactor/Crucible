namespace DataCore2
{
    public interface IDataCoreLocale { };
    public class DataCoreLocale : IDataCoreLocale
    {
        public DataCoreLocale()
        {
            this.String = "";
        }
        public DataCoreLocale(string str)
        {
            this.String = str;
        }

        public string String { get; set; }

        public override string ToString()
        {
            return String;
        }
    }
}
