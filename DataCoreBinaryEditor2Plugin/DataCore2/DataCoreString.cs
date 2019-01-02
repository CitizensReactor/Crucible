namespace DataCore2
{
    public interface IDataCoreString { };
    public class DataCoreString : IDataCoreString
    {
        public DataCoreString()
        {
            this.String = "";
        }
        public DataCoreString(string str)
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
