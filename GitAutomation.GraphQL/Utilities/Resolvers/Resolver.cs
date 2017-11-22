using GraphQL.Resolvers;
using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Types;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using GraphQL;
using Microsoft.Extensions.Logging;

namespace GitAutomation.GraphQL.Utilities.Resolvers
{
    public class Resolver : IFieldResolver
    {
        private Func<ResolveFieldContext, object> func;

        private Resolver(Func<ResolveFieldContext, object> func)
        {
            this.func = func;
        }

        public static IFieldResolver Resolve(object target, string methodName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new ArgumentException("Method not found on target.", nameof(methodName));
            }

            var paramTypes = (from param in method.GetParameters()
                              select param.ParameterType).ToArray();

            if (!method.ReturnType.IsConstructedGenericType || method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                throw new ArgumentException("Return type must be of type Task<>", nameof(methodName));
            }

            var contextParameter = Expression.Parameter(typeof(ResolveFieldContext), "context");
            var arguments = (from param in method.GetParameters()
                             select ParameterToExpression(param, contextParameter)).ToArray();
            var result = Expression.Lambda<Func<ResolveFieldContext, object>>(
                Expression.Convert(
                    Expression.Call(
                        instance: Expression.Constant(target, target.GetType()),
                        method: method,
                        arguments: arguments
                    ),
                    typeof(object)
                ),
                contextParameter
            );

            return new Resolver(result.Compile());
        }

        private static Expression ParameterToExpression(ParameterInfo param, ParameterExpression contextParameter)
        {
            if (param.IsOut || param.IsRetval)
            {
                throw new ArgumentException("Invalid argument; parameter cannot be out or ref.", param.Name);
            }
            var attr = param.GetCustomAttribute<ResolverAttribute>();
            switch (attr)
            {
                case SourceAttribute a:
                    return Expression.Convert(Expression.Property(contextParameter, nameof(ResolveFieldContext.Source)), param.ParameterType);

                case FromServicesAttribute a:
                    var serviceProviderExpression = Expression.Convert(
                            Expression.Property(
                                contextParameter,
                                nameof(ResolveFieldContext.UserContext)
                            ),
                            typeof(IServiceProvider)
                        );
                    return Expression.Call(
                        (param.IsOptional
                            ? GetOptionalServiceMethod
                            : GetRequiredServiceMethod)
                            .MakeGenericMethod(param.ParameterType),
                        serviceProviderExpression
                    );

                case FromArgumentAttribute a:
                    return Expression.Call(
                        contextParameter,
                        GetArgumentGeneric.MakeGenericMethod(param.ParameterType),
                        Expression.Constant(param.Name),
                        Expression.Constant(
                            param.ParameterType.IsValueType
                                ? Activator.CreateInstance(param.ParameterType)
                                : null,
                            param.ParameterType
                        )
                    );

                default:
                    if (param.ParameterType == contextParameter.Type)
                    {
                        return contextParameter;
                    }
                    throw new ArgumentException("Invalid argument; parameter must have a ResolverAttribute.", param.Name);
            }

        }

        private static readonly MethodInfo GetArgumentGeneric =
            typeof(ResolveFieldContext).GetMethods()
                .Where(m => m.Name == nameof(ResolveFieldContext.GetArgument) && m.IsGenericMethodDefinition)
                .SingleOrDefault();

        private static readonly MethodInfo GetOptionalServiceMethod = typeof(Resolver).GetMethod(nameof(GetOptionalService), BindingFlags.Static | BindingFlags.NonPublic);
        private static T GetOptionalService<T>(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<T>();
        }

        private static readonly MethodInfo GetRequiredServiceMethod = typeof(Resolver).GetMethod(nameof(GetRequiredService), BindingFlags.Static | BindingFlags.NonPublic);
        private static T GetRequiredService<T>(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<T>();
        }

        private static readonly MethodInfo FinalCastMethod = typeof(Resolver).GetMethod(nameof(FinalCast), BindingFlags.Static | BindingFlags.NonPublic);
        private static object FinalCast<T>(Task<T> input)
        {
            return input.Result;
        }

        public object Resolve(ResolveFieldContext context)
        {
            return ResolveAsync(context);
        }

        public async Task<object> ResolveAsync(ResolveFieldContext context)
        {
            try
            {
                var logger = (context.UserContext as IServiceProvider).GetRequiredService<ILoggerFactory>().CreateLogger<Resolver>();
                logger.LogInformation("Resolving field {0}", context.FieldName);
                var result = func(context);
                if (result is Task t)
                {
                    await t.ConfigureAwait(false);
                    result = t.GetProperyValue(nameof(Task<object>.Result));
                }
                logger.LogInformation("Finished resolving field {0}", context.FieldName);
                return result;
            }
            catch (Exception ex)
            {
                var err = new ExecutionError(ex.Message);
                err.AddLocation(context.FieldAst, context.Document);
                context.Errors.Add(err);
                return null;
            }
        }
    }
}
