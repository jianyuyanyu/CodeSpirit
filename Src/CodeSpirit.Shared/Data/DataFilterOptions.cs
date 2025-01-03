namespace CodeSpirit.Shared.Data
{
    public class DataFilterOptions
    {
        public Dictionary<Type, DataFilterState> DefaultStates { get; }

        public DataFilterOptions()
        {
            DefaultStates = new Dictionary<Type, DataFilterState>();
        }
    }
}