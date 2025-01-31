using CodeSpirit.Amis.App;
using FluentValidation;

namespace CodeSpirit.Amis.Validators
{
    public class PageValidator : AbstractValidator<Page>
    {
        public PageValidator()
        {
            RuleFor(page => page.Label)
                .NotEmpty().WithMessage("Page label is required.")
                .MaximumLength(100).WithMessage("Page label cannot exceed 100 characters.");

            RuleFor(page => page.Url)
                .NotEmpty().WithMessage("Page URL is required.")
                .Matches(@"^\/[a-zA-Z0-9\-\/]*$").WithMessage("Page URL is invalid.");

            RuleFor(page => page.ParentLabel)
                .MaximumLength(100).WithMessage("Parent label cannot exceed 100 characters.");

            // 其他业务规则可以在此添加
        }
    }
}
