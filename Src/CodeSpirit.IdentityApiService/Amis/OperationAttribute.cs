[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public class OperationAttribute : Attribute
{
    public string Label { get; }
    public string ActionType { get; }
    public string Api { get; }
    public string ConfirmText { get; }

    public OperationAttribute(string label, string actionType = "ajax", string api = null, string confirmText = null)
    {
        Label = label;
        ActionType = actionType;
        Api = api;
        ConfirmText = confirmText;
    }
}
