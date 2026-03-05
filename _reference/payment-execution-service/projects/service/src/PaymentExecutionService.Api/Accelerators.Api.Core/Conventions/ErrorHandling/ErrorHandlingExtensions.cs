// ######################################################################################
// IMPORTANT: This file is part of API Service Accelerator core code.
// --------------------------------------------------------------------------------------
// Editing, moving, renaming or deleting this file may impact your ability to upgrade
// this project to newer versions of the Accelerator template.
// --------------------------------------------------------------------------------------
// See `docs/upgrade-support.md` for more information.
// ######################################################################################

using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Serilog.Events;
using Serilog.Parsing;
using Xero.Accelerators.Api.Core.Conventions.Cataloguing;
using static Xero.Accelerators.Api.Core.Constants;

namespace Xero.Accelerators.Api.Core.Conventions.ErrorHandling;

public class CustomProblemDetailsOptions
{
    public Func<HttpContext, Exception, string>? GenerateProblemInstance { get; set; }
}

public static class ErrorHandlingExtensions
{
    public static void AddXeroErrorHandling(this WebApplicationBuilder builder, CustomProblemDetailsOptions? options = null)
    {
        builder.Services.AddProblemDetails(opt =>
        {
            opt.CustomizeProblemDetails = CustomizeXeroProblemDetails(options);
        });

        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    public static Action<ProblemDetailsContext> CustomizeXeroProblemDetails(CustomProblemDetailsOptions? options = null)
    {
        return context =>
        {
            var handlerFeature = context.HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = handlerFeature?.Error;

            if (exception != null)
            {
                if (exception.Data.Contains(ExceptionDataFields.ProblemDetailsIdentifier))
                {
                    var exceptionProblemIdentifier = exception.Data[ExceptionDataFields.ProblemDetailsIdentifier] as string;
                    if (exceptionProblemIdentifier != null)
                    {
                        context.ProblemDetails.Type = $"https://common.service.xero.com/schema/problems/{exceptionProblemIdentifier}";
                    }
                }

                if (exception.Data.Contains(ExceptionDataFields.ProblemDetailsTitle))
                {
                    var exceptionProblemTitle = exception.Data[ExceptionDataFields.ProblemDetailsTitle] as string;
                    if (exceptionProblemTitle != null)
                    {
                        context.ProblemDetails.Title = exceptionProblemTitle;
                    }
                }
            }

            if (options?.GenerateProblemInstance != null)
            {
                context.ProblemDetails.Instance = options.GenerateProblemInstance(context.HttpContext, context.Exception!);
            }

            else if (context.HttpContext.Request.Headers.TryGetValue(HttpHeaders.XeroCorrelationId, out var xeroCorrelationId))
            {
                context.ProblemDetails.Instance = $"{context.HttpContext.Request.Path}/xero-correlation-id/{xeroCorrelationId.ToString().ToLower()}";
            }
        };
    }

    // Satisfies XREQ-121 (partially)/XREQ-124/XREQ-158
    public static ObjectResult CommonProblem(this ControllerBase controller, string problemIdentifier, int statusCode, string message, params object[] propertyValues)
    {
        var problemDetailsFactory = controller.HttpContext.RequestServices.GetService<ProblemDetailsFactory>();
        var problemDetails = problemDetailsFactory!.CreateCommonProblem(controller.ControllerContext.HttpContext, problemIdentifier, statusCode, message, propertyValues);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }
    public static ObjectResult CustomProblem(this ControllerBase controller, string problemIdentifier, int statusCode, string message, params object[] propertyValues)
    {
        var problemDetailsFactory = controller.HttpContext.RequestServices.GetService<ProblemDetailsFactory>();
        var problemDetails = problemDetailsFactory!.CreateCustomProblem(controller.ControllerContext.HttpContext, problemIdentifier, statusCode, message, propertyValues);
        return new ObjectResult(problemDetails) { StatusCode = statusCode };
    }

    public static ProblemDetails CreateCustomProblem(this ProblemDetailsFactory factory, HttpContext context, string problemIdentifier, int statusCode, string message, params object[] propertyValues)
    {
        return CreateCustomProblem(factory, context, problemIdentifier, statusCode, string.Empty, message, propertyValues);
    }
    public static ProblemDetails CreateCustomProblem(this ProblemDetailsFactory factory, HttpContext context, string problemIdentifier, int statusCode, string instance, string message, params object[] propertyValues)
    {
        var catalogueMetadata = context.RequestServices.GetService<CatalogueMetadata>();
        var uuidLower = catalogueMetadata!.ComponentUuid.ToLower();
        var type = $"https://{uuidLower}.service.xero.com/schema/problems/{problemIdentifier}";
        return CreateProblem(factory, context, statusCode, instance, message, type, propertyValues);
    }

    public static ProblemDetails CreateCommonProblem(this ProblemDetailsFactory factory, HttpContext context, string problemIdentifier, int statusCode, string message, params object[] propertyValues)
    {
        return CreateCommonProblem(factory, context, problemIdentifier, statusCode, string.Empty, message, propertyValues);
    }
    public static ProblemDetails CreateCommonProblem(this ProblemDetailsFactory factory, HttpContext context, string problemIdentifier, int statusCode, string instance, string message, params object[] propertyValues)
    {
        var type = $"https://common.service.xero.com/schema/problems/{problemIdentifier}";
        return CreateProblem(factory, context, statusCode, instance, message, type, propertyValues);
    }

    /// <summary>
    /// Create a bad http request exception with problem details identifier and problem details title 
    /// </summary>
    /// <param name="problemIdentifier">The problem details identifier (e.g. "invalid-xero-user-id")</param>
    /// <param name="userVisibleProblemTitle">The problem details displayed to the user (e.g. "A Xero User ID value was not specified.")</param>
    /// <param name="message">The exception message</param>
    /// <returns></returns>
    public static BadHttpRequestException CreateBadHttpRequestException(string problemIdentifier, string userVisibleProblemTitle, string message)
    {
        var exception = new BadHttpRequestException(message);
        exception.Data.Add(ExceptionDataFields.ProblemDetailsIdentifier, problemIdentifier);
        exception.Data.Add(ExceptionDataFields.ProblemDetailsTitle, userVisibleProblemTitle);

        return exception;
    }

    public static async Task WriteProblemDetailsAsync(this HttpContext context, ProblemDetails problemDetails)
    {
        var problemDetailsService = context.RequestServices.GetService<IProblemDetailsService>();

        if (problemDetails.Status != null)
        {
            context.Response.StatusCode = (int)problemDetails.Status;
        }

        await problemDetailsService!.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });
    }

    private static ProblemDetails CreateProblem(this ProblemDetailsFactory factory, HttpContext context, int statusCode, string instance, string message, string type, params object[] propertyValues)
    {
        var details = RenderMessageTemplate(message, propertyValues);

        var problemDetails = factory.CreateProblemDetails(context, statusCode, message, type, details, instance);

        if (string.IsNullOrEmpty(problemDetails.Instance) && context.Request.Headers.TryGetValue(HttpHeaders.XeroCorrelationId, out var xeroCorrelationId))
        {
            problemDetails.Instance = $"{context.Request.Path}/xero-correlation-id/{xeroCorrelationId.ToString().ToLower()}";
        }

        return problemDetails;
    }

    private static string RenderMessageTemplate(string messageTemplate, object[] propertyValues)
    {
        var parsedMessageTemplate = new MessageTemplateParser().Parse(messageTemplate);

        //validation for number of property values
        Debug.Assert(propertyValues.Length == parsedMessageTemplate.Tokens.Count(token => token is PropertyToken),
            "The number of property values does not match the number of properties in the message template.");

        return string.Format(ParseValuesToStringTemplate(parsedMessageTemplate), propertyValues.ToArray());
    }

    private static string ParseValuesToStringTemplate(MessageTemplate parsedTemplate)
    {
        var stringBuilder = new StringBuilder();
        var counter = 0;
        foreach (var token in parsedTemplate.Tokens)
        {
            if (token is PropertyToken)
            {
                stringBuilder.Append($"{{{counter}}}");
                counter++;
            }
            else
            {
                stringBuilder.Append(token);
            }
        }
        return stringBuilder.ToString();
    }
}
