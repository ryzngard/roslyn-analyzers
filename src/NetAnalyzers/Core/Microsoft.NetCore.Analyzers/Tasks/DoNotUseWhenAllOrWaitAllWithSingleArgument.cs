﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.NetCore.Analyzers.Tasks
{
    public abstract class DoNotUseWhenAllOrWaitAllWithSingleArgument : DiagnosticAnalyzer
    {
        internal const string WhenAllRuleId = "CA2250";
        internal const string WaitAllRuleId = "CA2251";

        internal static readonly DiagnosticDescriptor WhenAllRule = DiagnosticDescriptorHelper.Create(WhenAllRuleId,
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            DiagnosticCategory.Usage,
            RuleLevel.BuildWarning,
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static readonly DiagnosticDescriptor WaitAllRule = DiagnosticDescriptorHelper.Create(WaitAllRuleId,
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            DiagnosticCategory.Usage,
            RuleLevel.BuildWarning,
            new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.DoNotUseWhenAllWithSingleTaskDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources)),
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(WhenAllRule, WaitAllRule);

        protected abstract bool IsSingleTaskArgument(IInvocationOperation invocation, INamedTypeSymbol taskType, INamedTypeSymbol task1Type);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterCompilationStartAction(compilationContext =>
            {
                if (compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask, out var taskType) &&
                    compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask1, out var task1Type))
                {
                    compilationContext.RegisterOperationAction(operationContext =>
                    {
                        var invocation = (IInvocationOperation)operationContext.Operation;
                        if (IsWhenOrWaitAllMethod(invocation.TargetMethod, taskType) &&
                            IsSingleTaskArgument(invocation, taskType, task1Type))
                        {
                            switch (invocation.TargetMethod.Name)
                            {
                                case nameof(Task.WhenAll):
                                    operationContext.ReportDiagnostic(invocation.CreateDiagnostic(WhenAllRule));
                                    break;

                                case nameof(Task.WaitAll):
                                    operationContext.ReportDiagnostic(invocation.CreateDiagnostic(WaitAllRule));
                                    break;

                                default:
                                    throw new InvalidOperationException($"Unexpected method name: {invocation.TargetMethod.Name}");
                            }
                        }
                    }, OperationKind.Invocation);
                }
            });
        }

        private static bool IsWhenOrWaitAllMethod(IMethodSymbol targetMethod, INamedTypeSymbol taskType)
        {
            var nameMatches = targetMethod.Name is (nameof(Task.WhenAll)) or (nameof(Task.WaitAll));
            var parameters = targetMethod.Parameters;

            return nameMatches &&
                targetMethod.IsStatic &&
                targetMethod.ContainingType == taskType &&
                parameters.Length == 1 &&
                parameters[0].IsParams;
        }
    }
}
