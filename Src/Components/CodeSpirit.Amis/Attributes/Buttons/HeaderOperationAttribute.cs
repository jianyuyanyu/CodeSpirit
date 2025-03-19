[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class HeaderOperationAttribute : OperationAttribute
{
    public HeaderOperationAttribute(string label, string actionType = "ajax", string api = null, string confirmText = null, string visibleOn = null, bool isBulkOperation = false) : base(label, actionType, api, confirmText, visibleOn, isBulkOperation)
    {
    }
}