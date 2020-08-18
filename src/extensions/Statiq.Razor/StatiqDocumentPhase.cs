﻿using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Statiq.Razor
{
    internal class StatiqDocumentPhase : RazorEnginePhaseBase
    {
        private readonly string _baseType;
        private readonly NamespaceCollection _namespaces;

        public StatiqDocumentPhase(string baseType, NamespaceCollection namespaces)
        {
            _baseType = baseType;
            _namespaces = namespaces;
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            DocumentIntermediateNode documentNode = codeDocument.GetDocumentIntermediateNode();

            NamespaceDeclarationIntermediateNode namespaceDeclaration =
                documentNode.Children.OfType<NamespaceDeclarationIntermediateNode>().Single();

            string modelType = ModelDirective.GetModelType(documentNode);

            // Set the base page type and perform default model type substitution here
            ClassDeclarationIntermediateNode classDeclaration =
                namespaceDeclaration.Children.OfType<ClassDeclarationIntermediateNode>().Single();
            classDeclaration.BaseType = _baseType.Replace("<TModel>", "<" + modelType + ">");

            // Add namespaces
            int insertIndex = namespaceDeclaration.Children.IndexOf(
                namespaceDeclaration.Children.OfType<UsingDirectiveIntermediateNode>().First());
            foreach (string ns in _namespaces)
            {
                namespaceDeclaration.Children.Insert(
                    insertIndex,
                    new UsingDirectiveIntermediateNode()
                    {
                        Content = ns
                    });
            }

            codeDocument.SetDocumentIntermediateNode(documentNode);
        }
    }
}