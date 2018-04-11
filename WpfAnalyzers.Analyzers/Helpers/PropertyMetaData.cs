﻿namespace WpfAnalyzers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class PropertyMetadata
    {
        internal static bool TryGetConstructor(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out IMethodSymbol constructor)
        {
            return Constructor.TryGet(objectCreation, KnownSymbol.PropertyMetadata, semanticModel, cancellationToken, out constructor) ||
                   Constructor.TryGet(objectCreation, KnownSymbol.UIPropertyMetadata, semanticModel, cancellationToken, out constructor) ||
                   Constructor.TryGet(objectCreation, KnownSymbol.FrameworkPropertyMetadata, semanticModel, cancellationToken, out constructor);
        }

        internal static bool TryGetDefaultValue(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax defaultValueArg)
        {
            defaultValueArg = null;
            if (objectCreation?.ArgumentList == null ||
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out var constructor) &&
                   constructor.Parameters.TryFirst(out var parameter) &&
                   parameter.Type == KnownSymbol.Object &&
                   objectCreation.ArgumentList.Arguments.TryFirst(out defaultValueArg);
        }

        internal static bool TryGetPropertyChangedCallback(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            return TryGetCallback(objectCreation, KnownSymbol.PropertyChangedCallback, semanticModel, cancellationToken, out callback);
        }

        internal static bool TryGetCoerceValueCallback(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            return TryGetCallback(objectCreation, KnownSymbol.CoerceValueCallback, semanticModel, cancellationToken, out callback);
        }

        internal static bool TryGetRegisteredName(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out string registeredName)
        {
            registeredName = null;
            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out _) &&
                   DependencyProperty.TryGetRegisteredName(objectCreation?.FirstAncestorOrSelf<InvocationExpressionSyntax>(), semanticModel, cancellationToken, out registeredName);
        }

        internal static bool TryGetDependencyProperty(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CancellationToken cancellationToken, out BackingFieldOrProperty fieldOrProperty)
        {
            fieldOrProperty = default(BackingFieldOrProperty);
            var invocation = objectCreation.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation == null)
            {
                return false;
            }

            if (DependencyProperty.TryGetRegisterCall(invocation, semanticModel, cancellationToken, out _) ||
                DependencyProperty.TryGetRegisterReadOnlyCall(invocation, semanticModel, cancellationToken, out _) ||
                DependencyProperty.TryGetRegisterAttachedCall(invocation, semanticModel, cancellationToken, out _) ||
                DependencyProperty.TryGetRegisterAttachedReadOnlyCall(invocation, semanticModel, cancellationToken, out _))
            {
                return BackingFieldOrProperty.TryCreate(semanticModel.GetDeclaredSymbolSafe(objectCreation.FirstAncestorOrSelf<FieldDeclarationSyntax>(), cancellationToken), out fieldOrProperty) ||
                       BackingFieldOrProperty.TryCreate(semanticModel.GetDeclaredSymbolSafe(objectCreation.FirstAncestorOrSelf<PropertyDeclarationSyntax>(), cancellationToken), out fieldOrProperty);
            }

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                (DependencyProperty.TryGetAddOwnerCall(invocation, semanticModel, cancellationToken, out _) ||
                 DependencyProperty.TryGetOverrideMetadataCall(invocation, semanticModel, cancellationToken, out _)))
            {
                return BackingFieldOrProperty.TryCreate(semanticModel.GetSymbolSafe(memberAccess.Expression, cancellationToken), out fieldOrProperty);
            }

            return false;
        }

        private static bool TryGetCallback(ObjectCreationExpressionSyntax objectCreation, QualifiedType callbackType, SemanticModel semanticModel, CancellationToken cancellationToken, out ArgumentSyntax callback)
        {
            callback = null;
            if (objectCreation?.ArgumentList == null ||
                objectCreation.ArgumentList.Arguments.Count == 0)
            {
                return false;
            }

            return TryGetConstructor(objectCreation, semanticModel, cancellationToken, out var constructor) &&
                   Argument.TryGetArgument(constructor.Parameters, objectCreation.ArgumentList, callbackType, out callback);
        }
    }
}
