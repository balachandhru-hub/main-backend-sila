using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SharedKernel.Attributes
{
    /// <summary>
    /// Model state validation attribute
    /// </summary>
    public sealed class ValidateModelStateAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Called before the action method is invoked
        /// </summary>
        /// <param name="context"></param>
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor descriptor)
            {
                foreach (ParameterInfo parameter in descriptor.MethodInfo.GetParameters())
                {
                    object? args = null;
                    if (context.ActionArguments.TryGetValue(parameter.Name!, out object? value))
                    {
                        args = value;
                    }

                    ValidateAttributes(parameter, args!, context.ModelState);
                }
            }

            if (!context.ModelState.IsValid)
            {
                context.Result = new BadRequestObjectResult(context.ModelState);
            }
        }

        private static void ValidateAttributes(ParameterInfo parameter, object args, ModelStateDictionary modelState)
        {
            foreach (CustomAttributeData attributeData in parameter.CustomAttributes)
            {
                System.Attribute attributeInstance = parameter.GetCustomAttribute(attributeData.AttributeType)!;

                if (attributeInstance is ValidationAttribute validationAttribute)
                {
                    bool isValid = validationAttribute.IsValid(args);
                    if (!isValid)
                    {
                        modelState.AddModelError(parameter.Name!, validationAttribute.FormatErrorMessage(parameter.Name!));
                    }
                }
            }
        }
    }
}