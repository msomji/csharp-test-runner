using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Exercism.TestRunner.CSharp
{
    internal static class CompilationRewriter
    {
        public static Compilation Rewrite(this Compilation compilation) =>
            compilation
                .UnskipTests()
                .CaptureTracesAsTestOutput();

        private static Compilation UnskipTests(this Compilation compilation) =>
            compilation.Rewrite(new UnskipTestsRewriter());

        private static Compilation CaptureTracesAsTestOutput(this Compilation compilation) =>
            compilation.Rewrite(new CaptureTracesAsTestOutputRewriter());

        private static Compilation Rewrite(this Compilation compilation, CSharpSyntaxRewriter rewriter)
        {
            foreach (var syntaxTree in compilation.SyntaxTrees)
                compilation = compilation.ReplaceSyntaxTree(
                    syntaxTree,
                    syntaxTree.WithRootAndOptions(
                        rewriter.Visit(syntaxTree.GetRoot()), syntaxTree.Options));

            return compilation;
        }

        private class UnskipTestsRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitAttributeArgument(AttributeArgumentSyntax node)
            {
                if (IsSkipAttributeArgument(node))
                    return null;

                return base.VisitAttributeArgument(node);
            }

            private static bool IsSkipAttributeArgument(AttributeArgumentSyntax node) =>
                node.NameEquals?.Name.ToString() == "Skip";
        }

        private class CaptureTracesAsTestOutputRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node)
            {
                if (node.DescendantNodes().Any(IsTestClass))
                    return base.VisitCompilationUnit(
                        node.WithUsings(
                            node.Usings
                                .Add(
                                UsingDirective(QualifiedName(
                                IdentifierName("Xunit").WithLeadingTrivia(Space),
                                IdentifierName("Abstractions"))))
                                .Add(UsingDirective(IdentifierName("System").WithLeadingTrivia(Space)))));

                return base.VisitCompilationUnit(node);
            }
            public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (IsTestClass(node))
                    return base.VisitClassDeclaration(
                        node.WithBaseList(
                                BaseList(
                                    SingletonSeparatedList<BaseTypeSyntax>(
                                        SimpleBaseType(
                                            IdentifierName("IDisposable")))))
                            .WithMembers(
                                node.Members
                                    .Insert(0, MethodDeclaration(
                                            PredefinedType(
                                                Token(SyntaxKind.VoidKeyword).WithTrailingTrivia(Space)),
                                            Identifier("Dispose"))
                                        .WithModifiers(
                                            TokenList(
                                                Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(Space)))
                                        .WithBody(
                                            Block(
                                                SingletonList<StatementSyntax>(
                                                    ExpressionStatement(
                                                        InvocationExpression(
                                                            MemberAccessExpression(
                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                IdentifierName("XunitContext"),
                                                                IdentifierName("Flush"))))))))
                                    .Insert(0, 
                                    ConstructorDeclaration(
                                            Identifier("XunitLoggerSample").WithTrailingTrivia(Space))
                                        .WithModifiers(
                                            TokenList(
                                                Token(SyntaxKind.PublicKeyword).WithTrailingTrivia(Space)))
                                        .WithParameterList(
                                            ParameterList(
                                                SingletonSeparatedList<ParameterSyntax>(
                                                    Parameter(
                                                            Identifier("testOutput"))
                                                        .WithType(
                                                            IdentifierName("ITestOutputHelper").WithTrailingTrivia(Space)))))
                                        .WithBody(
                                            Block(
                                                SingletonList<StatementSyntax>(
                                                    ExpressionStatement(
                                                        InvocationExpression(
                                                                MemberAccessExpression(
                                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                                    IdentifierName("XunitContext"),
                                                                    IdentifierName("Register")))
                                                            .WithArgumentList(
                                                                ArgumentList(
                                                                    SingletonSeparatedList<ArgumentSyntax>(
                                                                        Argument(
                                                                            IdentifierName("testOutput")))))))))))
                                    );

                return base.VisitClassDeclaration(node);
            }

            private static bool IsTestClass(SyntaxNode descendant) =>
                descendant is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.Identifier.Text.EndsWith("Test");
        }
    }
}