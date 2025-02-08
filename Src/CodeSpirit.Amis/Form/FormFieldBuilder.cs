using CodeSpirit.Amis.Helpers;
using Newtonsoft.Json.Linq;

namespace CodeSpirit.Amis.Form
{
    public class FormFieldBuilder
    {
        private readonly UtilityHelper _utilityHelper;

        private string _name;
        private string _label;
        private bool _isRequired;
        private string _fieldType;
        private JObject _validationRules;
        private JObject _validationErrors;

        public FormFieldBuilder(UtilityHelper utilityHelper)
        {
            _utilityHelper = utilityHelper;
            _validationRules = [];
            _validationErrors = [];
        }

        public FormFieldBuilder SetName(string name)
        {
            _name = name;
            return this;
        }

        public FormFieldBuilder SetLabel(string label)
        {
            _label = label;
            return this;
        }

        public FormFieldBuilder SetRequired(bool isRequired)
        {
            _isRequired = isRequired;
            return this;
        }

        public FormFieldBuilder SetFieldType(string fieldType)
        {
            _fieldType = fieldType;
            return this;
        }

        public FormFieldBuilder AddValidationRule(string ruleName, object ruleValue)
        {
            _validationRules[ruleName] = (JToken)ruleValue;
            return this;
        }

        public FormFieldBuilder AddValidationError(string ruleName, string errorMessage)
        {
            _validationErrors[ruleName] = errorMessage;
            return this;
        }

        public JObject Build()
        {
            JObject field = new()
            {
                ["name"] = _name,
                ["label"] = _label,
                ["required"] = _isRequired,
                ["type"] = _fieldType
            };

            if (_validationRules.HasValues)
            {
                field["validations"] = _validationRules;
            }

            if (_validationErrors.HasValues)
            {
                field["validationErrors"] = _validationErrors;
            }

            return field;
        }
    }

}
